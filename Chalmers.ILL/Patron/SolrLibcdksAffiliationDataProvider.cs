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
    public class SolrLibcdksAffiliationDataProvider : IAffiliationDataProvider
    {
        public string GetAffiliationFromPersonNumber(string pnum)
        {
            var res = "N/A";

            var fixedPnum = FixPersonNumber(pnum);

            if (!String.IsNullOrEmpty(fixedPnum))
            {
                // Only search on first barcode if there are multiple. Only search on exact pnr with or without dash.
                var query = "pernum:" + fixedPnum;

                try
                {
                    HttpWebRequest fileReq = (HttpWebRequest)HttpWebRequest.Create(ConfigurationManager.AppSettings["patronAffiliationSolrQueryUrl"] + query + "&wt=json");

                    if (!String.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["patronCacheSolrBasicAuthUsername"]) && !String.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["patronCacheSolrBasicAuthPassword"]))
                        fileReq.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(ConfigurationManager.AppSettings["patronCacheSolrBasicAuthUsername"] + ":" + ConfigurationManager.AppSettings["patronCacheSolrBasicAuthPassword"])));

                    fileReq.CookieContainer = new CookieContainer();
                    fileReq.AllowAutoRedirect = true;

                    HttpWebResponse fileResp = (HttpWebResponse)fileReq.GetResponse();
                    var outputStream = fileResp.GetResponseStream();

                    var sr = new StreamReader(outputStream);
                    var json = JsonConvert.DeserializeObject<dynamic>(sr.ReadToEnd());

                    if (json.response.numFound == 1)
                    {
                        res = json.response.docs[0].aff.ToString();
                    }

                }
                catch (Exception)
                {
                    res = "Misslyckad inläsning";
                }
            }

            return res;
        }

        #region Private methods

        private string FixPersonNumber(string pnum)
        {
            var res = pnum.Replace("-", "");
            if (res.Length == 12)
            {
                res = res.Substring(2);
            }
            else if (res.Length < 10)
            {
                res = ""; // We don't want to search with less than 10.
            }

            return res;
        }

        #endregion
    }
}