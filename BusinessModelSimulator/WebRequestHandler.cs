using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.IO;
using System.Net;
using System.Text;

namespace BusinessModelSimulator
{
    public static class WebRequestHandler
    {
        public static WebResponse InvokeWebRequest(string url, string method = "GET", object body = null)
        {
            return PrepareWebRequest(url, method, body).GetResponse();
        }

        public static T InvokeWebRequest<T>(string url, string method = "GET", object body = null)
        {
            var request = PrepareWebRequest(url, method, body);
            var content = GetRequestResponseContent(request);
            var responseSerialized = JsonConvert.DeserializeObject<T>(content);
            return responseSerialized;
        }

        private static HttpWebRequest PrepareWebRequest(string url, string method, object body)
        {
            var request = WebRequest.CreateHttp(url);
            request.Method = method;
            request.ContentType = "application/json";
            if (body != null)
            {
                using (var stream = request.GetRequestStream())
                {
                    var serializerSettings = new JsonSerializerSettings();
                    serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                    var serializedData = JsonConvert.SerializeObject(body, serializerSettings);
                    var data = Encoding.ASCII.GetBytes(serializedData);
                    request.ContentLength = data.Length;
                    stream.Write(data, 0, data.Length);
                }
            }
            return request;
        }

        private static string GetRequestResponseContent(HttpWebRequest request)
        {
            using (var response = request.GetResponse())
            {
                using (var responseStream = response.GetResponseStream())
                using (var reader = new StreamReader(responseStream))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}
