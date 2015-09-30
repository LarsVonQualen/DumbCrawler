using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DumbCrawler.Helpers;
using DumbCrawler.Workers;
using HtmlAgilityPack;

namespace DumbCrawler.Streams
{
    public class ParserStream<TWorker> : BaseStream<Uri, ContentLoad, TWorker> where TWorker : class, IWorker, new()
    {
        private readonly Regex _isUri = new Regex("(https|http)://(.*)");

        protected override void Worker()
        {
            try
            {
                while (!ReturnFeed.Peek())
                {
                    Thread.Yield();
                }

                var contentLoads = ReturnFeed.Next(100);

                var urls = contentLoads.SelectMany(Parsed).ToList();

                EnqueueRange(urls);
            }
            catch (Exception e)
            {
                Error("ParserStream", e);
            }
        }

        private IEnumerable<Uri> Parsed(ContentLoad contentLoad)
        {
            var parsed = new HtmlDocument();
            parsed.LoadHtml(contentLoad.Content);

            var nodes = parsed.DocumentNode.SelectNodes("//a");

            if (nodes == null || !nodes.Any())
            {
                return null;
            }

            var matches = nodes
                .Where(node => node != null)
                .Select(node => node.GetAttributeValue("href", ""))
                .Select(s =>
                {
                    if (s.StartsWith("//"))
                    {
                        return $"{contentLoad.Url.Scheme}:{s}";
                    }

                    if (s.StartsWith("/"))
                    {
                        return $"{contentLoad.Url.Scheme}://{contentLoad.Url.Host}{s}";
                    }

                    return s;
                })
                .Where(s => !string.IsNullOrEmpty(s) && (s.StartsWith("http") || s.StartsWith("https")) && _isUri.IsMatch(s))
                .Where(s =>
                {
                    Uri tmp;

                    return Uri.TryCreate(s, UriKind.Absolute, out tmp);
                })
                .Select(s => new Uri(s))
                .ToList();

            return matches;
        } 
    }
}