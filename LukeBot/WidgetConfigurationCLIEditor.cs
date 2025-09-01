using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using LukeBot.Common;
using LukeBot.Services;
using LukeBot.User.Common;
using LukeBot.Widget;
using LukeBot.Widget.Common;
using Org.BouncyCastle.Asn1.X509.Qualified;


namespace LukeBot
{
    internal class WidgetConfigurationCLIEditor
    {
        private enum EditorState
        {
            PickOption = 0,
            SetSimpleOption,
            EditArrayOption,
            EditListOption,
            EditEnumerableOption,
            Exit,
        };

        // not an error, but rather a way out from some CLI-while-query-loops
        private class OperationAbortedException: Common.Exception
        {
            public OperationAbortedException()
                : base("Current operation was aborted")
            { }
        }

        // something that should not happen and probably should be fixed ASAP
        private class InternalErrorException: Common.Exception
        {
            public InternalErrorException(string reason, params object[] args)
                : base("Encountered an internal error: " + String.Format(reason, args))
            { }
        }

        private string mPrintedName = "";
        private ConfigurationBase mConfiguration = null;
        private CLIMessageProxy mCLI = null;
        private EditorState mState = EditorState.PickOption;

        private void CLIMessage(string msg, params object[] args)
        {
            mCLI.Message(String.Format(msg, args));
        }

        private string CLIQuery(bool mask, string msg, params object[] args)
        {
            return mCLI.Query(mask, String.Format(msg, args));
        }

        // min-max are inclusive, so with min=0 and max=5 answers "0" or "5" are accepted
        private int CLIQueryNumber(int min, int max, string query)
        {
            while (true)
            {
                string answer = CLIQuery(false, query + " (range {0}-{1}, \"q\" to abort)", min, max);

                if (Int32.TryParse(answer, out int ret))
                {
                    if (ret < min)
                    {
                        CLIMessage("Provided too low number");
                    }
                    else if (ret > max)
                    {
                        CLIMessage("Provided too high number");
                    }
                    else
                    {
                        return ret;
                    }
                }
                else if (answer == "q")
                {
                    throw new OperationAbortedException();
                }
                else
                {
                    CLIMessage("Provided answer is not a number\n");
                }
            }
        }

        private void ProcessFieldEdit(ConfigurationField field)
        {
            try
            {
                switch (field.FieldType)
                {
                case ConfigurationFieldType.Simple:
                case ConfigurationFieldType.String:
                    string newValue = CLIQuery(false, "New value");
                    field.SetFromString(newValue);
                    break;
                case ConfigurationFieldType.Class:
                    if (!field.UnderlyingType.IsAssignableTo(typeof(ConfigurationBase)))
                    {
                        throw new InternalErrorException("Cannot edit field, it must inherit Configuration<T>.");
                    }

                    new WidgetConfigurationCLIEditor(field.Name, field.Get<ConfigurationBase>(), mCLI).MainLoop();
                    break;
                default:
                    CLIMessage("WARNING: Unrecognized field type {0}. Can't edit field {1}.", field.FieldType, field.Name);
                    break;
                }
            }
            catch (ConfigurationFieldException e)
            {
                CLIMessage("Failed to set new value for configuration field {0}: {1}", field.Name, e.Message);
            }
        }

        private void ProcessListAdd<T>(List<T> list)
            where T : new()
        {
            int option = 0;
            if (list.Count > 0)
            {
                option = CLIQueryNumber(0, list.Count, "Select index at which to add the new entry");
            }

            list.Insert(option, new T());
        }

        private void ProcessListRemove<T>(List<T> list)
        {
            int option = 0;
            if (list.Count > 1)
            {
                option = CLIQueryNumber(0, list.Count - 1, "Select element to remove");
            }

            list.RemoveAt(option);
        }

        private void ProcessListMove<T>(List<T> list)
        {
            if (list.Count <= 1)
            {
                CLIMessage("Nothing to move");
                return;
            }

            int who = CLIQueryNumber(0, list.Count - 1, "Select element to move");
            int where = CLIQueryNumber(0, list.Count - 1, "Move to which index");
            if (who == where)
                return;

            T item = list[who];
            if (who < where)
            {
                for (; who < where; who++)
                {
                    list[who] = list[who + 1];
                }

                list[who] = item;
            }
            else
            {
                for (; who > where; who--)
                {
                    list[who] = list[who - 1];
                }

                list[who] = item;
            }
        }

        private void ProcessListEdit<T>(ConfigurationFieldAccessor<List<T>> field, List<T> list)
            where T : new()
        {
            int element = CLIQueryNumber(0, list.Count - 1, "Select element to edit");

            switch (field.UnderlyingFieldType)
            {
            case ConfigurationFieldType.Simple:
            case ConfigurationFieldType.String:
                string newValue = CLIQuery(false, "New value: ");
                list[element] = (T)Convert.ChangeType(newValue, typeof(T));
                break;
            case ConfigurationFieldType.Class:
                if (!field.UnderlyingType.IsAssignableTo(typeof(ConfigurationBase)))
                {
                    throw new InternalErrorException("Cannot edit field, it must inherit Configuration<T>.");
                }

                new WidgetConfigurationCLIEditor(field.Name + "[" + element + "]", list[element] as ConfigurationBase, mCLI).MainLoop();
                break;
            }
        }

        private ConfigurationField ProcessPickOptionState()
        {
            Dictionary<string, ConfigurationField> fields = mConfiguration.GetFields();

            CLIMessage("Editing {0} Configuration", mPrintedName);
            CLIMessage("Available configuration options:");
            foreach (ConfigurationField field in fields.Values)
            {
                string msg = String.Format("  {0} - {1}", field.Name, field.GetValueString());

                string allowed = field.DescribeAllowedValues();
                if (allowed.Length > 0) msg += " [" + allowed + "]";
                CLIMessage(msg);
            }

            string answer = CLIQuery(false, "\nPick option to edit by name (or Q to exit)");

            if (answer == "Q" || answer == "q")
            {
                mState = EditorState.Exit;
            }
            else if (fields.TryGetValue(answer, out ConfigurationField fieldToEdit))
            {
                switch (fieldToEdit.FieldType)
                {
                case ConfigurationFieldType.Simple:
                case ConfigurationFieldType.String:
                    mState = EditorState.SetSimpleOption;
                    break;
                case ConfigurationFieldType.Array:
                    mState = EditorState.EditArrayOption;
                    break;
                case ConfigurationFieldType.List:
                    mState = EditorState.EditListOption;
                    break;
                case ConfigurationFieldType.Enumerable:
                    mState = EditorState.EditEnumerableOption;
                    break;
                case ConfigurationFieldType.Class:
                    // TODO not true! should be edited via a new instance of the editor
                    CLIMessage("Cannot edit Class fields! Edit their members by selecting them internally");
                    break;
                default:
                    throw new InternalErrorException("Invalid field type {0}", fieldToEdit.FieldType);
                }

                return fieldToEdit;
            }
            else
            {
                CLIMessage("Invalid answer: {0}\n", answer);
            }

            return null;
        }

        private void ProcessSetOptionState(ConfigurationField field)
        {
            if (field == null)
            {
                CLIMessage("ERROR: Field is null while we entered SetOption state. Exiting.");
                mState = EditorState.Exit;
                return;
            }

            ProcessFieldEdit(field);

            field = null;
            mState = EditorState.PickOption;
        }

        private void ProcessEditArrayOptionState(ConfigurationField arrayField)
        {
            // TODO
            throw new NotImplementedException("TODO Arrays not yet supported");
        }

        private void ProcessEditListOptionStateGeneric<T>(ConfigurationFieldAccessor<List<T>> fieldAccessor)
            where T: new()
        {
            if (fieldAccessor == null)
            {
                throw new InternalErrorException("Field accessor is NULL");
            }

            List<T> list = fieldAccessor.Get();

            bool done = false;
            while (!done)
            {
                try
                {
                    CLIMessage("Editing a {0} list field - available fields:", typeof(T).ToString());
                    int counter = 0;
                    if (list.Count > 0)
                    {
                        foreach (object f in list)
                        {
                            CLIMessage("  {0}. {1}", counter, f.ToString());
                            counter++;
                        }
                    }
                    else
                    {
                        CLIMessage("  EMPTY");
                    }

                    string action = CLIQuery(false, "\nChoose action (add, del, move, edit, quit)");
                    switch (action)
                    {
                    case "add":
                        ProcessListAdd(list);
                        break;
                    case "del":
                        ProcessListRemove(list);
                        break;
                    case "move":
                        ProcessListMove(list);
                        break;
                    case "edit":
                        ProcessListEdit(fieldAccessor, list);
                        break;
                    case "quit":
                        mState = EditorState.PickOption;
                        done = true;
                        break;
                    default:
                        CLIMessage("Unrecognized option: {0}", action);
                        break;
                    }
                }
                catch (OperationAbortedException)
                {
                    CLIMessage("Selected action was aborted.\n");
                }
            }
        }

        private void ProcessEditListOptionState(ConfigurationField listField)
        {
            if (listField == null)
            {
                CLIMessage("ERROR: Field is null while we entered EditListOption state. Exiting.");
                mState = EditorState.PickOption;
                return;
            }

            if (listField.FieldType != ConfigurationFieldType.List)
            {
                CLIMessage("ERROR: Field is not a List while we entered EditListOption state. Exiting.");
                mState = EditorState.PickOption;
                return;
            }

            if (listField.UnderlyingType == null)
            {
                CLIMessage("ERROR: Field's underlying type is not set.");
                mState = EditorState.PickOption;
                return;
            }

            Type underlyingType = listField.UnderlyingType;
            if (underlyingType == typeof(AudioPlay.AudioTrigger))
            {
                ProcessEditListOptionStateGeneric(listField as ConfigurationFieldAccessor<List<AudioPlay.AudioTrigger>>);
            }
            else
            {
                CLIMessage("ERROR: Unrecognized underlying type {0}. Maybe something needs to be added here.", listField.UnderlyingType.ToString());
                mState = EditorState.PickOption;
                return;
            }
        }


        public WidgetConfigurationCLIEditor(string name, ConfigurationBase configuration, CLIMessageProxy cli)
        {
            mPrintedName = name;
            mCLI = cli;
            mConfiguration = configuration;

            // verify if we have any configuration to edit
            if (mConfiguration.GetFields().Count == 0)
                throw new ArgumentException(string.Format("Requested Widget has no configuration options"));
        }

        // This takes over CLI from main CLI code and provides a sub-UI
        public void MainLoop()
        {
            mState = EditorState.PickOption;
            ConfigurationField mField = null;
            while (mState != EditorState.Exit)
            {
                switch (mState)
                {
                case EditorState.PickOption:
                    mField = ProcessPickOptionState();
                    break;
                case EditorState.SetSimpleOption:
                {
                    ProcessSetOptionState(mField);
                    break;
                }
                case EditorState.EditArrayOption:
                case EditorState.EditEnumerableOption:
                {
                    CLIMessage("ERROR: TODO");
                    mState = EditorState.PickOption;
                    break;
                }
                case EditorState.EditListOption:
                {
                    ProcessEditListOptionState(mField);
                    break;
                }
                default:
                    CLIMessage("ERROR: Entered invalid state. Exiting.");
                    mState = EditorState.Exit;
                    break;
                }
            }
        }
    }
}