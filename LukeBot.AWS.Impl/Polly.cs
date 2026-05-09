using System.Collections.Generic;
using LukeBot.AWS;
using LukeBot.Common;
using LukeBot.Config;
using Amazon;
using Amazon.Polly;
using Amazon.Polly.Model;
using System.Threading.Tasks;
using System.IO;
using System;
using System.Net;


namespace LukeBot.AWS.Impl
{
    internal class Polly: IPolly
    {
        private AmazonPollyConfig mConfig;
        private AmazonPollyClient mClient;

        private VoiceId PollyVoiceToVoiceId(PollyVoice voice)
        {
            switch (voice)
            {
            case PollyVoice.Brian: return VoiceId.Brian;
            case PollyVoice.Jacek: return VoiceId.Jacek;
            default: throw new ArgumentException(String.Format("Unknown Polly voice requested: {0}", voice));
            }
        }

        internal Polly()
        {
            Config.Path awsRegionConfPath = Config.Path.Start()
                .Push(Common.Constants.AWS_SERVICE_NAME)
                .Push(Common.Constants.PROP_STORE_REGION_PROP_NAME);

            mConfig = new();
            if (Conf.TryGet<string>(awsRegionConfPath, out string awsRegion))
            {
                mConfig.RegionEndpoint = RegionEndpoint.GetBySystemName(awsRegion);
            }
            else
            {
                mConfig.RegionEndpoint = RegionEndpoint.EUCentral1;
            }

            mClient = new(AWSCredentials.ID, AWSCredentials.Secret, mConfig);
        }

        public async Task<Stream> SynthesizeSpeech(PollyVoice voice, string text)
        {
            SynthesizeSpeechRequest request = new();
            request.OutputFormat = OutputFormat.Ogg_vorbis;
            request.Engine = Engine.Standard;
            request.VoiceId = PollyVoiceToVoiceId(voice);
            request.SampleRate = "44100";
            request.Text = text;

            SynthesizeSpeechResponse response = await mClient.SynthesizeSpeechAsync(request);
            if (response.HttpStatusCode != HttpStatusCode.OK)
            {
                throw new PollyException("SynthesizeSpeech API returned code {0} ({1})", response.HttpStatusCode, (int)response.HttpStatusCode);
            }

            return response.AudioStream;
        }
    }
}
