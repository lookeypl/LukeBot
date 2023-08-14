using System.Collections.Generic;
using Newtonsoft.Json;


namespace LukeBot.Twitch.Common.Command
{
    public class Descriptor
    {
        public string Name { get; private set; }
        public Type Type { get; private set; }
        public User Privilege { get; private set; }
        public bool Enabled { get; private set; }
        public string Value { get; private set; }

        public Descriptor(string name)
            : this(name, Type.invalid, User.Everyone, true, "")
        {
        }

        public Descriptor(string name, Type type, string value)
            : this(name, type, User.Everyone, true, value)
        {
        }

        [JsonConstructor]
        public Descriptor(string name, Type type, User privilege, bool enabled, string value)
        {
            Name = name;
            Type = type;
            Privilege = privilege;
            Enabled = enabled;
            Value = value;
        }

        public void UpdateValue(string newValue)
        {
            Value = newValue;
        }
    }

    public class DescriptorComparer: IComparer<Descriptor>
    {
        public int Compare(Descriptor a, Descriptor b)
        {
            return string.Compare(a.Name, b.Name);
        }
    }
}