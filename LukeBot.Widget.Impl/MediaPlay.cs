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
    public class MediaPlay: QueueableEventWidget
    {
        private const string FILE_REDEMPTION = "file";
        private const string TTS_REDEMPTION = "tts";

        public class MediaPlayStartPlayback: SerializableEventArgsBase
        {
            public string Type { get; set; }

            public MediaPlayStartPlayback(string eventName, string type)
                : base(eventName)
            {
                Type = type;
            }

            public override string Serialize()
            {
                return JsonSerializer.Serialize<MediaPlayStartPlayback>(this);
            }
        }

        public class MediaPlayStartAudioFilePlayback: MediaPlayStartPlayback
        {
            public string File { get; set; }
            public bool RandomRepeat { get; set; }
            public int TotalLength { get; set; }
            public int MinInterval { get; set; }
            public int MaxInterval { get; set; }

            public MediaPlayStartAudioFilePlayback(string file)
                : base(nameof(MediaPlayStartAudioFilePlayback), FILE_REDEMPTION)
            {
                File = file;
                RandomRepeat = false;
            }

            public override string Serialize()
            {
                return JsonSerializer.Serialize<MediaPlayStartAudioFilePlayback>(this);
            }

            public override string ToString()
            {
                if (RandomRepeat)
                    return base.ToString() + String.Format(" ({0})", File);
                else
                    return base.ToString() + String.Format(" ({0}, random for {1}, {2}-{3} intervals)", File, TotalLength, MinInterval, MaxInterval);
            }
        }

        public class MediaPlayStartTTSPlayback: MediaPlayStartPlayback
        {
            public string Voice { get; set; }
            public string Message { get; set; }

            public MediaPlayStartTTSPlayback(string voice, string message)
                : base(nameof(MediaPlayStartTTSPlayback), TTS_REDEMPTION)
            {
                Voice = voice;
                Message = message;
            }

            public override string Serialize()
            {
                return JsonSerializer.Serialize<MediaPlayStartTTSPlayback>(this);
            }

            public override string ToString()
            {
                return base.ToString() + String.Format(" ({0} - {1})", Voice, Common.Utils.Shorten(Message, 20));
            }
        }


        public class MediaTriggerFile: Configuration<MediaTriggerFile>
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
            public VisibleForFileRedemption(): base(nameof(MediaTrigger.RedemptionType)) {}

            public override bool Predicate(string parameter)
            {
                return parameter == FILE_REDEMPTION;
            }
        }

        public class VisibleForTTSRedemption: ConfigurationParameterizedVisibilityAttribute<string>
        {
            public VisibleForTTSRedemption(): base(nameof(MediaTrigger.RedemptionType)) {}

            public override bool Predicate(string parameter)
            {
                return parameter == TTS_REDEMPTION;
            }
        }

        public class MediaTrigger: Configuration<MediaTrigger>
        {
            [ConfigurationField]
            public string RedemptionName = "FILLMEIN";
            [ConfigurationListRestrictedField<string>(new[] { FILE_REDEMPTION, TTS_REDEMPTION })]
            public string RedemptionType = FILE_REDEMPTION;

            // File redemption details
            [ConfigurationField]
            [VisibleForFileRedemption]
            public List<MediaTriggerFile> Files = new();
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

        private Dictionary<string, MediaTrigger> mTriggers = new();

        public class Config: Configuration<Config>
        {
            [ConfigurationField]
            public List<MediaTrigger> Triggers = new();
            [ConfigurationField]
            public bool QueuePlayback = false;

            public Config()
            {
            }
        }

        private class MediaPlayWidgetInternalConfig: SerializableEventArgsBase
        {
            public bool QueuePlayback = false;

            public MediaPlayWidgetInternalConfig()
                : base(nameof(MediaPlayWidgetInternalConfig))
            {}

            public override string Serialize()
            {
                return JsonSerializer.Serialize<MediaPlayWidgetInternalConfig>(this);
            }
        }

        private void OnChannelPoints(object o, EventArgsBase args)
        {
            TwitchChannelPointsRedemptionArgs a = args as TwitchChannelPointsRedemptionArgs;
            if (a == null)
                return; // quiet exit, not of our concern

            if (!mTriggers.ContainsKey(a.Title))
            {
                Logger.Log().Debug("Media trigger {0} does not exist", a.Title);
                a.Completed();
                return;
            }

            MediaTrigger trigger = mTriggers[a.Title];

            if (trigger.RedemptionType == FILE_REDEMPTION)
            {
                Random rng = new Random();
                int fileIdx = -1;
                if (trigger.TotalOdds == 0)
                {
                    if (trigger.Files.Count == 0)
                    {
                        Logger.Log().Warning("Media trigger {0} has no files added", a.Title);
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
                    foreach (MediaTriggerFile f in trigger.Files)
                    {
                        if (f.Odds > rngRoll) break;

                        rngRoll -= f.Odds;
                        fileIdx++;
                    }
                }

                MediaPlayStartAudioFilePlayback playback = new(trigger.Files[fileIdx].FileName);

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
                    MediaPlayStartTTSPlayback playback = new(trigger.Voice, a.Message.Message);
                    SendEvent(playback);
                }
            }
        }

        private void SendConfiguration(MediaPlayWidgetInternalConfig config)
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
            MediaPlayWidgetInternalConfig internalConfig = new();
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
            foreach (MediaTrigger t in c.Triggers)
            {
                if (t.RedemptionType == FILE_REDEMPTION)
                {
                    t.TotalOdds = 0;
                    foreach (MediaTriggerFile f in t.Files)
                    {
                        t.TotalOdds += f.Odds;
                    }
                }

                mTriggers.Add(t.RedemptionName, t);
            }

            // send separate internal config to queue the alerts (or not)
            MediaPlayWidgetInternalConfig internalConfig = new();
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

        public MediaPlay(string lbUser, string id, string name)
            : base(lbUser, "Widgets/MediaPlay.html", id, name)
        {
        }

        public override WidgetType GetWidgetType()
        {
            return WidgetType.mediaplay;
        }

        ~MediaPlay()
        {
        }
    }
}
