using System;
using System.Collections.Generic;
using LukeBot.Common;
using LukeBot.Config;
using LukeBot.Globals;
using LukeBot.Logging;
using LukeBot.Module;


namespace LukeBot
{
    class UserContext
    {
        public string Username { get; private set; }

        private const string PROP_STORE_MODULES_DOMAIN = "modules";
        private const string PROP_STORE_WIDGETS_DOMAIN = "widgets";

        private Dictionary<ModuleType, IUserModule> mModules = new();

        private void AddModuleToConfig(string module)
        {
            Path modulesProp = Path.Start()
                .Push(Constants.PROP_STORE_USER_DOMAIN)
                .Push(Username)
                .Push(PROP_STORE_MODULES_DOMAIN);

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
            Path modulesProp = Path.Start()
                .Push(Constants.PROP_STORE_USER_DOMAIN)
                .Push(Username)
                .Push(PROP_STORE_MODULES_DOMAIN);

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
                    LoadModule(m.GetModuleTypeEnum());
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
            Path modulesProp = Path.Start()
                .Push(Constants.PROP_STORE_USER_DOMAIN)
                .Push(Username)
                .Push(PROP_STORE_MODULES_DOMAIN);

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

        private IUserModule LoadModule(ModuleType type)
        {
            IUserModule m = GlobalModules.UserModuleManager.Create(type, Username);
            mModules.Add(type, m);
            return m;
        }

        private void UnloadModule(ModuleType type)
        {
            IUserModule m = mModules[type];

            GlobalModules.UserModuleManager.Unload(m);
            m.RequestShutdown();
            m.WaitForShutdown();

            mModules.Remove(type);
        }


        public UserContext(string user)
        {
            Username = user;

            Logger.Log().Info("Loading required modules for user {0}", Username);
            LoadModulesFromConfig();

            Logger.Log().Info("Loaded LukeBot user {0}", Username);
        }

        public void EnableModule(ModuleType module)
        {
            if (mModules.ContainsKey(module))
            {
                throw new ModuleEnabledException(module, Username);
            }

            IUserModule m = LoadModule(module);
            AddModuleToConfig(module.ToConfString());

            m.Run();
        }

        public void DisableModule(ModuleType module)
        {
            if (!mModules.ContainsKey(module))
            {
                throw new ModuleDisabledException(module, Username);
            }

            UnloadModule(module);
            RemoveModuleFromConfig(module.ToConfString());
        }

        public void RunModules()
        {
            Logger.Log().Info("Running LukeBot modules for user {0}", Username);
            foreach (IUserModule m in mModules.Values)
                m.Run();
        }

        public void RequestModuleShutdown()
        {
            foreach (IUserModule m in mModules.Values)
                m.RequestShutdown();
        }

        public void WaitForModulesShutdown()
        {
            foreach (IUserModule m in mModules.Values)
                m.WaitForShutdown();
        }
    }
}
