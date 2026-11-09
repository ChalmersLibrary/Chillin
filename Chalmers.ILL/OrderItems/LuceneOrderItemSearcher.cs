using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Chalmers.ILL.Configuration;
using Chalmers.ILL.Models;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Chalmers.ILL.OrderItems
{
    // The search index, embedded: a Lucene index on disk under DataPath/search-index, replacing
    // Elasticsearch (and the isolated mode's in-memory fake) - see TODO-remove-dotnet-framework.md,
    // "Ersätt Elasticsearch med Lucene.NET". Lucene is the engine Elasticsearch is built on, and
    // ES's query_string syntax is Lucene's classic query syntax, so every query string the callers
    // already build works unchanged. The few things ES adds on top are added in EsLikeQueryParser.
    //
    // Each order is one document: every leaf value of the order's camelCase JSON (the field names
    // NEST gave the ES documents) as its own field, an "_all" field with all the string values for
    // free text without a field name, and the whole order as stored JSON ("_source", like ES), so a
    // search never has to read the order files.
    //
    // One instance per process, and only one process at a time: Lucene's write.lock on the index
    // directory enforces it. The index is opened lazily on first use (Program.cs does that at
    // startup) and rebuilt from the order files if it's missing or was built by an older
    // SchemaVersion.
    public sealed class LuceneOrderItemSearcher : IOrderItemSearcher, IDisposable
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(LuceneOrderItemSearcher));

        private const LuceneVersion Version = LuceneVersion.LUCENE_48;

        // Bump whenever ToDocument or the analyzer changes - an index built by another version is
        // rebuilt from the order files at startup.
        private const string SchemaVersion = "1";
        private const string SchemaVersionKey = "chillinSchemaVersion";

        // Elasticsearch's index.max_result_window - Search(query) has always meant "at most this many".
        private const int MaxResults = 10000;

        private const string IdField = "_id";
        private const string SourceField = "_source";
        private const string AllField = "_all";
        private const string ExistsField = "_exists_";
        private const string CreateDateSortField = "_createDate";
        private const string ProviderNameKeywordField = "providerName.keyword";

        private static readonly JsonSerializer CamelCase = JsonSerializer.Create(new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        });

        private static readonly Regex ArrayIndex = new Regex(@"\[\d+\]", RegexOptions.Compiled);

        private static readonly Sort CreateDateDescending = new Sort(new SortField(CreateDateSortField, SortFieldType.INT64, true));

        private readonly string _dataPath;
        private readonly object _writeLock = new object();
        private readonly Lazy<(IndexWriter Writer, SearcherManager Searchers)> _index;

        public LuceneOrderItemSearcher(IChillinConfiguration config) : this(config.DataPath)
        {
        }

        public LuceneOrderItemSearcher(string dataPath)
        {
            _dataPath = dataPath;
            _index = new Lazy<(IndexWriter, SearcherManager)>(Open);
        }

        public static string IndexDirectory(string dataPath) => Path.Combine(dataPath, "search-index");

        private IndexWriter Writer => _index.Value.Writer;
        private SearcherManager Searchers => _index.Value.Searchers;

        // Opens the index now instead of on the first search, so a rebuild happens at startup.
        public void EnsureOpen() => _ = _index.Value;

        private (IndexWriter, SearcherManager) Open()
        {
            System.IO.Directory.CreateDirectory(IndexDirectory(_dataPath));
            var config = new IndexWriterConfig(Version, new OrderItemAnalyzer())
            {
                OpenMode = OpenMode.CREATE_OR_APPEND,
                // A restart can overlap the previous process for a moment - wait for its lock.
                WriteLockTimeout = 60000
            };
            var writer = new IndexWriter(FSDirectory.Open(IndexDirectory(_dataPath)), config);

            writer.CommitData.TryGetValue(SchemaVersionKey, out var builtBy);
            if (builtBy != SchemaVersion)
            {
                _log.Info($"Sökindexet saknas eller är byggt av schemaversion '{builtBy}' - bygger om från orderfilerna.");
                var count = Rebuild(writer, OrderFileReader.ReadAll(_dataPath, _log));
                _log.Info($"Sökindexet ombyggt: {count} ordrar.");
            }

            return (writer, new SearcherManager(writer, true, null));
        }

        public void Added(OrderItemModel item) => Write(w => w.UpdateDocument(new Term(IdField, item.NodeId.ToString(CultureInfo.InvariantCulture)), ToDocument(item)));

        public void Modified(OrderItemModel item) => Added(item);

        public void Deleted(OrderItemModel item) => Write(w => w.DeleteDocuments(new Term(IdField, item.NodeId.ToString(CultureInfo.InvariantCulture))));

        // Replaces the whole index with these orders. Searches keep seeing the old index until the
        // new one is committed.
        public int Rebuild(IEnumerable<OrderItemModel> items)
        {
            var count = 0;
            Write(w => count = Rebuild(w, items));
            return count;
        }

        private static int Rebuild(IndexWriter writer, IEnumerable<OrderItemModel> items)
        {
            writer.DeleteAll();
            var count = 0;
            foreach (var item in items)
            {
                writer.AddDocument(ToDocument(item));
                count++;
            }
            Commit(writer);
            return count;
        }

        private void Write(Action<IndexWriter> change)
        {
            lock (_writeLock)
            {
                try
                {
                    change(Writer);
                    Commit(Writer);
                }
                catch (Exception e)
                {
                    throw new OrderItemSearchIndexingException("Failed to update the search index.", e);
                }
                Searchers.MaybeRefreshBlocking();
            }
        }

        private static void Commit(IndexWriter writer)
        {
            writer.SetCommitData(new Dictionary<string, string> { [SchemaVersionKey] = SchemaVersion });
            writer.Commit();
        }

        public IEnumerable<OrderItemModel> Search(string query) => Run(query, 0, MaxResults).Items;

        public SearchResult Search(string query, int start, int size)
        {
            var (count, items) = Run(query, start, size);
            return new SearchResult { Count = count, Items = items };
        }

        // `fields` was an Elasticsearch source filter to save network traffic - the whole order is
        // returned here, which callers only read a couple of properties from anyway.
        public IEnumerable<OrderItemModel> Search(string query, int size, string[] fields) => Run(query, 0, size).Items;

        public IEnumerable<string> AggregatedProviders()
        {
            var res = new List<string> { "TIB", "Libris", "Subito" };

            var query = Parse("NOT status:(Ny OR Annullerad OR Inköpt OR Överförd) AND NOT providerName:(\"libris\" OR \"subito\" OR \"tib\")");
            var counts = new ProviderNameCounter();
            var searcher = Searchers.Acquire();
            try
            {
                searcher.Search(query, counts);
            }
            finally
            {
                Searchers.Release(searcher);
            }

            res.AddRange(counts.Counts.OrderByDescending(c => c.Value).Select(c => c.Key));
            return res;
        }

        private (int Count, List<OrderItemModel> Items) Run(string queryString, int start, int size)
        {
            var query = Parse(queryString);
            if (query == null)
                return (0, new List<OrderItemModel>());

            var searcher = Searchers.Acquire();
            try
            {
                var top = searcher.Search(query, null, Math.Max(1, start + size), CreateDateDescending);
                var items = top.ScoreDocs
                    .Skip(start)
                    .Take(size)
                    .Select(d => JsonConvert.DeserializeObject<OrderItemModel>(searcher.Doc(d.Doc).Get(SourceField)))
                    .ToList();
                return (top.TotalHits, items);
            }
            finally
            {
                Searchers.Release(searcher);
            }
        }

        // A query Elasticsearch would have rejected gives no hits, like the ES client did (NEST
        // returned an invalid response with no hits rather than throwing).
        private static Query Parse(string queryString)
        {
            try
            {
                return new EsLikeQueryParser().ParseEs(queryString);
            }
            catch (Exception e) when (e is ParseException || e is BooleanQuery.TooManyClausesException)
            {
                _log.Warn("Ogiltig sökfråga: " + queryString, e);
                return null;
            }
        }

        private static Document ToDocument(OrderItemModel item)
        {
            var doc = new Document
            {
                new StringField(IdField, item.NodeId.ToString(CultureInfo.InvariantCulture), Field.Store.YES),
                new StoredField(SourceField, JsonConvert.SerializeObject(item)),
                new NumericDocValuesField(CreateDateSortField, item.CreateDate.Ticks),
            };

            if (!string.IsNullOrEmpty(item.ProviderName))
                doc.Add(new SortedDocValuesField(ProviderNameKeywordField, new BytesRef(item.ProviderName)));

            foreach (var value in JObject.FromObject(item, CamelCase).Descendants().OfType<JValue>())
            {
                if (value.Type == JTokenType.Null)
                    continue;

                // ES flattens arrays: logItemsList[3].type is indexed as logItemsList.type.
                var field = ArrayIndex.Replace(value.Path, "");
                doc.Add(new StringField(ExistsField, field, Field.Store.NO));

                if (value.Type == JTokenType.Date)
                {
                    // The clock time as written, whatever its Kind - the range bounds are read the
                    // same way (see EsLikeQueryParser.ParseDate), and that's how ES compared them too.
                    doc.Add(new Int64Field(field, ((DateTime)value.Value).Ticks, Field.Store.NO));
                    continue;
                }

                var text = value.Type == JTokenType.Boolean
                    ? ((bool)value.Value ? "true" : "false")
                    : Convert.ToString(value.Value, CultureInfo.InvariantCulture);
                doc.Add(new TextField(field, text, Field.Store.NO));
                if (value.Type == JTokenType.String)
                    doc.Add(new TextField(AllField, text, Field.Store.NO));
            }

            return doc;
        }

        public void Dispose()
        {
            if (!_index.IsValueCreated)
                return;

            lock (_writeLock)
            {
                Searchers.Dispose();
                Writer.Dispose();
            }
        }

        // Elasticsearch's standard analyzer: Unicode word splitting and lowercasing, no stopwords.
        // The position gap keeps a phrase from matching across two values of the same field (two
        // log entries, or two fields in _all) - 100 like ES.
        private sealed class OrderItemAnalyzer : Analyzer
        {
            protected override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                var source = new StandardTokenizer(Version, reader);
                TokenStream result = new StandardFilter(Version, source);
                result = new LowerCaseFilter(Version, result);
                return new TokenStreamComponents(source, result);
            }

            public override int GetPositionIncrementGap(string fieldName) => 100;
        }

        // The parts of Elasticsearch's query_string that aren't in Lucene's classic parser.
        private sealed class EsLikeQueryParser : QueryParser
        {
            public EsLikeQueryParser() : base(Version, AllField, new OrderItemAnalyzer())
            {
                DefaultOperator = OR_OPERATOR;
                AllowLeadingWildcard = false;
                // A multi-word value (status:01\:Ny -> "01 ny") means those words in a row.
                AutoGeneratePhraseQueries = true;
                // Range bounds keep their case (dates); wildcards are lowercased below instead.
                LowercaseExpandedTerms = false;
            }

            public Query ParseEs(string query)
            {
                if (string.IsNullOrWhiteSpace(query) || query.Trim() == "*")
                    return new MatchAllDocsQuery();
                return WithNegationsMeaningAllExcept(Parse(query));
            }

            // ES: a clause made of nothing but negations (-status:Ny) means "everything except".
            private static Query WithNegationsMeaningAllExcept(Query query)
            {
                if (query is BooleanQuery boolean)
                {
                    foreach (var clause in boolean.Clauses)
                        clause.Query = WithNegationsMeaningAllExcept(clause.Query);
                    if (boolean.Clauses.Count > 0 && boolean.Clauses.All(c => c.Occur == Occur.MUST_NOT))
                        boolean.Add(new MatchAllDocsQuery(), Occur.MUST);
                }
                return query;
            }

            protected override Query GetFieldQuery(string field, string queryText, bool quoted)
            {
                if (field == ExistsField)
                    return new TermQuery(new Term(ExistsField, queryText));
                return base.GetFieldQuery(field, queryText, quoted);
            }

            protected override Query GetPrefixQuery(string field, string termStr) => base.GetPrefixQuery(field, termStr.ToLowerInvariant());

            protected override Query GetWildcardQuery(string field, string termStr) => base.GetWildcardQuery(field, termStr.ToLowerInvariant());

            protected override Query GetRangeQuery(string field, string part1, string part2, bool startInclusive, bool endInclusive)
            {
                var lower = ParseDate(part1);
                var upper = ParseDate(part2);
                if ((lower.HasValue || IsOpen(part1)) && (upper.HasValue || IsOpen(part2)))
                    return NumericRangeQuery.NewInt64Range(field, lower?.Ticks, upper?.Ticks, startInclusive, endInclusive);
                return base.GetRangeQuery(field, part1?.ToLowerInvariant(), part2?.ToLowerInvariant(), startInclusive, endInclusive);
            }

            private static bool IsOpen(string bound) => string.IsNullOrEmpty(bound) || bound == "*";

            private static readonly string[] DateFormats = { "yyyy-MM-ddTHH:mm:ss.fffZ", "yyyy-MM-ddTHH:mm:ssZ", "yyyy-MM-dd", "yyyyMMdd" };

            private static DateTime? ParseDate(string text)
            {
                if (IsOpen(text))
                    return null;
                const DateTimeStyles styles = DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal;
                if (DateTime.TryParseExact(text, DateFormats, CultureInfo.InvariantCulture, styles, out var exact))
                    return exact;
                return null;
            }
        }

        // The ES terms aggregation on providerName.keyword: how many matching orders per provider.
        private sealed class ProviderNameCounter : ICollector
        {
            public Dictionary<string, int> Counts { get; } = new Dictionary<string, int>();
            private SortedDocValues _values;
            private readonly BytesRef _scratch = new BytesRef();

            public bool AcceptsDocsOutOfOrder => true;

            public void SetScorer(Scorer scorer)
            {
            }

            public void SetNextReader(AtomicReaderContext context) => _values = context.AtomicReader.GetSortedDocValues(ProviderNameKeywordField);

            public void Collect(int doc)
            {
                var ord = _values?.GetOrd(doc) ?? -1;
                if (ord < 0)
                    return;
                _values.LookupOrd(ord, _scratch);
                var name = _scratch.Utf8ToString();
                Counts[name] = Counts.TryGetValue(name, out var n) ? n + 1 : 1;
            }
        }
    }
}
