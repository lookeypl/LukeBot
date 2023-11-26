﻿using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using LukeBot.Logging;


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
            Property p = Property.Create((string)o["type"], (string)o["value"]);

            JToken hiddenToken;
            if (o.TryGetValue("hidden", out hiddenToken)) {
                p.SetHidden((bool)hiddenToken);
            }

            return p;
        }

        private void Load_WalkNode(PropertyStore store, Path path, JObject o)
        {
            Logger.Log().Secure("Walking node fullName {0}", path.ToString());

            if ((string)o["type"] == typeof(PropertyDomain).ToString())
            {
                JArray values = (JArray)o["value"];

                foreach (JObject val in values)
                {
                    Load_WalkNode(store, path.Copy().Push((string)val["name"]), val);
                }
            }
            else
            {
                store.Add(path, JObjectToProperty(o));
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
                throw new PropertyFileInvalidException(mPath);
            }

            foreach (JObject val in (JArray)rootObject["value"])
            {
                Load_WalkNode(store, Path.Start().Push((string)val["name"]), val);
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
