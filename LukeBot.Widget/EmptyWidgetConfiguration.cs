using System.Collections.Generic;
using LukeBot.Logging;
using LukeBot.Widget.Common;


namespace LukeBot.Widget
{
    internal class EmptyWidgetConfiguration: WidgetConfiguration
    {
        static EmptyWidgetConfiguration()
        {
            WidgetConfiguration.RegisterAllocator(Constants.EMPTY_WIDGET_CONFIGURATION_NAME, () => new EmptyWidgetConfiguration());
        }

        public EmptyWidgetConfiguration()
            : base(Constants.EMPTY_WIDGET_CONFIGURATION_NAME)
        {
        }
    }
}
