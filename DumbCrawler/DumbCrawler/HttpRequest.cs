using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace DumbCrawler
{
    public class HttpRequest
    {
        private const string UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/45.0.2454.99 Safari/537.36";

        private readonly Uri _url;

        public HttpRequest(Uri url)
        {
            _url = url;
        }

        public async Task<WebResponse> ProcessAsync()
        {
            var request = WebRequest.Create(_url) as HttpWebRequest;

            if (request == null) return null;

            request.UserAgent = UserAgent;
            request.MaximumAutomaticRedirections = 100;
            //request.Timeout = 1000;

            return await request.GetResponseAsync();
        }

        public WebResponse Process()
        {
            var request = WebRequest.Create(_url) as HttpWebRequest;

            if (request == null) return null;

            request.UserAgent = UserAgent;
            request.MaximumAutomaticRedirections = 100;
            request.Timeout = 1000;

            return request.GetResponse();
        }
    }
}