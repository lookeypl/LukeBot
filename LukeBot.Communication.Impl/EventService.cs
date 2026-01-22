using System.Collections.Generic;
using LukeBot.Common;
using LukeBot.Communication;
using LukeBot.Services;


namespace LukeBot.Communication.Impl
{
    /**
     * Event System entry point.
     *
     * This class allows for more sophisticated control over events. The underlying mechanism
     * uses the standard Event Handlers, but allows for some organization in how they're used.
     *
     * EventSystem collects Event Collections. By design, one Event Collection should be assigned
     * to one LukeBot user. In addition to that, there's a special "global" event collection which
     * should contain and manage all LukeBot-wide events.
     */
    public class EventService: IEventService
    {
        private Dictionary<string, EventCollection> mUserToCollection = new();

        private EventService()
        {
            // for Global events
            mUserToCollection.Add(Constants.LUKEBOT_USER_ID, new(Constants.LUKEBOT_USER_ID));
        }

        ~EventService()
        {
            mUserToCollection.Clear();
        }

        public static IEventService Create()
        {
            return new EventService();
        }

        public void AddUser(string lbUser)
        {
            mUserToCollection.Add(lbUser, new EventCollection(lbUser));
        }

        public void RemoveUser(string lbUser)
        {
            mUserToCollection.Remove(lbUser);
        }

        public IEventCollection User(string lbUser)
        {
            return mUserToCollection[lbUser];
        }

        public IEventCollection Global()
        {
            return mUserToCollection[Constants.LUKEBOT_USER_ID];
        }

        public string GetServiceDebugName()
        {
            return Common.Constants.EVENT_SERVICE_NAME;
        }

        public IEnumerable<string> GetServiceDependencies()
        {
            return null;
        }

        public void Run()
        {
            // noop
        }

        public void RequestShutdown()
        {
            // noop
        }

        public void WaitForShutdown()
        {
            // noop
        }
    }
}
