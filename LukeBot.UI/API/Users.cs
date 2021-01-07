using System.Collections.Generic;
using LukeBot.Common;

namespace LukeBot.UI
{
    class UsersResponse: ResponseBase
    {
        public List<UserItem> users { get; set; }

        public UsersResponse()
        {
            users = new List<UserItem>();
        }
    }
}
