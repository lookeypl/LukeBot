using System;
using System.Collections.Generic;


namespace LukeBot.Services
{
    /**
     * Base interface for all LukeBot services.
     *
     * For T provide the Type that will be used to recognize the Service in Service APIs.
     */
    public interface IService<T>: IServiceBase where T: IService<T>
    {
        public Type RecognizableType { get => typeof(T); }
    }
}
