using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;


namespace LukeBot.Config
{
    public class Path
    {
        private Queue<string> mPath = new();
        public bool Empty
        {
            get
            {
                return (mPath.Count == 0);
            }
        }

        public int Count
        {
            get
            {
                return mPath.Count;
            }
        }

        static public Path Form(params string[] paths)
        {
            Path p = Start();

            foreach (string path in paths)
                p.Push(path);

            return p;
        }

        static public Path Parse(string fullPath)
        {
            return Form(fullPath.Split('.'));
        }

        static public Path Start()
        {
            return new Path();
        }

        private Path()
        {
        }

        public Path Copy()
        {
            return Path.Form(AsStringArray());
        }

        public Path Push(string domain)
        {
            if (String.IsNullOrEmpty(domain))
                throw new ArgumentNullException(nameof(domain));

            mPath.Enqueue(domain);
            return this;
        }

        public string Pop()
        {
            if (Empty)
                throw new PathEmptyException();

            return mPath.Dequeue();
        }

        public override string ToString()
        {
            string[] pathArray = AsStringArray();

            string result = "";
            for (int pIdx = 0; pIdx < pathArray.Length; ++pIdx)
            {
                result += pathArray[pIdx];
                if (pIdx < pathArray.Length - 1)
                    result += '.';
            }

            return result;
        }

        internal string[] AsStringArray()
        {
            string[] array = new string[Count];
            mPath.CopyTo(array, 0);
            return array;
        }
    }

    public class PathEqualityComparer: IEqualityComparer<Path>
    {
        public bool Equals(Path x, Path y)
        {
            if (x.Count != y.Count) return false;
            string[] xArray = x.AsStringArray();
            string[] yArray = y.AsStringArray();

            for (int i = 0; i < x.Count; ++i)
            {
                if (xArray[i] != yArray[i]) return false;
            }

            return true;
        }

        public int GetHashCode([DisallowNull] Path obj)
        {
            int hash = 0;
            string[] array = obj.AsStringArray();
            foreach (string s in array)
            {
                hash ^= s.GetHashCode();
            }

            return hash;
        }
    }
}
