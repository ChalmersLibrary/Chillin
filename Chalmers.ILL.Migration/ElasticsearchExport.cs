using System;
using System.Linq;
using System.Net.Http;
using Chalmers.ILL.Models;
using Chalmers.ILL.Repositories;
using Chalmers.ILL.Templates;
using Newtonsoft.Json.Linq;

namespace Chalmers.ILL.Migration
{
    // The templates and the Chillin texts lived in Elasticsearch, not in the SQL database, and
    // Elasticsearch is gone from the app ("Ersätt Elasticsearch med Lucene.NET" in
    // TODO-remove-dotnet-framework.md) - this moves them to the files the app reads now. Plain
    // HTTP against the old server's _search API, read-only; the orders' own ES index isn't needed,
    // the orders come from SQL and the app builds its Lucene index from the order files.
    public class ElasticsearchExport
    {
        private readonly HttpClient _http;
        private readonly string _baseUrl;

        public ElasticsearchExport(HttpClient http, string baseUrl)
        {
            _http = http;
            _baseUrl = baseUrl.TrimEnd('/');
        }

        // ElasticsearchTemplateService read them the same way: every document in the index.
        public int ExportTemplates(string templatesIndex, FileTemplateService target)
        {
            var hits = Search(templatesIndex, 10000);
            foreach (var hit in hits)
                target.Import(hit["_source"].ToObject<Template>());
            return hits.Length;
        }

        // ChillinTextRepository's index: a single document.
        public bool ExportChillinText(FileChillinTextRepository target)
        {
            var hit = Search("chillin_text", 1).FirstOrDefault();
            if (hit == null)
                return false;
            target.Put((string)hit["_id"], hit["_source"].ToObject<ChillinText>());
            return true;
        }

        private JToken[] Search(string index, int size)
        {
            var response = _http.GetAsync($"{_baseUrl}/{Uri.EscapeDataString(index)}/_search?size={size}").GetAwaiter().GetResult();
            var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Elasticsearch svarade {(int)response.StatusCode} för indexet {index}: {body}");
            return JObject.Parse(body)["hits"]["hits"].ToArray();
        }
    }
}
