using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using LukeBot.Common;
using LukeBot.Services;
using LukeBot.Twitch.Command;
using LukeBot.User;
using LukeBot.Widget;
using LukeBot.Widget.Common;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc.TagHelpers;
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
        private MethodInfo mListEditGenericMethodInfo = null;

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
                string answer = CLIQuery(false, query + " (range {0}-{1}, \"Q\" to abort)", min, max);

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
                else if (answer == "Q")
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

        private int ValidateOrFetchSpot(int spot, int limit, string queryMsg)
        {
            if (spot > limit)
            {
                throw new ArgumentException(String.Format("Spot {0} is larger than limit {1}", spot, limit));
            }

            int ret = spot;
            if (ret < 0)
            {
                if (limit > 0)
                {
                    ret = CLIQueryNumber(0, limit, queryMsg);
                }
                else ret = 0;
            }

            return ret;
        }

        private int ProcessListAdd<T>(List<T> list, int spot)
            where T : new()
        {
            spot = ValidateOrFetchSpot(spot, list.Count, "Spot for new entry");
            list.Insert(spot, new T());
            return spot;
        }

        private int ProcessListAdd(List<string> list, int spot)
        {
            spot = ValidateOrFetchSpot(spot, list.Count, "Spot for new entry");
            list.Insert(spot, "");
            return spot;
        }

        private void ProcessListRemove<T>(List<T> list, int spot)
        {
            if (list.Count < 1)
            {
                CLIMessage("Nothing to remove");
                return;
            }

            spot = ValidateOrFetchSpot(spot, list.Count - 1, "Which entry to remove");
            list.RemoveAt(spot);
        }

        private void ProcessListMove<T>(List<T> list, int who, int where)
        {
            if (list.Count < 1)
            {
                CLIMessage("Nothing to move");
                return;
            }

            who = ValidateOrFetchSpot(who, list.Count - 1, "Select element to move");
            where = ValidateOrFetchSpot(where, list.Count - 1, "Move to which index");
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

        private void ProcessListEdit<T>(ConfigurationField field, List<T> list, int spot, string newValue)
        {
            if (list.Count < 1)
            {
                CLIMessage("Nothing to edit");
                return;
            }

            spot = ValidateOrFetchSpot(spot, list.Count - 1, "Select element to edit");

            switch (field.UnderlyingFieldType)
            {
            case ConfigurationFieldType.Simple:
            case ConfigurationFieldType.String:
                if (newValue == "")
                {
                    newValue = CLIQuery(false, "New value");
                }

                list[spot] = (T)Convert.ChangeType(newValue, typeof(T));
                break;
            case ConfigurationFieldType.Class:
                if (!field.UnderlyingType.IsAssignableTo(typeof(ConfigurationBase)))
                {
                    throw new InternalErrorException("Cannot edit field, it must inherit Configuration<T>.");
                }

                new WidgetConfigurationCLIEditor(field.Name + "[" + spot + "]", list[spot] as ConfigurationBase, mCLI).MainLoop();
                break;
            }
        }

        private void PrintListContents<T>(ConfigurationFieldAccessor<List<T>> listAccessor)
        {
            CLIMessage("Available list fields:");

            List<T> list = listAccessor.Get();

            int counter = 0;
            if (list.Count > 0)
            {
                foreach (object o in list)
                {
                    CLIMessage("  {0}. {1}", counter, o.ToString());
                    counter++;
                }
            }
            else
            {
                CLIMessage("  EMPTY");
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

            if (answer == "Q")
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
                    if (!fieldToEdit.Type.IsAssignableTo(typeof(ConfigurationBase)))
                    {
                        throw new InternalErrorException("Cannot edit field, it must inherit Configuration<T>.");
                    }

                    new WidgetConfigurationCLIEditor(fieldToEdit.Name, fieldToEdit.GetRawObject() as ConfigurationBase, mCLI).MainLoop();
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

        private void ProcessListHelp()
        {
            CLIMessage("\nAvailable actions are: add, del, move, edit, help, quit");
            CLIMessage("Actions can be followed by an index stating which element to affect.");
            CLIMessage("All indexes are checked against list's current length. \"add\" allows to");
            CLIMessage("add an element at the end of the list by providing the first out-of-bounds index.");
            CLIMessage("Examples:");
            CLIMessage("  add       - simply adds an element, querying for spot where to insert the element to");
            CLIMessage("  add 3     - adds a new element at index 3 ");
            CLIMessage("  del 2     - removes an element at index 2");
            CLIMessage("  move 2 5  - moves element at index 2 to index 5 preserving existing element order");
            CLIMessage("  edit 1    - edits an element at index 1");
        }

        private void QueryListUserAction(out string action, out int spot, out int where, out string remainder)
        {
            action = "";
            spot = -1;
            where = -1;
            remainder = "";

            string a = CLIQuery(false, "\nChoose action (add, del, move, edit, help, quit)");
            string[] tokenizedAction = a.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (tokenizedAction.Length < 1)
            {
                throw new ArgumentException("Provide an action");
            }

            action = tokenizedAction[0];
            int currentIdx = 1;
            while (currentIdx < tokenizedAction.Length)
            {
                if (action == "move")
                {
                    if (currentIdx == 1 || currentIdx == 2)
                    {
                        int val;
                        if (!Int32.TryParse(tokenizedAction[currentIdx], out val))
                        {
                            remainder = String.Join(' ', tokenizedAction[currentIdx..]);
                            break;
                        }

                        if (currentIdx == 1) spot = val;
                        if (currentIdx == 2) where = val;
                    }
                    else
                    {
                        remainder = String.Join(' ', tokenizedAction[currentIdx..]);
                        break;
                    }
                }
                else
                {
                    if (currentIdx == 1)
                    {
                        if (!Int32.TryParse(tokenizedAction[currentIdx], out spot))
                        {
                            remainder = String.Join(' ', tokenizedAction[currentIdx..]);
                            break;
                        }
                    }
                }

                currentIdx++;
            }
        }

        private void ProcessEditListOptionStateGeneric<T>(ConfigurationFieldAccessor<List<T>> listAccessor)
            where T : new()
        {
            List<T> list = listAccessor.Get();

            bool done = false;
            while (!done)
            {
                try
                {
                    PrintListContents(listAccessor);
                    QueryListUserAction(out string action, out int spot, out int where, out string remainder);

                    switch (action)
                    {
                    case "add":
                        spot = ProcessListAdd(list, spot);
                        if (remainder != "")
                        {
                            ProcessListEdit(listAccessor, list, spot, remainder);
                        }
                        break;
                    case "del":
                        ProcessListRemove(list, spot);
                        break;
                    case "move":
                        ProcessListMove(list, spot, where);
                        break;
                    case "edit":
                        ProcessListEdit(listAccessor, list, spot, remainder);
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
                catch (ArgumentException e)
                {
                    CLIMessage("Invalid input: " + e.Message);
                }
                catch (OperationAbortedException)
                {
                    CLIMessage("Selected action was aborted.\n");
                }
            }
        }

        private void ProcessEditListOptionStateString(ConfigurationFieldAccessor<List<string>> listAccessor)
        {
            List<string> list = listAccessor.Get();

            bool done = false;
            while (!done)
            {
                try
                {
                    PrintListContents(listAccessor);
                    QueryListUserAction(out string action, out int spot, out int where, out string remainder);

                    switch (action)
                    {
                    case "add":
                        ProcessListAdd(list, spot);
                        if (remainder != "")
                        {
                            ProcessListEdit(listAccessor, list, spot, remainder);
                        }
                        break;
                    case "del":
                        ProcessListRemove(list, spot);
                        break;
                    case "move":
                        ProcessListMove(list, spot, where);
                        break;
                    case "edit":
                        ProcessListEdit(listAccessor, list, spot, remainder);
                        break;
                    case "help":
                        ProcessListHelp();
                        CLIMessage("\nFor this String-type list you can also provide the contents of a String after the command");
                        CLIMessage("Examples:");
                        CLIMessage("    add             - queries where to add a string, then queries for the string to add");
                        CLIMessage("    add 4           - queries fro the string to add, then adds an string at index 4");
                        CLIMessage("    add Test string - queries where to add a string, afterwards adds \"Test string\" at that spot");
                        CLIMessage("    add 5 Test test - adds \"Test test\" string at index 5");
                        CLIMessage("    edit 3 ab cd    - Replaces string at index 3 with \"ab cd\"");
                        CLIMessage("    edit 1          - Queries for replacement string, then replaces string at index 1 with it");
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
                catch (ArgumentException e)
                {
                    CLIMessage("Invalid input: " + e.Message);
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

            switch (listField.UnderlyingFieldType)
            {
            case ConfigurationFieldType.String:
                ProcessEditListOptionStateString(listField as ConfigurationFieldAccessor<List<string>>);
                break;
            default:
                MethodInfo editMethod = mListEditGenericMethodInfo.MakeGenericMethod(new Type[] { listField.UnderlyingType });
                if (editMethod == null)
                    throw new InternalErrorException("Failed to make generic editor method for list field with type {0}", listField.UnderlyingType.ToString());
                editMethod.Invoke(this, new[] { listField });
                break;
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

            Type accessorTypeGeneric = typeof(ConfigurationFieldAccessor<>);
            MethodInfo[] methods = typeof(WidgetConfigurationCLIEditor).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance);
            mListEditGenericMethodInfo = methods.Single(m => m.Name == "ProcessEditListOptionStateGeneric" &&
                                                             m.IsGenericMethodDefinition &&
                                                             m.GetParameters().Length == 1 &&
                                                             m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == accessorTypeGeneric);
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
                    ProcessSetOptionState(mField);
                    break;
                case EditorState.EditArrayOption:
                case EditorState.EditEnumerableOption:
                    CLIMessage("ERROR: TODO");
                    mState = EditorState.PickOption;
                    break;
                case EditorState.EditListOption:
                    ProcessEditListOptionState(mField);
                    break;
                default:
                    CLIMessage("ERROR: Entered invalid state. Exiting.");
                    mState = EditorState.Exit;
                    break;
                }
            }
        }
    }
}