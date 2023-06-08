using System.Collections.Generic;


namespace LukeBot.Module
{
    public class UserModuleManager
    {
        private Dictionary<ModuleType, UserModuleDescriptor> mDescriptors = new();

        private UserModuleDescriptor GetModuleDescriptor(ModuleType moduleType)
        {
            if (!mDescriptors.ContainsKey(moduleType))
            {
                throw new UnknownModuleException(moduleType);
            }

            return mDescriptors[moduleType];
        }

        /**
         * Create a new Module. This is called when enabling a new module for
         * already existing user.
         */
        public IUserModule Create(ModuleType type, string lbUser)
        {
            UserModuleDescriptor umd = GetModuleDescriptor(type);

            if (umd.LoadPrerequisite != null)
            {
                if (umd.LoadPrerequisite(lbUser) == false)
                {
                    throw new PrerequisiteNotMetException(type);
                }
            }

            return umd.Loader(lbUser);
        }

        public void Unload(IUserModule module)
        {
            UserModuleDescriptor umd = GetModuleDescriptor(module.GetModuleType());
            umd.Unloader(module);
        }

        public void RegisterUserModule(UserModuleDescriptor umd)
        {
            if (umd.Type == ModuleType.Unknown)
            {
                throw new InvalidDescriptorException("Module type is unknown");
            }

            if (mDescriptors.ContainsKey(umd.Type))
            {
                throw new ModuleAlreadyRegisteredException(umd.Type);
            }

            if (umd.Loader == null)
            {
                throw new InvalidDescriptorException(umd.Type, "Loader delegate is empty");
            }

            if (umd.Unloader == null)
            {
                throw new InvalidDescriptorException(umd.Type, "Unloader delegate is empty");
            }

            mDescriptors.Add(umd.Type, umd);
        }
    }
}
