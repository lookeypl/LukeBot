using System.Collections.Generic;


namespace LukeBot.Module
{
    public class UserModuleManager
    {
        private Dictionary<string, UserModuleDescriptor> mDescriptors = new();

        public IUserModule Allocate(string moduleName, string lbUser)
        {
            if (!mDescriptors.ContainsKey(moduleName))
            {
                throw new UnknownModuleException(moduleName);
            }

            UserModuleDescriptor umd = mDescriptors[moduleName];

            if (umd.LoadPrerequisite != null)
            {
                if (umd.LoadPrerequisite() == false)
                {
                    throw new PrerequisiteNotMetException(moduleName);
                }
            }

            return umd.Loader(lbUser);
        }

        public void RegisterUserModule(UserModuleDescriptor umd)
        {
            if (umd.ModuleName.Length == 0)
            {
                throw new InvalidDescriptorException("Module name is empty");
            }

            if (mDescriptors.ContainsKey(umd.ModuleName))
            {
                throw new ModuleAlreadyRegisteredException(umd.ModuleName);
            }

            if (umd.Loader == null)
            {
                throw new InvalidDescriptorException(umd.ModuleName, "Loader delegate is empty");
            }

            mDescriptors.Add(umd.ModuleName, umd);
        }
    }
}
