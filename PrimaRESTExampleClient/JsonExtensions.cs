using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PrimaRESTExampleClient
{
    public static class JsonExtensions
    {
        public static string ToPrettyString(this string s)
        {
            return JToken.Parse(s)
                         .ToString(Formatting.Indented);
        }
    }

    public class JsonContent : StringContent
    {
        public JsonContent(object obj) : base(JsonConvert.SerializeObject(obj), Encoding.UTF8, "application/json") { }
    }
}