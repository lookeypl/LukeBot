using System;
using System.Collections.Generic;
using LukeBot.Common;
using LukeBot.Services;
using LukeBot.User.Common;
using LukeBot.Widget.Common;


namespace LukeBot
{
    internal class WidgetConfigurationCLIEditor
    {
        private enum EditorState
        {
            PickOption = 0,
            SetOption,
            ListSetOption,
            ListAdd,
            ListDel,
            ListMove,
            Exit,
        };

        private string mWidgetID = "";
        private string mPrintedName = "";
        private IWidgetConfiguration mConfiguration = null;
        private CLIMessageProxy mCLI = null;
        private EditorState mState = EditorState.PickOption;

        private IWidgetService GetWidgetService()
        {
            return Service.Get(Constants.WIDGET_SERVICE_NAME) as IWidgetService;
        }

        private IWidgetUserModule GetWidgetUserModule()
        {
            return GetWidgetService().GetModuleByWidgetUUID(mWidgetID) as IWidgetUserModule;
        }

        private IWidgetConfiguration GetWidgetConfiguration()
        {
            return GetWidgetUserModule().GetWidgetConfiguration(mWidgetID);
        }

        private void CLIMessage(string msg, params object[] args)
        {
            mCLI.Message(String.Format(msg, args));
        }

        private string CLIQuery(bool mask, string msg, params object[] args)
        {
            return mCLI.Query(mask, String.Format(msg, args));
        }

        public WidgetConfigurationCLIEditor(string widgetID, CLIMessageProxy cli)
            : this(widgetID, "", cli)
        {
        }

        public WidgetConfigurationCLIEditor(string widgetID, string friendlyName, CLIMessageProxy cli)
        {
            mWidgetID = widgetID;
            mPrintedName = (friendlyName != null && friendlyName.Length > 0) ? friendlyName : widgetID;
            mCLI = cli;
            mConfiguration = GetWidgetConfiguration();

            // verify if we have any configuration to edit
            if (mConfiguration.GetFields().Count == 0)
                throw new ArgumentException(string.Format("Requested Widget has no configuration options"));
        }

        public WidgetConfigurationField ProcessPickOptionState()
        {
            Dictionary<string, WidgetConfigurationField> fields = mConfiguration.GetFields();

            CLIMessage("Editing configuration of widget {0}", mPrintedName);
            CLIMessage("Available configuration options:");
            foreach (WidgetConfigurationField field in fields.Values)
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
            else if (fields.TryGetValue(answer, out WidgetConfigurationField fieldToEdit))
            {
                if (fieldToEdit.Type.IsGenericType && fieldToEdit.Type.GetGenericTypeDefinition() == typeof(List<>))
                    mState = EditorState.ListSetOption;
                else
                    mState = EditorState.SetOption;

                return fieldToEdit;
            }
            else
            {
                CLIMessage("Invalid answer: {0}\n", answer);
            }

            return null;
        }

        public void ProcessSetOptionState(WidgetConfigurationField field)
        {
            string newValue = CLIQuery(false, "Enter new value for {0}", field.Name);

            try
            {
                field.SetFromString(newValue);
            }
            catch (WidgetConfigurationFieldException e)
            {
                CLIMessage("Failed to set new value {0} for configuration field {1}: {2}", newValue, field.Name, e.Message);
            }

            field = null;
            mState = EditorState.PickOption;
        }

        public void ProcessListSetOptionState(WidgetConfigurationField listField)
        {
            List<IWidgetConfigurationEditableField> list = listField.Get<List<IWidgetConfigurationEditableField>>();

            bool done = false;
            while (!done)
            {
                CLIMessage("Editing a list field - available fields:");

                foreach (IWidgetConfigurationEditableField f in list)
                {
                    CLIMessage("  {0}", f.ToString());
                }

                string action = CLIQuery(false, "Choose action (add, del, move, quit):");
                switch (action)
                {
                case "add":
                    mState = EditorState.ListAdd;
                    done = true;
                    break;
                case "del":
                    mState = EditorState.ListDel;
                    done = true;
                    break;
                case "move":
                    mState = EditorState.ListMove;
                    done = true;
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
        }

        public void ProcessListAdd(WidgetConfigurationField field)
        {

        }

        public void ProcessListDel(WidgetConfigurationField field)
        {

        }

        public void ProcessListMove(WidgetConfigurationField field)
        {

        }

        // This takes over CLI from main CLI code and provides a sub-UI
        public void MainLoop()
        {
            CLIMessage("Starting Widget Configuration editor");

            mState = EditorState.PickOption;
            WidgetConfigurationField mField = null;
            while (mState != EditorState.Exit)
            {
                switch (mState)
                {
                case EditorState.PickOption:
                    mField = ProcessPickOptionState();
                    break;
                case EditorState.SetOption:
                {
                    if (mField == null)
                    {
                        CLIMessage("ERROR: Field is null while we entered SetOption state. Exiting.");
                        mState = EditorState.Exit;
                        break;
                    }

                    ProcessSetOptionState(mField);
                    break;
                }
                case EditorState.ListSetOption:
                {
                    if (mField == null)
                    {
                        CLIMessage("ERROR: Field is null while we entered ListSetOption state. Exiting.");
                        mState = EditorState.Exit;
                        break;
                    }

                    if (mField.Type.IsGenericType && mField.Type.GetGenericTypeDefinition() == typeof(List<>))
                    {
                        CLIMessage("ERROR: Field is not a List while we entered ListSetOption state. Exiting.");
                        mState = EditorState.Exit;
                        break;
                    }

                    ProcessListSetOptionState(mField);
                    break;
                }
                case EditorState.ListAdd:
                {
                    if (mField == null)
                    {
                        CLIMessage("ERROR: Field is null while we entered ListAdd state. Exiting.");
                        mState = EditorState.Exit;
                        break;
                    }

                    if (mField.Type.IsGenericType && mField.Type.GetGenericTypeDefinition() == typeof(List<>))
                    {
                        CLIMessage("ERROR: Field is not a List while we entered ListAdd state. Exiting.");
                        mState = EditorState.Exit;
                        break;
                    }

                    ProcessListAdd(mField);
                    break;
                }
                case EditorState.ListDel:
                {
                    if (mField == null)
                    {
                        CLIMessage("ERROR: Field is null while we entered ListDel state. Exiting.");
                        mState = EditorState.Exit;
                        break;
                    }

                    if (mField.Type.IsGenericType && mField.Type.GetGenericTypeDefinition() == typeof(List<>))
                    {
                        CLIMessage("ERROR: Field is not a List while we entered ListDel state. Exiting.");
                        mState = EditorState.Exit;
                        break;
                    }

                    ProcessListDel(mField);
                    break;
                }
                case EditorState.ListMove:
                {
                    if (mField == null)
                    {
                        CLIMessage("ERROR: Field is null while we entered ListMove state. Exiting.");
                        mState = EditorState.Exit;
                        break;
                    }

                    if (mField.Type.IsGenericType && mField.Type.GetGenericTypeDefinition() == typeof(List<>))
                    {
                        CLIMessage("ERROR: Field is not a List while we entered ListMove state. Exiting.");
                        mState = EditorState.Exit;
                        break;
                    }

                    ProcessListMove(mField);
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