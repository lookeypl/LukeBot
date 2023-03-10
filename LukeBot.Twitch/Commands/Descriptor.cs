using System.Collections.Generic;
using LukeBot.Twitch.Common;
using Newtonsoft.Json;


namespace LukeBot.Twitch.Command
{
    public class Descriptor
    {
        public string Name { get; private set; }
        public TwitchCommandType Type { get; private set; }
        public string Value { get; private set; }

        public Descriptor(string name)
            : this(name, TwitchCommandType.invalid, "")
        {
        }

        [JsonConstructor]
        public Descriptor(string name, TwitchCommandType type, string value)
        {
            Name = name;
            Type = type;
            Value = value;
        }

        public void UpdateValue(string newValue)
        {
            Value = newValue;
        }
    }

    internal class DescriptorComparer: IComparer<Descriptor>
    {
        public int Compare(Descriptor a, Descriptor b)
        {
            return string.Compare(a.Name, b.Name);
        }
    }
}