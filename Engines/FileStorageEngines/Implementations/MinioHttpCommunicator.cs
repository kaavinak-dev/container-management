using Amazon.Runtime.Internal.Auth;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engines.FileStorageEngines.Implementations
{
    public class MinioHttpCommunicator : FileStorageEngines.Abstractions.StorageEngineCommunications
    {

        string url;
        string access_key;
        string secret_key;

        public MinioHttpCommunicator(string url, string accessKey, string secretKey)
        {
            this.url = url;
            this.access_key = accessKey;
            this.secret_key = secretKey;
        }


        public override async Task<HttpResponseMessage> SendSignedGetAsync()
        {

            using (var client = new HttpClient())
            {
                // Ensure the path starts with /
                //if (!path.StartsWith("/")) path = "/" + path;
                var requestUrl = $"{this.url}";
                var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);

                // NOTE: Minio health check endpoints (/minio/health/live, /minio/health/ready) 
                // are unauthenticated and do NOT require signing.
                // If you try to sign them, Minio might actually reject it or ignore it.

                // If you later need to call Admin APIs (which require auth), 
                // you would need to manually add the Authorization header here 
                // because the Minio SDK does not expose a generic signer.

                return await client.SendAsync(request);
            }

        }

        public override void SignHttpRequest(HttpRequestMessage httpMessage)
        {

            throw new NotImplementedException();
        }
    }
}
