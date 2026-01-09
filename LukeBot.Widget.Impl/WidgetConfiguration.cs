using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using LukeBot.Common;
using LukeBot.Communication;
using LukeBot.Logging;


/*namespace LukeBot.Widget.Impl
{
    public abstract class WidgetConfiguration: Configuration
    {
        private IWidget mOwner = null;

        public WidgetConfiguration(string name)
            : base(name)
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
}*/
