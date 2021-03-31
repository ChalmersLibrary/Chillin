using Chalmers.ILL.Models;
using Chalmers.ILL.Models.PartialPage.Settings;
using Nest;
using System.Linq;

namespace Chalmers.ILL.Repositories
{
    public class ChillinTextRepository : IChillinTextRepository
    {
        private readonly string Index = "chillin_text";
        private readonly string Type = "chillinText";
        private readonly IElasticClient _elasticClient;

        public ChillinTextRepository(IElasticClient elasticClient)
        {
            _elasticClient = elasticClient;
        }

        public ChillinText ByTextField(string textField)
        {
            var response = _elasticClient.Search<ChillinText>(x => x
                .Index(Index)
                .Type(Type)
                .Size(1)
                .Query(q => q.Bool(b => b.Must(m => m.Exists(e => e.Field(textField)))))
                .Source(s => s.Includes(i => i.Field(textField))));
            return response.Documents.First();
        }

        public ChillinTextDto All()
        {
            var response = _elasticClient.Search<ChillinText>(x => x
                 .Index(Index)
                 .Type(Type)
                 .Size(1)
                 .Query(q => q.MatchAll()));
            IHit<ChillinText> hit = response.Hits.FirstOrDefault();
            return new ChillinTextDto
            {
                Id = hit.Id,
                Source = hit.Source
            };
        }

        public void Put(string id, ChillinText chillinText)
        {
            _elasticClient.Index(chillinText, x => x
                .Index(Index)
                .Type(Type)
                .Id(id)
                .Refresh(new Elasticsearch.Net.Refresh()));
        }
    }
}