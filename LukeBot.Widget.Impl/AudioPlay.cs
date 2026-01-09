using System;
using System.Collections.Generic;
//using System.Linq;
using LukeBot.Common;
using LukeBot.Communication;
using LukeBot.Communication.Common;
using LukeBot.Logging;
using LukeBot.Twitch;
using Newtonsoft.Json;


namespace LukeBot.Widget.Impl
{
    /**
     * Widget responsible for playing an audio file available to it from the bot. That's
     * basically it, there is no more functionality.
     *
     * TODO consider adding some sort of "stop playing" event or whatever
     */
    public class AudioPlay: IWidget
    {
        private const string FILE_REDEMPTION = "file";
        private const string TTS_REDEMPTION = "tts";

        public class AudioPlayInterrupt: EventArgsBase
        {
            public AudioPlayInterrupt()
                : base("AudioPlayInterrupt")
            {
            }
        }

        public class AudioPlayStartPlayback: EventArgsBase
        {
            public string Type { get; set; }

            public AudioPlayStartPlayback(string eventName, string type)
                : base(eventName)
            {
                Type = type;
            }
        }

        public class AudioPlayStartFilePlayback: AudioPlayStartPlayback
        {
            public string File { get; set; }
            public bool RandomRepeat { get; set; }
            public int TotalLength { get; set; }
            public int MinInterval { get; set; }
            public int MaxInterval { get; set; }

            public AudioPlayStartFilePlayback(string file)
                : base("AudioPlayStartFilePlayback", FILE_REDEMPTION)
            {
                File = file;
                RandomRepeat = false;
            }
        }

        public class AudioPlayStartTTSPlayback: AudioPlayStartPlayback
        {
            public string Voice { get; set; }
            public string Message { get; set; }

            public AudioPlayStartTTSPlayback(string voice, string message)
                : base("AudioPlayStartTTSPlayback", TTS_REDEMPTION)
            {
                Voice = voice;
                Message = message;
            }
        }


        public class AudioTriggerFile: Configuration<AudioTriggerFile>
        {
            [ConfigurationField]
            public string FileName = "FILLMEIN";
            [ConfigurationField]
            public int Odds = 1;

            public override string ToString()
            {
                return String.Format("\"{0}\", Odds = {1}", FileName, Odds);
            }

            public override string ToShortString()
            {
                return FileName;
            }
        }

        public class VisibleForFileRedemption: ConfigurationParameterizedVisibilityAttribute<string>
        {
            public VisibleForFileRedemption(): base(nameof(AudioTrigger.RedemptionType)) {}

            public override bool Predicate(string parameter)
            {
                return parameter == FILE_REDEMPTION;
            }
        }

        public class VisibleForTTSRedemption: ConfigurationParameterizedVisibilityAttribute<string>
        {
            public VisibleForTTSRedemption(): base(nameof(AudioTrigger.RedemptionType)) {}

            public override bool Predicate(string parameter)
            {
                return parameter == TTS_REDEMPTION;
            }
        }

        public class AudioTrigger: Configuration<AudioTrigger>
        {
            [ConfigurationField]
            public string RedemptionName = "FILLMEIN";
            [ConfigurationListRestrictedField<string>(new[] { FILE_REDEMPTION, TTS_REDEMPTION })]
            public string RedemptionType = FILE_REDEMPTION;

            // File redemption details
            [ConfigurationField]
            [VisibleForFileRedemption]
            public List<AudioTriggerFile> Files = new();
            [ConfigurationField]
            [VisibleForFileRedemption]
            public int RepeatLength = 0;
            [ConfigurationField]
            [VisibleForFileRedemption]
            public int MinRepeatInterval = 0;
            [ConfigurationField]
            [VisibleForFileRedemption]
            public int MaxRepeatInterval = 0;

            [ConfigurationField]
            [VisibleForTTSRedemption]
            public string Voice = "";

            internal int TotalOdds = 0;

            public override string ToString()
            {
                string ret = String.Format("<{0}, {1}, ", RedemptionName, RedemptionType);
                switch (RedemptionType)
                {
                case FILE_REDEMPTION:
                {
                    ret += String.Format("{0} files, repeat for {1}, every {2}-{3}",
                        Files.Count,
                        RepeatLength,
                        MinRepeatInterval,
                        MaxRepeatInterval
                    );
                    break;
                }
                case TTS_REDEMPTION:
                {
                    ret += String.Format("{0}", Voice);
                    break;
                }
                }

                ret += ">";

                return ret;
            }

            public override string ToShortString()
            {
                return String.Format("{0} ({1})", RedemptionName, RedemptionType);
            }
        }

        private Dictionary<string, AudioTrigger> mTriggers = new();

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

            if (resp.ErrorCount == 0)
            {
                Logger.Log().Debug("Widget completed event successfully");
            }
            else if (resp.ErrorCount == 1)
            {
                Logger.Log().Warning("Widget failed to complete the event: {0}", resp.Reason[0]);
            }
            else
            {
                Logger.Log().Warning("{0} errors occured during Widget's event completion attempt:", resp.ErrorCount);
                for (int i = 0; i < resp.Reason.Length; ++i)
                {
                    Logger.Log().Warning("  - {0}", resp.Reason[i]);
                }
            }
        }

        private void OnChannelPoints(object o, EventArgsBase args)
        {
            TwitchChannelPointsRedemptionArgs a = args as TwitchChannelPointsRedemptionArgs;
            if (a == null)
                return; // quiet exit, not of our concern

            if (!mTriggers.ContainsKey(a.Title))
            {
                Logger.Log().Debug("Audio trigger {0} does not exist", a.Title);
                return;
            }

            AudioTrigger trigger = mTriggers[a.Title];

            if (trigger.RedemptionType == FILE_REDEMPTION)
            {
                Random rng = new Random();
                int fileIdx = -1;
                if (trigger.TotalOdds == 0)
                {
                    if (trigger.Files.Count == 0)
                    {
                        Logger.Log().Warning("Audio trigger {0} has no files added", a.Title);
                        return;
                    }

                    // TotalOdds are 0 for some reason, as a backup randomize equally between all files
                    fileIdx = rng.Next() % trigger.Files.Count;
                }
                else
                {
                    int rngRoll = rng.Next() % trigger.TotalOdds;

                    // iterate over all files until odds weight falls where needed
                    fileIdx = 0;
                    foreach (AudioTriggerFile f in trigger.Files)
                    {
                        if (f.Odds > rngRoll) break;

                        rngRoll -= f.Odds;
                        fileIdx++;
                    }
                }

                AudioPlayStartFilePlayback playback = new(trigger.Files[fileIdx].FileName);

                if (trigger.RepeatLength > 0)
                {
                    playback.RandomRepeat = true;
                    playback.TotalLength = trigger.RepeatLength;
                    playback.MinInterval = trigger.MinRepeatInterval;
                    playback.MaxInterval = trigger.MaxRepeatInterval;
                }

                SendToWS(playback);
            }
            else if (trigger.RedemptionType == TTS_REDEMPTION)
            {
                AudioPlayStartTTSPlayback playback = new(trigger.Voice, a.Message);
                SendToWS(playback);
            }

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
                if (t.RedemptionType == FILE_REDEMPTION)
                {
                    t.TotalOdds = 0;
                    foreach (AudioTriggerFile f in t.Files)
                    {
                        t.TotalOdds += f.Odds;
                    }
                }

                mTriggers.Add(t.RedemptionName, t);
            }
        }

        protected override void OnLoad()
        {
            EventCollection collection = Comms.Event.User(mLBUser);

            collection.Event(Events.TWITCH_CHANNEL_POINTS_REDEMPTION).Endpoint += OnChannelPoints;
            collection.Event(Events.TWITCH_CHANNEL_POINTS_REDEMPTION).InterruptEndpoint += OnEventInterrupt;
        }

        protected override void OnUnload()
        {
            // TODO should pause any played music probably

            EventCollection collection = Comms.Event.User(mLBUser);

            collection.Event(Events.TWITCH_CHANNEL_POINTS_REDEMPTION).Endpoint -= OnChannelPoints;
            collection.Event(Events.TWITCH_CHANNEL_POINTS_REDEMPTION).InterruptEndpoint -= OnEventInterrupt;
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
