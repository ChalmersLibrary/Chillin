using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;

namespace Chalmers.ILL.Services
{
    public class JsonService : IJsonService
    {
        public T DeserializeObject<T>(string obj)
        {
            if (string.IsNullOrEmpty(obj))
            {
                throw new ArgumentNullException(nameof(obj));
            }
            return JsonConvert.DeserializeObject<T>(obj);
        }

        public string SerializeObject<T>(T obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
            return JsonConvert.SerializeObject(obj, settings);
        }
    }
}