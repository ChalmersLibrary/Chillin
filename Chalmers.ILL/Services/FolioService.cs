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

        public void InitFolio(InstanceBasic instanceBasic, string barcode, string pickUpServicePoint, bool readOnlyAtLibrary, string folioUserId)
        {

            VerifyBarCode(barcode);
            var resInstance = CreateInstance(instanceBasic);
            var resHolding = CreateHolding(resInstance.Id);
            var resItem = CreateItem(resHolding.Id, barcode, readOnlyAtLibrary);
            var resCiruclation = CreateCirculation(resItem.Id, folioUserId, pickUpServicePoint);
        }

        private void VerifyBarCode(string barcode)
        {
            var response = GetDataFromFolioWithRetries($"/item-storage/items?query=(barcode={barcode})", "GET");
            BarCodeQuery data = JsonConvert.DeserializeObject<BarCodeQuery>(response);

            if (data.TotalRecords > 0)
            {
                throw new BarCodeException("Streckkoden finns redan i FOLIO");
            }
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
                DiscoverySuppress = true,
                InstanceId = instanceId,
                PermanentLocationId = ConfigurationManager.AppSettings["holdingPermanentLocationId"].ToString(),
                CallNumber = "Interlibrary-in-loan",
                StatisticalCodeIds = new string[]
                {
                    ConfigurationManager.AppSettings["chillinStatisticalCodeId"].ToString()
                }
            };
            var response = GetDataFromFolioWithRetries("/holdings-storage/holdings", "POST", SerializeObject(data));
            return JsonConvert.DeserializeObject<Holding>(response);
        }

        private Item CreateItem(string holdingId, string barCode, bool readOnlyAtLibrary)
        {
            var data = new ItemBasic
            {
                DiscoverySuppress = true,
                MaterialTypeId = ConfigurationManager.AppSettings["itemMaterialTypeId"].ToString(),
                PermanentLoanTypeId = readOnlyAtLibrary ? 
                    ConfigurationManager.AppSettings["itemPermanentLoanTypeIdInHouse"].ToString() : 
                    ConfigurationManager.AppSettings["itemPermanentLoanTypeId"].ToString(),
                HoldingsRecordId = holdingId,
                Barcode = barCode,
                Status = new Status { Name = "Available" },
                StatisticalCodeIds = new string[]
                {
                    ConfigurationManager.AppSettings["chillinStatisticalCodeId"].ToString()
                },
                CirculationNotes = new List<CirculationNotes>
                {
                    new CirculationNotes
                    {
                        NoteType = "Check in",
                        Note = "NÄR LÅNTAGARE ÅTERLÄMNAR: Lägg på hyllan för återlämnade fjärrlån på HB",
                        StaffOnly = true
                    }
                }
            };

            if (readOnlyAtLibrary)
            {
                data.CirculationNotes.Add(
                    new CirculationNotes
                    {
                        NoteType = "Check out",
                        Note = "Ej hemlån",
                        StaffOnly = true
                    });
            }

            var response = GetDataFromFolioWithRetries("/item-storage/items", "POST", SerializeObject(data));
            return JsonConvert.DeserializeObject<Item>(response);
        }

        private Circulation CreateCirculation(string itemId, string requesterId, string pickupServicePoint)
        {
            var data = new CirculationBasic
            {
                ItemId = itemId,
                RequestDate = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                RequesterId = requesterId,
                RequestType = "Page",
                FulfilmentPreference = "Hold Shelf",
                PickupServicePointId = ServicePoints()[pickupServicePoint]
            };
            var response = GetDataFromFolioWithRetries("/circulation/requests", "POST", SerializeObject(data));
            return JsonConvert.DeserializeObject<Circulation>(response);
        }

        public void SetItemToWithdrawn(string barcode)
        {
            var response = GetItemByBarCode(barcode);
            if (response.TotalRecords > 0 && response.Items[0].Barcode == barcode)
            {
                response.Items[0].Status.Name = "Withdrawn";
                PostItem(response.Items[0]);
            }
            //TODO - Slänga fel om det inte blir satt?
        }

        private BarCodeQuery GetItemByBarCode(string barcode)
        {
            var response = GetDataFromFolioWithRetries($"/item-storage/items?query=(barcode={barcode})", "GET");
            return JsonConvert.DeserializeObject<BarCodeQuery>(response);
        }

        private void PostItem(Item item)
        {
            Put($"/item-storage/items/{item.Id}", SerializeObject(item));
        }

        private string SerializeObject(dynamic data)
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
            return JsonConvert.SerializeObject(data, settings);
        }

        private dynamic GetDataFromFolioWithRetries(string path, string method, string body = null)
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

            if (string.IsNullOrEmpty(body) == false)
            {
                UTF8Encoding encoding = new UTF8Encoding();
                var bodyBytes = encoding.GetBytes(body);
                request.ContentLength = bodyBytes.Length;
                var requestStream = request.GetRequestStream();
                requestStream.Write(bodyBytes, 0, bodyBytes.Length);
            }

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

        private dynamic Put(string path, string body)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(_folioApiBaseAddress + path);

            request.Accept = "text/plain";
            request.ContentType = "application/json";
            request.Headers["x-okapi-tenant"] = _tenant;
            request.Headers["x-okapi-token"] = _token;
            request.Method = "PUT";

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
                { "Huvudbiblioteket", ConfigurationManager.AppSettings["servicePointHuvudbiblioteketId"].ToString() },
                { "Lindholmenbiblioteket", ConfigurationManager.AppSettings["servicePointLindholmenbiblioteketId"].ToString() },
                { "Arkitekturbiblioteket", ConfigurationManager.AppSettings["servicePointArkitekturbiblioteketId"].ToString() }
            };
    }
}