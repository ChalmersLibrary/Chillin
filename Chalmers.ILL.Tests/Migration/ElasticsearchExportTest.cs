using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Chalmers.ILL.Migration;
using Chalmers.ILL.Repositories;
using Chalmers.ILL.Templates;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chalmers.ILL.Tests.Migration
{
    // The templates and Chillin texts moving out of Elasticsearch into files. The ES responses
    // below are shaped like ES 6's _search output, with the camelCase sources NEST wrote.
    [TestClass]
    public class ElasticsearchExportTest
    {
        private string _dataPath;

        [TestInitialize]
        public void Setup()
        {
            _dataPath = Path.Combine(Path.GetTempPath(), "chillin-esexport-test-" + Guid.NewGuid());
        }

        private static ElasticsearchExport ExportFrom(Dictionary<string, string> responses)
        {
            var http = new HttpClient(new StubHandler(request =>
            {
                var key = responses.Keys.FirstOrDefault(k => request.RequestUri.AbsolutePath.StartsWith(k));
                return key == null
                    ? new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("{\"error\":\"index_not_found\"}") }
                    : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(responses[key]) };
            }));
            return new ElasticsearchExport(http, "http://old-es:9200/");
        }

        [TestMethod]
        public void ExportTemplates_WritesEveryTemplateAsAFileTheAppReads()
        {
            var export = ExportFrom(new Dictionary<string, string>
            {
                ["/chillin-templates/_search"] = @"{""hits"":{""total"":2,""hits"":[
                    {""_id"":""11"",""_source"":{""id"":11,""nodeName"":""DeliveryTemplate"",""description"":""Leverans"",""data"":""Hej {{PatronName}}"",""automatic"":true,""acquisition"":false}},
                    {""_id"":""12"",""_source"":{""id"":12,""nodeName"":""NodeName12"",""description"":""Åter"",""data"":""Tack"",""automatic"":false,""acquisition"":true}}
                ]}}"
            });

            var count = export.ExportTemplates("chillin-templates", new FileTemplateService(_dataPath));

            Assert.AreEqual(2, count);
            var templates = new FileTemplateService(_dataPath);
            Assert.AreEqual("Hej {{PatronName}}", templates.GetTemplateData("DeliveryTemplate"));
            Assert.AreEqual("Åter", templates.GetManualTemplates().Single().Description);
        }

        [TestMethod]
        public void ExportChillinText_WritesTheSingleDocument()
        {
            var export = ExportFrom(new Dictionary<string, string>
            {
                ["/chillin_text/_search"] = @"{""hits"":{""total"":1,""hits"":[
                    {""_id"":""AWx1"",""_source"":{""checkInNote"":""In"",""checkOutNote"":""Ut"",""standardTitleText"":""Titel""}}
                ]}}"
            });

            Assert.IsTrue(export.ExportChillinText(new FileChillinTextRepository(_dataPath)));

            var text = new FileChillinTextRepository(_dataPath).All().Source;
            Assert.AreEqual("In", text.CheckInNote);
            Assert.AreEqual("Ut", text.CheckOutNote);
            Assert.AreEqual("Titel", text.StandardTitleText);
        }

        [TestMethod]
        public void ExportChillinText_EmptyIndex_WritesNothing()
        {
            var export = ExportFrom(new Dictionary<string, string>
            {
                ["/chillin_text/_search"] = @"{""hits"":{""total"":0,""hits"":[]}}"
            });

            Assert.IsFalse(export.ExportChillinText(new FileChillinTextRepository(_dataPath)));
            Assert.IsFalse(File.Exists(Path.Combine(_dataPath, "chillin_text.json")));
        }

        [TestMethod]
        public void ExportTemplates_MissingIndex_Throws()
        {
            var export = ExportFrom(new Dictionary<string, string>());

            Assert.ThrowsException<InvalidOperationException>(() =>
                export.ExportTemplates("wrong-index", new FileTemplateService(_dataPath)));
        }

        private sealed class StubHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;
            public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) => _respond = respond;

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
                Task.FromResult(_respond(request));
        }
    }
}
