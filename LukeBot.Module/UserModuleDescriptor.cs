namespace LukeBot.Module
{
    public class UserModuleDescriptor
    {
        public delegate bool LoadPrerequisiteCheckDelegate(string lbUser);
        public delegate IUserModule ModuleLoader(string lbUser);

        /**
         * Name of the module that's being loaded.
         */
        public string ModuleName { get; set; }

        /**
         * Delegate checking for any prerequisites a module might need before loading
         * (ex. module-specific login to an external service).
         *
         * Returning "false" or throwing an exception will fail the user module load early. To
         *
         * This is an optional function and can be set to null when module has no prerequisites.
         */
        public LoadPrerequisiteCheckDelegate LoadPrerequisite { get; set; }

        /**
         * Delegate loading a user module and returning a reference to it.
         *
         * A returned module has to be initialized - after loading User Context will call its Run()
         * interface when required, assuming it is ready to work.
         *
         * This function is required.
         */
        public ModuleLoader Loader { get; set; }

        public UserModuleDescriptor()
        {
            ModuleName = "";
            LoadPrerequisite = null;
            Loader = null;
        }
    };
}