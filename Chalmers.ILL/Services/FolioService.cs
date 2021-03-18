using Chalmers.ILL.Models;
using Chalmers.ILL.Patron;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Text;

namespace Chalmers.ILL.Services
{
    public class FolioService : IFolioService
    {
        private readonly string _folioApiBaseAddress = ConfigurationManager.AppSettings["folioApiBaseAddress"].ToString();
        private readonly string _tenant = ConfigurationManager.AppSettings["folioXOkapiTenant"].ToString();
        private readonly string _username = ConfigurationManager.AppSettings["folioUsername"].ToString();
        private readonly string _password = ConfigurationManager.AppSettings["folioPassword"].ToString();
        private string _token;

        public IFolioService Connect()
        {
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(_folioApiBaseAddress + "/authn/login");

            request.Accept = "application/json";
            request.ContentType = "application/json";
            request.Headers["x-okapi-tenant"] = _tenant;
            request.Method = "POST";

            UTF8Encoding encoding = new UTF8Encoding();
            var bodyBytes = encoding.GetBytes("{ \"username\": \"" + _username + "\", \"password\": \"" + _password + "\" }");
            request.ContentLength = bodyBytes.Length;
            var requestStream = request.GetRequestStream();
            requestStream.Write(bodyBytes, 0, bodyBytes.Length);

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            _token = response.Headers.Get("x-okapi-token");

            return this;
        }


        public void InitFolio(InstanceBasic instanceBasic, string barcode)
        {
            var resInstance = CreateInstance(instanceBasic);
            var resHolding = CreateHolding(resInstance.Id);
            var resItem = CreateItem(resHolding.Id, barcode);
            //RequesterId
            //PickupServicePointId
           //var resCiruclation = CreateCirculation(resItem.Id, , ,barcode);
        }

        private Instance CreateInstance(InstanceBasic instanceBasic)
        {
            //var settings = new JsonSerializerSettings
            //{
            //    ContractResolver = new CamelCasePropertyNamesContractResolver()
            //};
            //var body = JsonConvert.SerializeObject(instanceBasic, settings);
            var body = SerializeObject(instanceBasic);
            var response = GetDataFromFolioWithRetries("/instance-storage/instances", "POST", body);
            var data = JsonConvert.DeserializeObject<Instance>(response);
            return data;
        }

        private Holding CreateHolding(string instanceId)
        {
            var holdingBasic = new HoldingBasic
            {
                InstanceId = instanceId,
                PermanentLocationId = "fcd64ce1-6995-48f0-840e-89ffa2288371"
            };
            //var settings = new JsonSerializerSettings
            //{
            //    ContractResolver = new CamelCasePropertyNamesContractResolver()
            //};
            //var body = JsonConvert.SerializeObject(holdingBasic, settings);
            var body = SerializeObject(holdingBasic);
            var response = GetDataFromFolioWithRetries("/holdings-storage/holdings", "POST", body);
            var data = JsonConvert.DeserializeObject<Holding>(response);
            return data;
        }

        private Item CreateItem(string holdingId, string barCode)
        {
            var itemBasic = new ItemBasic
            {
                MaterialTypeId = "1a54b431-2e4f-452d-9cae-9cee66c9a892",
                PermanentLoanTypeId = "2b94c631-fca9-4892-a730-03ee529ffe27",
                HoldingsRecordId = holdingId,
                Barcode = barCode,
                Status = new Status { Name = "Available" }
            };
            //var settings = new JsonSerializerSettings
            //{
            //    ContractResolver = new CamelCasePropertyNamesContractResolver()
            //};
            //var body = JsonConvert.SerializeObject(itemBasic, settings);
            var body = SerializeObject(itemBasic);
            var response = GetDataFromFolioWithRetries("/item-storage/items", "POST", body);
            var data = JsonConvert.DeserializeObject<Item>(response);
            return data;
        }

        //private Request CreateCirculation(string itemId, string requesterId, string pickupServicePointId, string barCode)
        //{
        //    var requestBasic = new CirculationBasic
        //    {
        //        ItemId = itemId,
        //        RequestDate = DateTime.Now.ToString(),
        //        RequesterId = requesterId,
        //        RequestType = "Page",
        //        FulFilmentPreference = "Hold Shelf",
        //        Status = "Open - Not yet filled",
        //        PickupServicePointId = ,
        //        Item = new CircualtionBasicItem { Barcode = barCode }
        //    };
        //    //var settings = new JsonSerializerSettings
        //    //{
        //    //    ContractResolver = new CamelCasePropertyNamesContractResolver()
        //    //};
        //    //var body = JsonConvert.SerializeObject(requestBasic, settings);
        //    var body = SerializeObject(requestBasic);
        //    var response = GetDataFromFolioWithRetries("/circulation/requests", "POST", body);
        //    var data = JsonConvert.DeserializeObject<Request>(response);
        //    return data;
        //}

        private string SerializeObject(dynamic data)
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
            return JsonConvert.SerializeObject(data, settings);
        }

        private dynamic GetDataFromFolioWithRetries(string path, string method, string body)
        {
            dynamic res = null;
            var retry = true;
            var retryCount = 1;
            while (retry)
            {
                try
                {
                    res = GetDataFromFolio(path, method, body);
                    retry = false;
                }
                catch (InvalidTokenException)
                {
                    Connect();
                    retry = retryCount > 0;
                }
                retryCount -= 1;
            }
            return res;
        }

        private dynamic GetDataFromFolio(string path, string method, string body)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(_folioApiBaseAddress + path);

            request.Accept = "application/json";
            request.ContentType = "application/json";
            request.Headers["x-okapi-tenant"] = _tenant;
            request.Headers["x-okapi-token"] = _token;
            request.Method = method;

            UTF8Encoding encoding = new UTF8Encoding();
            var bodyBytes = encoding.GetBytes(body);
            request.ContentLength = bodyBytes.Length;
            var requestStream = request.GetRequestStream();
            requestStream.Write(bodyBytes, 0, bodyBytes.Length);

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            var outputStream = response.GetResponseStream();

            var sr = new StreamReader(outputStream);
            var resultString = sr.ReadToEnd();

            if (resultString.ToLower().Contains("invalid token"))
            {
                throw new InvalidTokenException();
            }

            return resultString;
        }
    }
}