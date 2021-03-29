namespace Chalmers.ILL.Models
{
    public class FolioRequest
    {
        public string Accept { get; private set; }
        public string Body { get; private set; }
        public string Method { get; private set; }
        public string Path { get; private set; }

        public FolioRequest(string path, string method = "GET", string body = null, string accept = "application/json")
        {
            Accept = accept;
            Body = body;
            Method = method;
            Path = path;
        }
    }
}