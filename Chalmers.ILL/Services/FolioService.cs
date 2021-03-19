﻿using Chalmers.ILL.Models;
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


        public void InitFolio(InstanceBasic instanceBasic, string barcode, string pickUpServicePoint, bool readOnlyAtLibrary, string patronCardNumber)
        {
            var userId = UserId(patronCardNumber);
            var resInstance = CreateInstance(instanceBasic);
            var resHolding = CreateHolding(resInstance.Id);
            var resItem = CreateItem(resHolding.Id, barcode, readOnlyAtLibrary);
          //  var resCiruclation = CreateCirculation(resItem.Id, userId, pickUpServicePoint, barcode);
        }

        private string UserId(string barcode)
        {
            var response = GetDataFromFolioWithRetries($"/users?query=(barcode={barcode})", "GET");
            var data = JsonConvert.DeserializeObject<FolioUser>(response);

            if (data.Users.Length == 0)
            {
                throw new FolioUserException("Användaren hittades inte i FOLIO");
            }

            return data.Users[0].Id;
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
                CallNumberTypeId = ConfigurationManager.AppSettings["holdingCallNumberTypeId"].ToString(),
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
                PermanentLoanTypeId = ConfigurationManager.AppSettings["itemPermanentLoanTypeId"].ToString(),
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
                        Note = "Vid återlämning - ska till HBs fjärr-in-skrivbord",
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
                RequestDate = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffK"),
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

        private Dictionary<string, string> ServicePoints() =>
            new Dictionary<string, string>()
            {
                { "Huvudbiblioteket", ConfigurationManager.AppSettings["servicePointHuvudbiblioteketId"].ToString() },
                { "Lindholmenbiblioteket", ConfigurationManager.AppSettings["servicePointLindholmenbiblioteketId"].ToString() },
                { "Arkitekturbiblioteket", ConfigurationManager.AppSettings["servicePointArkitekturbiblioteketId"].ToString()}
            };
    }
}