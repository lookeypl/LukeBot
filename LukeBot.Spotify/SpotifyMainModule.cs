using System;
using System.Collections.Generic;
using LukeBot.Communication;
using LukeBot.Config;
using LukeBot.Interface;
using LukeBot.Module;
using CommonConstants = LukeBot.Common.Constants;


namespace LukeBot.Spotify
{
    public class SpotifyMainModule: IMainModule
    {
        private Dictionary<string, SpotifyUserModule> mModules = new();


        private bool UserModuleLoadPrerequisites(string lbUser)
        {
            Path userSpotifyLoginProp = Path.Start()
                .Push(CommonConstants.PROP_STORE_USER_DOMAIN)
                .Push(lbUser)
                .Push(CommonConstants.SPOTIFY_MODULE_NAME)
                .Push(Constants.PROP_STORE_SPOTIFY_LOGIN_PROP);

            string login;
            if (Conf.TryGet<string>(userSpotifyLoginProp, out login))
                return true; // quietly exit - login is already there, prerequisites are met

            // User login does not exist - query for it
            login = CLI.Instance.Query("Spotify login for user " + lbUser);
            if (login.Length == 0)
            {
                CLI.Instance.Message("No login provided");
                return false;
            }

            Conf.Add(userSpotifyLoginProp, Property.Create<string>(login));
            return true;
        }

        private IUserModule UserModuleLoader(string lbUser)
        {
            SpotifyUserModule m = new SpotifyUserModule(lbUser);
            mModules.Add(lbUser, m);
            return m;
        }

        private void UserModuleUnloader(IUserModule module)
        {
            SpotifyUserModule um = module as SpotifyUserModule;
            mModules.Remove(um.LBUser);
        }


        public SpotifyMainModule()
        {
            Comms.Communication.Register(CommonConstants.SPOTIFY_MODULE_NAME);
        }

        public UserModuleDescriptor GetUserModuleDescriptor()
        {
            UserModuleDescriptor umd = new UserModuleDescriptor();
            umd.Type = ModuleType.Spotify;
            umd.LoadPrerequisite = UserModuleLoadPrerequisites;
            umd.Loader = UserModuleLoader;
            umd.Unloader = UserModuleUnloader;
            return umd;
        }

        public void UpdateLoginForUser(string lbUser, string newLogin)
        {
            // TODO
            throw new NotImplementedException("Updating login for Spotify modules not yet implemented");
        }
    }
}