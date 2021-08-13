using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace LukeBot.Common
{
    // NOTE This class is more of a temporary solution for PropertyStore
    // Proper PropertyStore implementation will make this one obsolete
    public class DataFileReader
    {
        private static readonly Lazy<DataFileReader> mInstance =
            new Lazy<DataFileReader>(() => new DataFileReader());
        public static DataFileReader Instance { get { return mInstance.Value; } }

        private Dictionary<string, string> mCache;
        private Mutex mMutex;

        private DataFileReader()
        {
            mCache = new Dictionary<string, string>();
            mMutex = new Mutex();
        }

        public bool Get(string name, out string value)
        {
            string path = "Data/" + name + ".lukebot";

            mMutex.WaitOne();

            // first, check cache
            if (mCache.TryGetValue(name, out value))
            {
                mMutex.ReleaseMutex();
                return true;
            }

            // then, try to find it on Data folder as a <name>.lukebot file
            if (!FileUtils.Exists(path))
            {
                mMutex.ReleaseMutex();
                return false;
            }

            value = File.ReadAllLines(path)[0];
            mCache.Add(name, value);
            mMutex.ReleaseMutex();
            return true;
        }
    }
}
