using Chalmers.ILL.Templates;
using Chalmers.ILL.UmbracoApi;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;

namespace Chalmers.ILL.Patron
{
    public class SierraCache : IPatronDataProvider
    {
        IUmbracoWrapper _umbraco;
        ITemplateService _templateService;

        public SierraCache(IUmbracoWrapper umbraco, ITemplateService templateService)
        {
            _umbraco = umbraco;
            _templateService = templateService;
        }

        public IPatronDataProvider Connect()
        {
            // Not needed
            return this;
        }

        public IPatronDataProvider Disconnect()
        {
            // Not needed
            return this;
        }

        public Models.SierraModel GetPatronInfoFromLibraryCardNumber(string barcode)
        {
            return GetPatronInfoFromLibraryCardNumberOrPersonnummer(barcode, barcode);
        }

        public Models.SierraModel GetPatronInfoFromLibraryCardNumberOrPersonnummer(string barcode, string pnr)
        {
            var ret = new Models.SierraModel();

            var pnrWithoutDash = pnr.Replace("-", "").Replace(" ", "");
            var pnrWithDash = pnrWithoutDash.Insert(6, "-").Replace(" ", "");

            var query = "barcode:*" + barcode + "* OR pnum:(*" + pnrWithoutDash + "* OR *" + pnrWithDash + "*)";

            try
            {
                HttpWebRequest fileReq = (HttpWebRequest)HttpWebRequest.Create(ConfigurationManager.AppSettings["patronCacheSolrQueryUrl"] + query + "&wt=json");
                fileReq.CookieContainer = new CookieContainer();
                fileReq.AllowAutoRedirect = true;
                HttpWebResponse fileResp = (HttpWebResponse)fileReq.GetResponse();
                var outputStream = fileResp.GetResponseStream();

                var sr = new StreamReader(outputStream);
                var json = JsonConvert.DeserializeObject<dynamic>(sr.ReadToEnd());

                if (json.response.numFound == 1)
                {
                    var address1 = BuildSierraAddressModel(((string)json.response.docs[0].address));
                    var address2 = BuildSierraAddressModel(((string)json.response.docs[0].address2));
                    var address3 = BuildSierraAddressModel(((string)json.response.docs[0].address3));
                    string patronName = json.response.docs[0].pname;
                    var patronNameParts = patronName.Split(',');
                    string recordId = json.response.docs[0].recordnum;

                    if (address1 != null)
                    {
                        ret.adress.Add(address1);
                    }

                    if (address2 != null)
                    {
                        ret.adress.Add(address2);
                    }

                    if (address3 != null)
                    {
                        ret.adress.Add(address3);
                    }

                    ret.barcode = json.response.docs[0].barcode;
                    ret.home_library = ((string)json.response.docs[0].homelib).Trim();
                    ret.home_library_pretty_name = _templateService.GetPrettyLibraryNameFromLibraryAbbreviation(ret.home_library);
                    ret.id = "0";
                    ret.email = json.response.docs[0].email;

                    if (patronNameParts.Length == 2)
                    {
                        ret.first_name = patronNameParts[1].Trim();
                        ret.last_name = patronNameParts[0].Trim();
                    }
                    else
                    {
                        ret.first_name = patronName;
                    }

                    ret.mblock = json.response.docs[0].mblock;
                    ret.ptype = json.response.docs[0].ptype;
                    ret.record_id = Convert.ToInt32(recordId.Remove(recordId.Length - 1).Remove(0, 1));
                }

            }
            catch (Exception e)
            {
                // NOP
            }

            return ret;
        }

        private Models.SierraAddressModel BuildSierraAddressModel(string address)
        {
            Models.SierraAddressModel ret = null;

            if (!String.IsNullOrWhiteSpace(address))
            {
                ret = new Models.SierraAddressModel();
                var addressParts = address.Split('$');
                ret.addresscount = addressParts.Length.ToString();
                if (addressParts.Length > 0)
                {
                    ret.addr1 = addressParts[0];
                }

                if (addressParts.Length > 1)
                {
                    ret.addr2 = addressParts[1];
                }

                if (addressParts.Length > 2)
                {
                    ret.addr3 = addressParts[2];
                }
            }

            return ret;
        }
    }
}