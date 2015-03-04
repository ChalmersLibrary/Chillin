using Chalmers.ILL.OrderItems;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Web;

namespace Chalmers.ILL.Providers
{
    public class LibrisOrderItemsSource : ISource
    {
        public SourcePollingResult Poll()
        {
            var res = new SourcePollingResult("Libris");

            var addressStr = ConfigurationManager.AppSettings["librisApiBaseAddress"] + "/illse/api/userrequests/" + ConfigurationManager.AppSettings["librarySigel"];
            
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("api-key", ConfigurationManager.AppSettings["librisApiKey"]);
            var task = httpClient.GetStringAsync(new Uri(addressStr));

            task.Wait();

            var userRequestsQueryResult = JsonConvert.DeserializeObject<dynamic>(task.Result);

            for (int index = 0; index < userRequestsQueryResult.count.Value; index++)
            {
                // TODO: Do something
            }

            return res;
        }
    }
}