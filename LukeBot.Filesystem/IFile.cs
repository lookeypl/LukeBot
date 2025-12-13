using System;
using System.Data;
using System.IO;

namespace LukeBot.Filesystem
{
    public interface IFile: IAsyncDisposable, IDisposable
    {
        public string Path { get; }


    }
}