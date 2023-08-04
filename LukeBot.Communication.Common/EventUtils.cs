using System;
using System.Linq;
using System.Reflection;


namespace LukeBot.Communication.Common
{
    public class EventUtils
    {
        static Assembly[] mCommonAssemblies = null;

        public static Type GetEventTypeArgs(string typeStr)
        {
            if (mCommonAssemblies == null)
            {
                mCommonAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(asm => asm.GetName().Name.StartsWith("LukeBot"))
                    .Where(asm => !asm.GetName().Name.Equals("LukeBot.Common"))
                    .Where(asm => asm.GetName().Name.EndsWith("Common"))
                    .ToArray();
            }

            foreach (Assembly asm in mCommonAssemblies)
            {
                Type t = asm.GetType(asm.GetName().Name + '.' + typeStr + "Args", false);
                if (t != null)
                {
                    return t;
                }
            }

            return null;
        }
    }
}