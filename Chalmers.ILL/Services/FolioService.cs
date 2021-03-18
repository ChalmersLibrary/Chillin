using Chalmers.ILL.Models;
using Chalmers.ILL.Patron;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
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


        public void InitFolio(InstanceBasic instanceBasic, string barcode, string pickUpServicePoint)
        {
            var resInstance = CreateInstance(instanceBasic);
            var resHolding = CreateHolding(resInstance.Id);
            var resItem = CreateItem(resHolding.Id, barcode);
            //RequesterId
         //   var resCiruclation = CreateCirculation(resItem.Id, ,pickUpServicePoint ,barcode);
        }

        private Instance CreateInstance(InstanceBasic data)
        {
            var response = GetDataFromFolioWithRetries("/instance-storage/instances", "POST", SerializeObject(data));
            return JsonConvert.DeserializeObject<Instance>(response);
        }

        private Holding CreateHolding(string instanceId)
        {
            var data = new HoldingBasic
            {
                InstanceId = instanceId,
                PermanentLocationId = "fcd64ce1-6995-48f0-840e-89ffa2288371"
            };
            var response = GetDataFromFolioWithRetries("/holdings-storage/holdings", "POST", SerializeObject(data));
            return JsonConvert.DeserializeObject<Holding>(response);
        }

        private Item CreateItem(string holdingId, string barCode)
        {
            var data = new ItemBasic
            {
                MaterialTypeId = "1a54b431-2e4f-452d-9cae-9cee66c9a892",
                PermanentLoanTypeId = "2b94c631-fca9-4892-a730-03ee529ffe27",
                HoldingsRecordId = holdingId,
                Barcode = barCode,
                Status = new Status { Name = "Available" }
            };
            var response = GetDataFromFolioWithRetries("/item-storage/items", "POST", SerializeObject(data));
            return JsonConvert.DeserializeObject<Item>(response);
        }

        private Request CreateRequest()
        {
            var data = new RequestBasic
            {

            };
            var response = GetDataFromFolioWithRetries("/circulation/requests", "POST", SerializeObject(data));
            return JsonConvert.DeserializeObject<Circulation>(response);
        }

        private Circulation CreateCirculation(string itemId, string requesterId, string pickupServicePoint, string barCode)
        {
            var data = new CirculationBasic
            {
                ItemId = itemId,
                RequestDate = DateTime.Now.ToString(),
                RequesterId = requesterId,
                RequestType = "Page",
                FulFilmentPreference = "Hold Shelf",
                Status = "Open - Not yet filled",
                PickupServicePointId = ServicePoints()[pickupServicePoint],
                Item = new CircualtionBasicItem { Barcode = barCode }
            };
            var response = GetDataFromFolioWithRetries("/circulation/requests", "POST", SerializeObject(data));
            return JsonConvert.DeserializeObject<Circulation>(response);
        }

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

        private Dictionary<string, string> ServicePoints() =>
            new Dictionary<string, string>()
            {
                { "Huvudbiblioteket", "3a40852d-49fd-4df2-a1f9-6e2641a6e91f" },
                { "Lindholmenbiblioteket", "c4c90014-c8c9-4ade-8f24-b5e313319f4b" },
                { "Arkitekturbiblioteket", "7c5abc9f-f3d7-4856-b8d7-6712462ca007"}
            };
    }
}