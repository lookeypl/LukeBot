using System;
using System.Collections.Generic;
//using System.Linq;
using LukeBot.Common;
using LukeBot.Communication;
using LukeBot.Communication.Common;
using LukeBot.Logging;
using LukeBot.Twitch.Common;
using LukeBot.Widget.Common;
using Newtonsoft.Json;


namespace LukeBot.Widget
{
    /**
     * Widget responsible for playing an audio file available to it from the bot. That's
     * basically it, there is no more functionality.
     *
     * TODO consider adding some sort of "stop playing" event or whatever
     */
    public class AudioPlay: IWidget
    {
        public class AudioPlayInterrupt: EventArgsBase
        {
            public AudioPlayInterrupt()
                : base("AudioPlayInterrupt")
            {
            }
        }

        public class AudioPlayStartPlayback: EventArgsBase
        {
            public string File { get; set; }
            public bool RandomRepeat { get; set; }
            public int TotalLength { get; set; }
            public int MinInterval { get; set; }
            public int MaxInterval { get; set; }

            public AudioPlayStartPlayback(string file)
                : base("AudioPlayStartPlayback")
            {
                File = file;
                RandomRepeat = false;
            }
        }

        public class AudioTrigger: Configuration<AudioTrigger>
        {
            [ConfigurationField]
            public string RedemptionName = "FILLMEIN";
            [ConfigurationField]
            public List<string> Files = new();
            [ConfigurationField]
            public List<int> SomeNumbers = new();
            [ConfigurationField]
            public int RepeatLength = 0;
            [ConfigurationField]
            public int MinRepeatInterval = 0;
            [ConfigurationField]
            public int MaxRepeatInterval = 0;

            public override string ToString()
            {
                return String.Format("<{0}, [{1}], {2}, {3}, {4}>", RedemptionName, String.Join(", ", Files), RepeatLength, MinRepeatInterval, MaxRepeatInterval);
            }

            public override string ToShortString()
            {
                return RedemptionName;
            }
        }

        private Dictionary<string, AudioTrigger> mTriggers;

        public class Config: Configuration<Config>
        {
            [ConfigurationField]
            public List<AudioTrigger> Triggers = new();

            public Config()
            {
            }
        }

        private void AwaitEventCompletion()
        {
            if (!Connected)
                return;

            WidgetEventCompletionResponse resp = RecvFromWS<WidgetEventCompletionResponse>();
            if (resp == null)
            {
                Logger.Log().Warning("Widget's response was null - possibly connection was broken or is not connected");
                return;
            }

            if (resp.Status != 0)
            {
                Logger.Log().Warning("Widget failed to complete the event: {0}", resp.Reason);
            }
            else
            {
                Logger.Log().Debug("Widget completed event");
            }
        }

        private void OnChannelPoints(object o, EventArgsBase args)
        {
            TwitchChannelPointsRedemptionArgs a = args as TwitchChannelPointsRedemptionArgs;
            if (a == null)
                return; // quiet exit, not of our concern

            if (!mTriggers.ContainsKey(a.Title))
                return;

            /*string[] files = [
                "/content/bad_to_the_bone.ogg",
                "/content/gnome_reverb.ogg",
                "/content/megalovania.ogg",
                "/content/metal_pipe_sfx.ogg"
            ];*/

            AudioTrigger trigger = mTriggers[a.Title];

            Random rng = new Random();
            int fileIdx = rng.Next() % trigger.Files.Count;

            AudioPlayStartPlayback playback = new(trigger.Files[fileIdx]);

            if (trigger.RepeatLength > 0)
            {
                playback.RandomRepeat = true;
                playback.TotalLength = trigger.RepeatLength;
                playback.MinInterval = trigger.MinRepeatInterval;
                playback.MaxInterval = trigger.MaxRepeatInterval;
            }

            SendToWS(playback);
            AwaitEventCompletion();
        }

        private void OnEventInterrupt(object o, EventArgsBase args)
        {
            SendToWS(new AudioPlayInterrupt());
            AwaitEventCompletion();
        }

        protected override void OnConnected()
        {
        }

        protected override void OnConfigurationUpdate()
        {
            mTriggers.Clear();

            Config c = GetConfig() as Config;
            foreach (AudioTrigger t in c.Triggers)
            {
                mTriggers.Add(t.RedemptionName, t);
            }
        }

        protected override void OnLoad()
        {
            EventCollection collection = Comms.Event.User(mLBUser);

            // TODO this should all be configurable. This widget should be able to:
            //  - Accept events from multiple sources
            //  - Provide its own events so that it is controllable in some way
            collection.Event(Events.TWITCH_CHANNEL_POINTS_REDEMPTION).Endpoint += OnChannelPoints;
            collection.Event(Events.TWITCH_CHANNEL_POINTS_REDEMPTION).InterruptEndpoint += OnEventInterrupt;
        }

        protected override void OnUnload()
        {
            // noop
            // TODO should pause any played music probably
        }

        protected override ConfigurationBase CreateDefaultConfiguration()
        {
            return new Config();
        }

        public AudioPlay(string lbUser, string id, string name)
            : base(lbUser, "Widgets/AudioPlay.html", id, name)
        {
        }

        public override WidgetType GetWidgetType()
        {
            return WidgetType.audioplay;
        }

        ~AudioPlay()
        {
        }
    }
}
