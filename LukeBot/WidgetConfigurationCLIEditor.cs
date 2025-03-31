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
            Exit,
        };

        private string mWidgetID = "";
        private string mPrintedName = "";
        private IWidgetConfiguration mConfiguration = null;
        private CLIMessageProxy mCLI = null;
        private EditorState mState = EditorState.PickOption;
        private WidgetConfigurationField mFieldToEdit = null;

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

        public void ProcessPickOptionState()
        {
            Dictionary<string, WidgetConfigurationField> fields = mConfiguration.GetFields();

            CLIMessage("Editing configuration of widget {0}", mPrintedName);
            CLIMessage("Available configuration options:");
            foreach (WidgetConfigurationField field in fields.Values)
            {
                CLIMessage("  {0} - {1}", field.Name, field.GetValueString());
            }

            string answer = CLIQuery(false, "Pick option to edit by name (or Q to exit)");

            if (answer == "Q" || answer == "q")
            {
                mState = EditorState.Exit;
            }
            else if (fields.TryGetValue(answer, out mFieldToEdit))
            {
                mState = EditorState.SetOption;
            }
            else
            {
                CLIMessage("Invalid answer: {0}", answer);
            }
        }

        public void ProcessSetOptionState()
        {
            string newValue = CLIQuery(false, "Enter new value for {0}", mFieldToEdit.Name);

            try
            {
                mFieldToEdit.SetFromString(newValue);
            }
            catch (WidgetConfigurationFieldException e)
            {
                CLIMessage("Failed to set new value {0} for configuration field {1}: {2}", newValue, mFieldToEdit.Name, e.Message);
            }

            mFieldToEdit = null;
            mState = EditorState.PickOption;
        }

        // This takes over CLI from main CLI code and provides a sub-UI
        public void MainLoop()
        {
            CLIMessage("Starting Widget Configuration editor");

            mState = EditorState.PickOption;
            while (mState != EditorState.Exit)
            {
                switch (mState)
                {
                case EditorState.PickOption: ProcessPickOptionState(); break;
                case EditorState.SetOption: ProcessSetOptionState(); break;
                default:
                    break;
                }
            }
        }
    }
}