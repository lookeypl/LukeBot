using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using LukeBot.Communication.Common;
using LukeBot.Logging;
using LukeBot.Widget.Common;


namespace LukeBot.Widget
{
    public abstract class WidgetConfiguration : Configuration, IWidgetConfiguration
    {
        private IWidget mOwner = null;

        public WidgetConfiguration(string eventName)
            : base(eventName)
        {

        }

        public void SetOwner(IWidget owner)
        {
            mOwner = owner;

            if (mOwner != null)
            {
                foreach (ConfigurationField f in mFields.Values)
                {
                    f.mFieldSetDelegate = mOwner.NotifyConfigurationUpdate;
                }
            }
        }

        public static new WidgetConfiguration Deserialize(string confString)
        {
            return Configuration.Deserialize(confString) as WidgetConfiguration;
        }
    }
}
