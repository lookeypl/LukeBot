using System;
using System.Collections.Generic;
using System.Diagnostics;
using LukeBot.Communication;
using LukeBot.Communication.Common.Intercom;
using LukeBot.Config;
using LukeBot.Logging;
using LukeBot.Services;
using LukeBot.Spotify;
using LukeBot.User;
using CommonConstants = LukeBot.Common.Constants;
using CommonUtils = LukeBot.Common.Utils;


namespace LukeBot.Spotify.Impl
{
    public class SpotifyService: ISpotifyService
    {
        private Dictionary<string, SpotifyUserModule> mModules = new();


        // Config interactions //

        private void LoadUserModulesFromConfig()
        {
            string[] users = CommonUtils.GetUserModulesFromConfig(CommonConstants.SPOTIFY_SERVICE_NAME);

            foreach (string u in users)
            {
                try
                {
                    IUserService userService = Service.Get(CommonConstants.USER_SERVICE_NAME) as IUserService;
                    CreateModule(userService.GetUser(u));
                }
                catch (System.Exception e)
                {
                    Logger.Log().Error("Failed to initialize Spotify user module for user {0}: {1}", u, e.Message);
                    Logger.Log().Error("Spotify user module for user {0} will be skipped on this load.", u);
                    Logger.Log().Trace("Stack trace:\n{0}", e.StackTrace);
                }
            }
        }


        // Intercom interface

        private ResponseBase Intercom_ResponseAllocator(MessageBase msg)
        {
            switch (msg.Message)
            {
            case Messages.ADD_SONG_TO_QUEUE: return new AddSongToQueueResponse();
            }

            Debug.Assert(false, "Message should be validated by now - should not happen");
            return new ResponseBase();
        }

        private void Intercom_AddSongToQueueDelegate(MessageBase mb, ref ResponseBase rb)
        {
            AddSongToQueueMsg message = (AddSongToQueueMsg)mb;
            AddSongToQueueResponse response = (AddSongToQueueResponse)rb;

            try
            {
                API.Spotify.Track t = mModules[message.User].AddSongToQueue(message.URL);
                response.Artist = t.artists[0].name;
                response.Title = t.name;
                response.SignalSuccess();
            }
            catch (Exception e)
            {
                response.SignalError(string.Format("{0}", e.Message));
            }
        }


        // IUserModuleFactory interfaces //

        public IUserModule CreateModule(IUserContext user)
        {
            SpotifyUserModule module = new(user.GetUsername());
            module.Run();

            mModules.Add(user.GetUsername(), module);
            CommonUtils.AddUserModuleToConfig(CommonConstants.SPOTIFY_SERVICE_NAME, user.GetUsername());
            return module;
        }

        public IUserModule GetModule(IUserContext user)
        {
            return mModules[user.GetUsername()];
        }

        public void DestroyModule(IUserContext user)
        {
            if (mModules.TryGetValue(user.GetUsername(), out SpotifyUserModule module))
            {
                CommonUtils.RemoveUserModuleFromConfig(CommonConstants.SPOTIFY_SERVICE_NAME, user.GetUsername());

                module.RequestShutdown();
                module.WaitForShutdown();

                mModules.Remove(user.GetUsername());
            }
        }


        // Publics //

        private SpotifyService()
        {
        }

        public static ISpotifyService Create()
        {
            return new SpotifyService();
        }

        public string GetServiceName()
        {
            return CommonConstants.SPOTIFY_SERVICE_NAME;
        }

        public IEnumerable<string> GetServiceDependencies()
        {
            return new List<String>{ CommonConstants.USER_SERVICE_NAME };
        }

        public void Run()
        {
            LoadUserModulesFromConfig();
        }

        public void RequestShutdown()
        {
            foreach (SpotifyUserModule um in mModules.Values)
            {
                um.RequestShutdown();
            }
        }

        public void WaitForShutdown()
        {
            foreach (SpotifyUserModule um in mModules.Values)
            {
                um.WaitForShutdown();
            }
        }
    }
}