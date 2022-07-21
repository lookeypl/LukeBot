using LukeBot.Common;


namespace LukeBot.API
{
    public class UserToken: PromiseData
    {
        public string code { get; set; }
        //public List<string> scope { get; set; }
        public string state { get; set; }

        public override void Fill(PromiseData data)
        {
            UserToken r = data as UserToken;

            code = r.code;
            //scope = r.scope;
            state = r.state;
        }
    }
}
