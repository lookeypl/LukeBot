using System;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;
using LukeBot.Common;
using LukeBot.Communication;
using LukeBot.Logging;
using LukeBot.Twitch;
using System.Security.Cryptography.X509Certificates;


namespace LukeBot.Widget.Impl
{
    /**
     * Widget responsible for playing an audio file available to it from the bot. That's
     * basically it, there is no more functionality.
     *
     * TODO consider adding some sort of "stop playing" event or whatever
     */
    public class AudioPlay: QueueableEventWidget
    {
        private const string FILE_REDEMPTION = "file";
        private const string TTS_REDEMPTION = "tts";

        public class AudioPlayStartPlayback: SerializableEventArgsBase
        {
            public string Type { get; set; }

            public AudioPlayStartPlayback(string eventName, string type)
                : base(eventName)
            {
                Type = type;
            }

            public override string Serialize()
            {
                return JsonSerializer.Serialize<AudioPlayStartPlayback>(this);
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

            public override string Serialize()
            {
                return JsonSerializer.Serialize<AudioPlayStartFilePlayback>(this);
            }

            public override string ToString()
            {
                if (RandomRepeat)
                    return base.ToString() + String.Format(" ({0})", File);
                else
                    return base.ToString() + String.Format(" ({0}, random for {1}, {2}-{3} intervals)", File, TotalLength, MinInterval, MaxInterval);
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

            public override string Serialize()
            {
                return JsonSerializer.Serialize<AudioPlayStartTTSPlayback>(this);
            }

            public override string ToString()
            {
                return base.ToString() + String.Format(" ({0} - {1})", Voice, Common.Utils.Shorten(Message, 20));
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
            [ConfigurationField]
            public bool QueuePlayback = false;

            public Config()
            {
            }
        }

        private class AudioPlayWidgetInternalConfig: SerializableEventArgsBase
        {
            public bool QueuePlayback = false;

            public AudioPlayWidgetInternalConfig()
                : base(nameof(AudioPlayWidgetInternalConfig))
            {}

            public override string Serialize()
            {
                return JsonSerializer.Serialize<AudioPlayWidgetInternalConfig>(this);
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
                a.Completed();
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
                        a.Completed();
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

                SendEvent(playback);
            }
            else if (trigger.RedemptionType == TTS_REDEMPTION)
            {
                if (a.Message != null)
                {
                    AudioPlayStartTTSPlayback playback = new(trigger.Voice, a.Message.Message);
                    SendEvent(playback);
                }
            }
        }

        private void SendConfiguration(AudioPlayWidgetInternalConfig config)
        {
            WidgetResponse response = SendEventAndWait(config);
            if (response == null) return; // quietly ignore, Widget is not connected yet

            if (response.ErrorCount == 0)
            {
                Logger.Log().Info("Widget {0}: Configuration applied.", GetPrintableWidgetID());
            }
            else
            {
                LogResponseStatus(response);
            }
        }

        protected override void OnConnected()
        {
            EventSubscribe(Events.TWITCH_CHANNEL_POINTS_REDEMPTION, OnChannelPoints, true);

            // notify internal config to not queue the alerts
            AudioPlayWidgetInternalConfig internalConfig = new();
            internalConfig.QueuePlayback = false;
            SendConfiguration(internalConfig);
        }

        protected override void OnDisconnected()
        {
            // TODO should pause any played music probably
            EventUnsubscribe(Events.TWITCH_CHANNEL_POINTS_REDEMPTION, OnChannelPoints);
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

            // send separate internal config to queue the alerts (or not)
            AudioPlayWidgetInternalConfig internalConfig = new();
            internalConfig.QueuePlayback = c.QueuePlayback;
            SendConfiguration(internalConfig);
        }

        protected override void OnLoad()
        {
        }

        protected override void OnUnload()
        {
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
