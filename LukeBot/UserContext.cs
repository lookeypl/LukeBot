using System;
using System.Collections.Generic;
using LukeBot.Common;
using LukeBot.Config;
using LukeBot.Globals;


namespace LukeBot
{
    class UserContext
    {
        public string Username { get; private set; }

        private const string PROP_STORE_MODULES_DOMAIN = "modules";
        private const string PROP_STORE_WIDGETS_DOMAIN = "widgets";

        private readonly Dictionary<string, Func<string, IModule>> mModuleAllocators = new Dictionary<string, Func<string, IModule>>
        {
            {"twitch", (string user) => GlobalModules.Twitch.JoinChannel(user)},
            {"spotify", (string user) => null /* TODO */},
            {"widget", (string user) => GlobalModules.Widget.LoadWidgetUserModule(user)},
        };

        private List<IModule> mModules = null;

        private class ModuleDesc
        {
            public string moduleType { get; set; }
        }


        private void AddModuleToConfig(string module)
        {
            string modulesProp = Utils.FormConfName(
                Constants.PROP_STORE_USER_DOMAIN, Username, PROP_STORE_MODULES_DOMAIN
            );

            string[] modules;
            if (!Conf.TryGet<string[]>(modulesProp, out modules))
            {
                modules = new string[1];
                modules[0] = module;
                Conf.Add(modulesProp, Property.Create<string[]>(modules));
                return;
            }

            Array.Resize(ref modules, modules.Length + 1);
            modules[modules.Length - 1] = module;
            Array.Sort<string>(modules);
            Conf.Modify<string[]>(modulesProp, modules);
        }

        private void LoadModulesFromConfig()
        {
            string modulesProp = Utils.FormConfName(
                Constants.PROP_STORE_USER_DOMAIN, Username, PROP_STORE_MODULES_DOMAIN
            );

            string[] modules;
            if (!Conf.TryGet<string[]>(modulesProp, out modules))
            {
                modules = new string[0];
            }

            foreach (string m in modules)
            {
                try
                {
                    // here we ignore the returned module and do not start it
                    // RunModules() will be called later and will kickstart it for us
                    LoadModule(m);
                }
                catch (System.Exception e)
                {
                    Logger.Log().Error(String.Format("Failed to initialize module {0} for user {1}: {2}",
                        m, Username, e.Message));
                    Logger.Log().Error(String.Format("Module {0} for user {1} will be skipped on this load.",
                        m, Username));
                }
            }
        }

        private void RemoveModuleFromConfig(string module)
        {
            string modulesProp = Utils.FormConfName(
                Constants.PROP_STORE_USER_DOMAIN, Username, PROP_STORE_MODULES_DOMAIN
            );

            string[] modules;
            if (!Conf.TryGet<string[]>(modulesProp, out modules))
            {
                return;
            }

            modules = Array.FindAll<string>(modules, m => m != module);
            if (modules.Length == 0)
                Conf.Remove(modulesProp);
            else
                Conf.Modify<string[]>(modulesProp, modules);
        }


        public UserContext(string user)
        {
            Username = user;
            mModules = new List<IModule>();

            Logger.Log().Info("Loading required modules for user {0}", Username);
            LoadModulesFromConfig();

            Logger.Log().Info("Loaded LukeBot user {0}", Username);
        }

        private IModule LoadModule(string moduleType)
        {
            IModule m = mModuleAllocators[moduleType](Username);
            mModules.Add(m);
            return m;
        }

        private void UnloadModule(string moduleType)
        {
            IModule m = mModules.Find(m => m.GetModuleName() == moduleType);
            m.RequestShutdown();
            m.WaitForShutdown();

            mModules.Remove(m);
        }

        public void EnableModule(string module)
        {
            if (mModules.Exists(m => m.GetModuleName() == module))
            {
                throw new ModuleEnabledException(module, Username);
            }

            IModule m = LoadModule(module);
            AddModuleToConfig(module);

            m.Run();
        }

        public void DisableModule(string module)
        {
            if (!mModules.Exists(m => m.GetModuleName() == module))
            {
                throw new ModuleDisabledException(module, Username);
            }

            UnloadModule(module);
            RemoveModuleFromConfig(module);
        }

        public void RunModules()
        {
            Logger.Log().Info("Running LukeBot modules for user {0}", Username);
            foreach (IModule m in mModules)
                m.Run();
        }

        public void RequestModuleShutdown()
        {
            foreach (IModule m in mModules)
                m.RequestShutdown();
        }

        public void WaitForModulesShutdown()
        {
            foreach (IModule m in mModules)
                m.WaitForShutdown();
        }
    }
}
