using System.Collections.Generic;

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
            string[] pathArray = new string[mPath.Count];
            mPath.CopyTo(pathArray, 0);
            return Path.Form(pathArray);
        }

        public Path Push(string domain)
        {
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
            string[] pathArray = new string[mPath.Count];
            mPath.CopyTo(pathArray, 0);

            string result = "";
            for (int pIdx = 0; pIdx < pathArray.Length; ++pIdx)
            {
                result += pathArray[pIdx];
                if (pIdx < pathArray.Length - 1)
                    result += '.';
            }

            return result;
        }
    }
}
