using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using LukeBot.Common;


namespace LukeBot.Config
{
    class StorageBackendJSON: IStorageBackend
    {
        public StorageBackendJSON(string path)
            : base(path)
        {
        }

        private Property JObjectToProperty(JObject o)
        {
            return Property.Create((string)o["type"], (string)o["value"]);
        }

        private void Load_WalkNode(PropertyStore store, string fullName, JObject o)
        {
            Logger.Log().Secure("Walking node fullName {0}", fullName);

            if ((string)o["type"] == typeof(PropertyDomain).ToString())
            {
                JArray values = (JArray)o["value"];

                foreach (JObject val in values)
                {
                    Load_WalkNode(store, fullName + "." + (string)val["name"], val);
                }
            }
            else
            {
                store.Add(fullName, JObjectToProperty(o));
            }
        }

        public override void Load(PropertyStore store)
        {
            Logger.Log().Debug("Loading storage file {0}", mPath);

            JsonReader reader = new JsonTextReader(new StreamReader(mPath));
            reader.CloseInput = true;
            JObject rootObject = (JObject)JToken.ReadFrom(reader);
            reader.Close();

            if (((string)rootObject["name"]) != PropertyStore.PROP_STORE_DOMAIN_ROOT ||
                ((string)rootObject["type"]) != typeof(PropertyDomain).ToString())
            {
                throw new PropertyFileInvalidException("Property file doesn't start with a root domain");
            }

            foreach (JObject val in (JArray)rootObject["value"])
            {
                Load_WalkNode(store, (string)val["name"], val);
            }
        }

        public override void Save(PropertyStore store)
        {
            Logger.Log().Debug("Saving storage file {0}", mPath);

            StorageBackendJSONWriterVisitor visitor = new StorageBackendJSONWriterVisitor(mPath);
            store.Traverse(visitor);

            JsonTextWriter writer = new JsonTextWriter(new StreamWriter(mPath));
            writer.AutoCompleteOnClose = true;
            writer.CloseOutput = true;

            visitor.GetRootJObject().WriteTo(writer);
            writer.Close();
        }

        public override void Update(Queue<string> path, Property p)
        {
            throw new System.NotImplementedException();
        }
    }
}
