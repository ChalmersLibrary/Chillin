using System;
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
using Umbraco.Core.Logging;

namespace Chalmers.ILL.Patron
{
    public class FolioPatronDataProvider : IPatronDataProvider
    {
        private string _token = "fejktoken";

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

            var json = GetDataFromFolioWithRetries("/users?limit=1&query=" + Uri.EscapeDataString("barcode=" + barcode));

            if (json != null && json.users != null && json.users.Count == 1)
            {
                var mblockJson = GetDataFromFolioWithRetries("/manualblocks?query=userId=" + json.users[0].id);
                FillInSierraModelFromFolioData(json.users[0], mblockJson, res);
            }

            return res;
        }

        public SierraModel GetPatronInfoFromLibraryCardNumberOrPersonnummer(string barcode, string pnr)
        {

            string testPnr = pnr.Length == 12 ? pnr.Remove(0, 2) : pnr;

            SierraModel res = new SierraModel();

            var requestUserDataPath = "/users?limit=1&query=" + Uri.EscapeDataString("username=" + testPnr + " or barcode=" + barcode);

            try
            {
                var json = GetDataFromFolioWithRetries(requestUserDataPath);

                if (json != null && json.users != null && json.users.Count == 1)
                {
                    var requestManualBlocksPath = "/manualblocks?query=userId=" + json.users[0].id;

                    try
                    {
                        var mblockJson = GetDataFromFolioWithRetries(requestManualBlocksPath);
                        FillInSierraModelFromFolioData(json.users[0], mblockJson, res);
                    }
                    catch (Exception e)
                    {
                        LogHelper.Error<FolioPatronDataProvider>("Error while fetching manual block data from Folio with (" + requestManualBlocksPath + "): ", e);
                    }
                }
            }
            catch (Exception e)
            {
                LogHelper.Error<FolioPatronDataProvider>("Error while fetching patron data from Folio with (" + requestUserDataPath + "): ", e);
            }

            return res;
        }

        public SierraModel GetPatronInfoFromSierraId(string sierraId)
        {
            SierraModel res = new SierraModel();

            var json = GetDataFromFolioWithRetries("/users?limit=1&query=" + Uri.EscapeDataString("id=" + sierraId));

            if (json != null && json.users != null && json.users.Count == 1)
            {
                var mblockJson = GetDataFromFolioWithRetries("/manualblocks?query=userId=" + json.users[0].id);
                FillInSierraModelFromFolioData(json.users[0], mblockJson, res);
            }

            return res;
        }

        public IList<SierraModel> GetPatrons(string query)
        {
            var res = new List<SierraModel>();

            query = "(personal.email=\"" + query + "*\" or barcode=\"" + query + "*\" or username=\"" + query + "*\" or personal.firstName=\"" + query + 
                "*\" or personal.lastName=\"" + query + "*\")";

            var json = GetDataFromFolioWithRetries("/users?query=" + Uri.EscapeDataString(query));

            if (json != null && json.users != null)
            {
                foreach (var user in json.users)
                {
                    var sierraModelForUser = new SierraModel();
                    FillInSierraModelFromFolioData(user, null, sierraModelForUser, true);
                    res.Add(sierraModelForUser);
                }
            }

            return res;
        }

        #region Private methods

        private dynamic GetDataFromFolioWithRetries(string path)
        {
            dynamic res = null;
            var retry = true;
            var retryCount = 1;
            while (retry)
            {
                try
                {
                    res = GetDataFromFolio(path);
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

        private dynamic GetDataFromFolio(string pathAndQuery)
        {
            dynamic res = null;
            System.Net.ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(_folioApiBaseAddress + pathAndQuery);

            request.Accept = "application/json";
            request.ContentType = "application/json";
            request.Headers["x-okapi-tenant"] = _tenant;
            request.Headers["x-okapi-token"] = _token;

            try
            {
                using (WebResponse response = request.GetResponse())
                {
                    HttpWebResponse httpResponse = (HttpWebResponse)response;

                    using (Stream outputStream = httpResponse.GetResponseStream())
                    using (var sr = new StreamReader(outputStream))
                    {
                        var resultString = sr.ReadToEnd();

                        if (resultString.ToLower().Contains("invalid token"))
                        {
                            throw new InvalidTokenException();
                        }

                        res = JsonConvert.DeserializeObject<dynamic>(resultString);
                    }
                }
            }
            catch (WebException e)
            {
                using (WebResponse response = e.Response)
                {
                    if(response == null)
                    {
                        throw new InvalidTokenException();
                    }

                    HttpWebResponse httpResponse = (HttpWebResponse)response;

                    using (Stream outputStream = httpResponse.GetResponseStream())
                    using (var sr = new StreamReader(outputStream))
                    {
                        var resultString = sr.ReadToEnd();

                        if (resultString.ToLower().Contains("invalid token"))
                        {
                            throw new InvalidTokenException();
                        }
                        else
                        {
                            LogHelper.Error<FolioPatronDataProvider>("WebException on Folio request but no \"invalid token\" in response body.", e);
                            throw e;
                        }
                    }
                }
            }

            return res;
        }

        private void FillInSierraModelFromFolioData(dynamic recordData, dynamic mblockData, /* out */ SierraModel result, bool skipAffiliation = false)
        {
            result.barcode = recordData.barcode;
            result.id = recordData.id;
            if (recordData.personal != null)
            {
                result.email = recordData.personal.email;
                result.first_name = recordData.personal.firstName;
                result.last_name = recordData.personal.lastName;
            }

            result.mblock = mblockData != null ? CalculateMblock(mblockData, recordData.id.ToString()) : "";
            result.ptype = ConvertToSierraPtype(recordData.patronGroup.ToString());
            result.expdate = recordData.expirationDate;
            result.pnum = recordData.username;
            result.active = recordData.active;
            if (!skipAffiliation)
            {
                _affiliationDataProvider.GetAffiliationFromPersonNumber(Convert.ToString(recordData.username), result);
            }
        }

        private int ConvertToSierraPtype(string uuid)
        {
            var res = 50;
            if (uuid == "c568f50b-a7f3-44ac-9f19-335da89ec6bc" || uuid == "f336c902-ff8b-438f-b4c8-efbe435a7304") // Student
            {
                res = 20;
            }
            if (uuid == "5464cbd9-2c7d-4286-8e89-20c75980884b" || uuid == "32bf8ce6-555d-47a7-ab40-de17b40edded" ||
                uuid == "a7528187-78fe-4e33-a89c-c82bd407fcf3") // Anställd
            {
                res = 10;
            }
            return res;
        }

        private string CalculateMblock(dynamic manualblocksResult, string userId)
        {
            var borrowing = false;
            var renewals = false;
            var requests = false;
            for (var i=0; i<manualblocksResult.manualblocks.Count; i++)
            {
                var blockItem = manualblocksResult.manualblocks[i];
                if (blockItem.userId == userId)
                {
                    borrowing |= Convert.ToBoolean(blockItem.borrowing);
                    renewals |= Convert.ToBoolean(blockItem.renewals);
                    requests |= Convert.ToBoolean(blockItem.requests);
                }
            }

            var blockStrings = new List<string>();
            if (borrowing)
            {
                blockStrings.Add("borrowing");
            }
            if (renewals)
            {
                blockStrings.Add("renewals");
            }
            if (requests)
            {
                blockStrings.Add("requests");
            }
            return String.Join(", ", blockStrings);
        }

        #endregion
    }
}