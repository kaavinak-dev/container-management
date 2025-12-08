using Microsoft.AspNetCore.Http.HttpResults;
using Newtonsoft.Json;

namespace ContainerManagerBackend.Helpers
{
    public class ResponseFormatter
    {

        public ResponseFormatter()
        {

        }

        public string Ok(string message)
        {
            Dictionary<string,string> responseData = new Dictionary<string,string>();
            responseData["message"] = message;
            return JsonConvert.SerializeObject(responseData);
        }

        public string Error(string message) {
            Dictionary<string, string> responseData = new Dictionary<string, string>();
            responseData["error"] = message;
            return JsonConvert.SerializeObject(responseData);
            
        }

    }
}
