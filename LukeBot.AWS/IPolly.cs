using System.IO;
using System.Threading.Tasks;
using LukeBot.Services;

namespace LukeBot.AWS
{
    public interface IPolly
    {
        Task<Stream> SynthesizeSpeech(PollyVoice voice, string text);
    }
}
