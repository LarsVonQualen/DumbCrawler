using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace DumbCrawler
{
    public class Parser
    {
        private readonly Regex _isUri = new Regex("(https|http)://(.*)");

        public event ErrorEventHandler OnError;

        public async Task<IEnumerable<Uri>> ProcessAsync(WebResponse response)
        {
            try
            {
                var res = response as HttpWebResponse;

                if (res?.StatusCode != HttpStatusCode.OK)
                {
                    Error($"Parser: Bad status code ({res?.StatusCode})");
                    return null;
                }

                if (!res.ContentType.StartsWith(MediaTypeNames.Text.Html))
                {
                    Error($"Parser: Bad content type ({res.ContentType})");
                    return null;
                }

                using (var stream = new StreamReader(res.GetResponseStream()))
                {
                    var content = await stream.ReadToEndAsync();
                    var parsed = new HtmlDocument();
                    await Task.Run(() => parsed.LoadHtml(content));

                    var nodes = await Task.Run(() => parsed.DocumentNode.SelectNodes("//a"));

                    if (!nodes.Any())
                    {
                        return null;
                    }

                    var matches = nodes
                        .Where(node => node != null)
                        .Select(node => node.GetAttributeValue("href", ""))
                        .AsParallel()
                        .Select(s =>
                        {
                            if (s.StartsWith("//"))
                            {
                                return $"{res.ResponseUri.Scheme}:{s}";
                            }

                            if (s.StartsWith("/"))
                            {
                                return $"{res.ResponseUri.Scheme}://{res.ResponseUri.Host}{s}";
                            }

                            return s;
                        })
                        .Where(s => !string.IsNullOrEmpty(s) && (s.StartsWith("http") || s.StartsWith("https")) && _isUri.IsMatch(s))
                        .Select(s => new Uri(s))
                        .ToList();

                    return matches;
                }
            }
            catch (Exception e)
            {
                Error($"Parser: Unhandled exception ({e.Message})", e);
                return null;
            }
        }

        public IEnumerable<Uri> Process(WebResponse response)
        {
            try
            {
                var res = response as HttpWebResponse;

                if (res?.StatusCode != HttpStatusCode.OK)
                {
                    Error($"Parser: Bad status code ({res?.StatusCode})");
                    return null;
                }

                if (!res.ContentType.StartsWith(MediaTypeNames.Text.Html))
                {
                    Error($"Parser: Bad content type ({res.ContentType})");
                    return null;
                }

                using (var stream = new StreamReader(res.GetResponseStream()))
                {
                    var content = stream.ReadToEnd();
                    var parsed = new HtmlDocument();
                    parsed.LoadHtml(content);

                    var nodes = parsed.DocumentNode.SelectNodes("//a");

                    if (!nodes.Any())
                    {
                        return null;
                    }

                    var matches = nodes
                        .Where(node => node != null)
                        .Select(node => node.GetAttributeValue("href", ""))
                        .AsParallel()
                        .Select(s =>
                        {
                            if (s.StartsWith("//"))
                            {
                                return $"{res.ResponseUri.Scheme}:{s}";
                            }

                            if (s.StartsWith("/"))
                            {
                                return $"{res.ResponseUri.Scheme}://{res.ResponseUri.Host}{s}";
                            }

                            return s;
                        })
                        .Where(s => !string.IsNullOrEmpty(s) && (s.StartsWith("http") || s.StartsWith("https")) && _isUri.IsMatch(s))
                        .Select(s => new Uri(s))
                        .ToList();

                    return matches;
                }
            }
            catch (Exception e)
            {
                Error($"Parser: Unhandled exception ({e.Message})", e);
                return null;
            }
        }

        private void Error(string message, Exception e = null)
        {
            OnError?.Invoke(this, new ErrorEventArgs(new Exception(message, e)));
        }
    }
}