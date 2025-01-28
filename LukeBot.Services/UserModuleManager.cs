using System;
using System.Collections.Generic;


namespace LukeBot.Services
{
    // TODO do we need this??
    public class UserModuleManager
    {
        private struct UserModuleManagerEntry
        {
            public Guid userGuid;
            public Dictionary<string, IUserModule> modules = new();

            public UserModuleManagerEntry(Guid guid)
            {
                userGuid = guid;
            }
        };

        private Dictionary<Guid, UserModuleManagerEntry> mUsers = new();
        private Dictionary<string, UserModuleDescriptor> mDescriptors = new();

        private UserModuleDescriptor GetModuleDescriptor(string moduleType)
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
        private IUserModule Create(string type, string lbUser)
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
            if (umd == null)
            {
                // assume this service has no user module
                return;
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

        public void EnableModuleForUser(Guid userGuid, string username, string moduleType)
        {
            UserModuleManagerEntry entry;
            if (!mUsers.TryGetValue(userGuid, out entry))
            {
                entry = new(userGuid);
                mUsers.Add(userGuid, entry);
            }

            entry.modules.TryAdd(moduleType, Create(moduleType, username));
        }
    }
}
