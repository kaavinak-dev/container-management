using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engines.FileStorageEngines.Abstractions
{
    public abstract class StorageEngineCommunications
    {
        public abstract Task<HttpResponseMessage> SendSignedGetAsync();
        public abstract void SignHttpRequest(HttpRequestMessage httpMessage);
    }
}
