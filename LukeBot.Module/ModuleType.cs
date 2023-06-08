using LukeBot.Common;
using System;


namespace LukeBot.Module
{
    public enum ModuleType
    {
        Unknown = 0,
        Twitch,
        Spotify,
        Widget,
    }

    public static class ModuleTypeExtensions
    {
        public static ModuleType GetModuleTypeEnum(this string typeStr)
        {
            switch (typeStr)
            {
            case Constants.TWITCH_MODULE_NAME: return ModuleType.Twitch;
            case Constants.SPOTIFY_MODULE_NAME: return ModuleType.Spotify;
            case Constants.WIDGET_MODULE_NAME: return ModuleType.Widget;
            default:
                throw new ArgumentException(string.Format("Invalid type string {0}, should not happen", typeStr));
            }
        }

        public static string ToConfString(this ModuleType type)
        {
            switch (type)
            {
            case ModuleType.Twitch: return Constants.TWITCH_MODULE_NAME;
            case ModuleType.Spotify: return Constants.SPOTIFY_MODULE_NAME;
            case ModuleType.Widget: return Constants.WIDGET_MODULE_NAME;
            default:
                throw new ArgumentException(string.Format("Invalid enum {0}, should not happen", type));
            }
        }
    }
}