using System.Collections.Generic;
using LukeBot.Common;

namespace LukeBot.Endpoint
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
