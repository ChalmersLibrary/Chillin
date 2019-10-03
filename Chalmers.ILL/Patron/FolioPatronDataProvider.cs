﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Chalmers.ILL.Models;
using System.Net;
using System.Configuration;
using System.IO;
using Newtonsoft.Json;
using Chalmers.ILL.Templates;
using System.Text;

namespace Chalmers.ILL.Patron
{
    public class FolioPatronDataProvider : IPatronDataProvider
    {
        private string _token { get; set; }

        private string _folioApiBaseAddress = ConfigurationManager.AppSettings["folioApiBaseAddress"].ToString();
        private string _tenant = ConfigurationManager.AppSettings["folioXOkapiTenant"].ToString();
        private string _username = ConfigurationManager.AppSettings["folioUsername"].ToString();
        private string _password = ConfigurationManager.AppSettings["folioPassword"].ToString();

        private ITemplateService _templateService;
        private IAffiliationDataProvider _affiliationDataProvider;

        public FolioPatronDataProvider(ITemplateService templateService, IAffiliationDataProvider affiliationDataProvider)
        {
            _templateService = templateService;
            _affiliationDataProvider = affiliationDataProvider;
        }

        public IPatronDataProvider Connect()
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

        public IPatronDataProvider Disconnect()
        {
            // Not needed
            return this;
        }

        public SierraModel GetPatronInfoFromLibraryCardNumber(string barcode)
        {
            SierraModel res = new SierraModel();

            var json = GetDataFromFolioWithRetries("barcode=" + barcode);

            if (json != null && json.users != null && json.users.Count == 1)
            {
                FillInSierraModelFromFolioData(json.users[0], res);
            }

            return res;
        }

        public SierraModel GetPatronInfoFromLibraryCardNumberOrPersonnummer(string barcode, string pnr)
        {
            SierraModel res = new SierraModel();

            var json = GetDataFromFolioWithRetries("username=" + pnr + " or barcode=" + barcode);

            if (json != null && json.users != null && json.users.Count == 1)
            {
                FillInSierraModelFromFolioData(json.users[0], res);
            }

            return res;
        }

        public SierraModel GetPatronInfoFromSierraId(string sierraId)
        {
            SierraModel res = new SierraModel();

            var json = GetDataFromFolioWithRetries("id=" + sierraId);

            if (json != null && json.users != null && json.users.Count == 1)
            {
                FillInSierraModelFromFolioData(json.users[0], res);
            }

            return res;
        }

        public IList<SierraModel> GetPatrons(string query)
        {
            var res = new List<SierraModel>();

            query = "(personal.email=\"" + query + "*\" or barcode=\"" + query + "*\" or username=\"" + query + "*\")";

            var json = GetDataFromFolioWithRetries(query, false);

            if (json != null && json.users != null)
            {
                foreach (var user in json.users)
                {
                    var sierraModelForUser = new SierraModel();
                    FillInSierraModelFromFolioData(user, sierraModelForUser);
                    res.Add(sierraModelForUser);
                }
            }

            return res;
        }

        #region Private methods

        private dynamic GetDataFromFolioWithRetries(string query, bool limitToOne = true)
        {
            dynamic res = null;
            var retry = true;
            var retryCount = 1;
            while (retry)
            {
                try
                {
                    res = GetDataFromFolio(query, false);
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

        private dynamic GetDataFromFolio(string query, bool limitToOne = true)
        {
            dynamic res = null;

            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(_folioApiBaseAddress + "/users?" + 
                (limitToOne ? "limit=1&" : "") + "query=" + Uri.EscapeDataString(query));

            request.Accept = "application/json";
            request.ContentType = "application/json";
            request.Headers["x-okapi-tenant"] = _tenant;
            request.Headers["x-okapi-token"] = _token;

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            var outputStream = response.GetResponseStream();

            var sr = new StreamReader(outputStream);
            var resultString = sr.ReadToEnd();

            if (resultString.ToLower().Contains("invalid token"))
            {
                throw new InvalidTokenException();
            }

            res = JsonConvert.DeserializeObject<dynamic>(resultString);
            return res;
        }

        private void FillInSierraModelFromFolioData(dynamic recordData, /* out */ SierraModel result)
        {
            result.barcode = recordData.barcode;
            result.id = recordData.id;
            if (recordData.personal != null)
            {
                result.email = recordData.personal.email;
                result.first_name = recordData.personal.firstName;
                result.last_name = recordData.personal.lastName;
            }

            //result.mblock = recordData.mblock;
            //result.ptype = recordData.ptype;
            result.expdate = recordData.expirationDate;
            result.pnum = recordData.username;
            result.aff = _affiliationDataProvider.GetAffiliationFromPersonNumber(Convert.ToString(recordData.username));
        }

        #endregion
    }
}