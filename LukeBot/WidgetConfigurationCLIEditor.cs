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
            PickArrayElement,
            SetOption,
            Exit,
        };

        private string mWidgetID;
        private CLIMessageProxy mCLI;
        private EditorState mState;

        private IWidgetService GetWidgetService()
        {
            return Service.Get(Constants.WIDGET_SERVICE_NAME) as IWidgetService;
        }

        private IWidgetUserModule GetWidgetUserModule(IUserContext user)
        {
            return GetWidgetService().GetModule(user) as IWidgetUserModule;
        }

        private IWidgetConfiguration GetWidgetConfiguration()
        {
            return GetWidgetService().GetModuleByWidgetUUID(mWidgetID).GetWidgetConfiguration(mWidgetID);
        }

        public WidgetConfigurationCLIEditor(string widgetID, CLIMessageProxy cli)
        {
            mWidgetID = widgetID;
            mCLI = cli;
        }

        public void ProcessPickOptionState()
        {
            IWidgetConfiguration config = GetWidgetConfiguration();

            mCLI.Message("Available configuration options:");

            mCLI.Message("Pick option to edit:");
            string answer = mCLI.Query(false, "widget/" + mWidgetID + "> ");
        }

        public void ProcessPickArrayElementState()
        {

        }

        public void ProcessSetOptionState()
        {

        }

        // This takes over CLI from main CLI code and provides a sub-UI
        public void MainLoop()
        {
            mCLI.Message("Starting Widget Configuration editor");
            mCLI.Message("Press Ctrl+Q to return to main LukeBot CLI");

            mState = EditorState.PickOption;
            while (mState != EditorState.Exit)
            {
                switch (mState)
                {
                case EditorState.PickOption: ProcessPickOptionState(); break;
                case EditorState.PickArrayElement: ProcessPickArrayElementState(); break;
                case EditorState.SetOption: ProcessSetOptionState(); break;
                default:
                    break;
                }
            }
        }
    }
}