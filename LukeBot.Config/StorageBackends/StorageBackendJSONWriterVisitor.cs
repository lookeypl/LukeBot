using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using LukeBot.Logging;


namespace LukeBot.Config
{
    internal class StorageBackendJSONWriterVisitor: PropertyStoreVisitor
    {
        JObject mRootObject;
        Stack<JArray> mObjectArrays;

        public StorageBackendJSONWriterVisitor(string path)
        {
            mObjectArrays = new Stack<JArray>();
        }

        public void Visit<T>(PropertyType<T> p)
        {
            string val = JsonConvert.SerializeObject(p.Value);

            Logger.Log().Secure("Visit: {0} = {1}", p.Name, val);

            JObject o = new JObject();

            o.Add("name", p.Name);
            o.Add("type", p.Type.ToString());
            if (p.Hidden)
                o.Add("hidden", p.Hidden);

            o.Add("value", val);

            mObjectArrays.Peek().Add(o);
        }

        public void VisitStart(PropertyDomain pd)
        {
            Logger.Log().Secure("VisitStart: {0}", pd.mName);

            if (pd.mName == PropertyStore.PROP_STORE_DOMAIN_ROOT)
            {
                mRootObject = new JObject();
            }

            mObjectArrays.Push(new JArray());
        }

        public void VisitEnd(PropertyDomain pd)
        {
            Logger.Log().Secure("VisitEnd: {0}", pd.mName);

            JObject o;
            if (pd.mName == PropertyStore.PROP_STORE_DOMAIN_ROOT)
            {
                o = mRootObject;
            }
            else
            {
                o = new JObject();
            }

            o.Add("name", pd.mName);
            o.Add("type", typeof(PropertyDomain).ToString());
            o.Add("value", mObjectArrays.Pop());

            if (pd.mName != PropertyStore.PROP_STORE_DOMAIN_ROOT)
            {
                mObjectArrays.Peek().Add(o);
            }
        }

        public JObject GetRootJObject()
        {
            if (mObjectArrays.Count != 0)
            {
                throw new System.InvalidOperationException(string.Format("Traversal invalid - got {0} items on stack", mObjectArrays.Count));
            }

            return mRootObject;
        }
    }
}