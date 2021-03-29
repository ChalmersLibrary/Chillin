using Chalmers.ILL.Models;
using Chalmers.ILL.Patron;
using System.Configuration;
using System.IO;
using System.Net;
using System.Text;

namespace Chalmers.ILL.Repositories
{
    public class FolioRepository : IFolioRepository
    {
        private readonly string _folioApiBaseAddress = ConfigurationManager.AppSettings["folioApiBaseAddress"].ToString();
        private readonly string _tenant = ConfigurationManager.AppSettings["folioXOkapiTenant"].ToString();
        private readonly string _username = ConfigurationManager.AppSettings["folioUsername"].ToString();
        private readonly string _password = ConfigurationManager.AppSettings["folioPassword"].ToString();
        private string _token;

        public string ByQuery(string path) =>
            GetDataFromFolioWithRetries(new FolioRequest(path));

        public string Post(string path, string body) =>
            GetDataFromFolioWithRetries(new FolioRequest(path, "POST", body));

        public string Put(string path, string body) =>
            GetDataFromFolioWithRetries(new FolioRequest(path, "PUT", body, "text/plain"));

        private string GetDataFromFolioWithRetries(FolioRequest folioRequest)
        {
            dynamic res = null;
            var retry = true;
            var retryCount = 1;
            while (retry)
            {
                try
                {
                    res = GetDataFromFolio(folioRequest);
                    retry = false;
                }
                catch (InvalidTokenException)
                {
                    SetToken();
                    retry = retryCount > 0;
                }
                retryCount -= 1;
            }
            return res;
        }

        private string GetDataFromFolio(FolioRequest folioRequest)
        {
            if (string.IsNullOrEmpty(_token))
            {
                SetToken();
            }

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(_folioApiBaseAddress + folioRequest.Path);

            request.Accept = folioRequest.Accept;
            request.ContentType = "application/json";
            request.Headers["x-okapi-tenant"] = _tenant;
            request.Headers["x-okapi-token"] = _token;
            request.Method = folioRequest.Method;

            if (string.IsNullOrEmpty(folioRequest.Body) == false)
            {
                UTF8Encoding encoding = new UTF8Encoding();
                var bodyBytes = encoding.GetBytes(folioRequest.Body);
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

        private void SetToken()
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
        }
    }
}