using System.Linq;
using LukeBot.API;
using LukeBot.Logging;
using Command = LukeBot.Twitch.Common.Command;


namespace LukeBot.Twitch.Command
{
    public class Speak: ICommand
    {
        private Streamlabs.Voice voice = Streamlabs.Voice.Justin;

        public Speak(Command::Descriptor d)
            : base(d)
        {
        }

        public override string Execute(Command::User callerPrivilege, string[] args)
        {
            string text = string.Join(' ', args.Skip(1).ToArray());

            Streamlabs.TTS tts = Streamlabs.GetTTS(voice, text);
            if (!tts.IsSuccess)
            {
                Logger.Log().Error("SL TTS request failed: {0} ({1})", tts.code, tts.message.ReasonPhrase);
                return "";
            }

            if (!tts.success)
            {
                Logger.Log().Error("SL TTS request invalid");
                return "";
            }

            Logger.Log().Debug("{0}", tts.speak_url);
            return "";
        }

        public override void Edit(string newValue)
        {
        }

        public override Command::Descriptor ToDescriptor()
        {
            return new Command::Descriptor(mName, Command::Type.speak, mPrivilegeLevel, mEnabled, "");
        }
    }
}