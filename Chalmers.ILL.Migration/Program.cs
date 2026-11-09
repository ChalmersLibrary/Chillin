using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using Chalmers.ILL.OrderItems;
using Chalmers.ILL.Repositories;
using Chalmers.ILL.Templates;

namespace Chalmers.ILL.Migration
{
    // CLI entry point for fas 7's "Bygg engångsmigreringen från SQL till filer" - see
    // TODO-remove-dotnet-framework.md. Usage:
    //
    //   dotnet run --project Chalmers.ILL.Migration -- \
    //     --connection-string "<restored copy, NEVER the live database>" \
    //     --data-path /path/to/chillin-data \
    //     [--sample-size 25] \
    //     [--elasticsearch-url http://old-es:9200 --elasticsearch-templates-index chillin-templates]
    //
    // --connection-string must point at a locally restored copy of an exported dump (e.g. a
    // bacpac imported into a throwaway SQL Server instance) - this tool never talks to the live
    // database directly. --elasticsearch-url is the old Elasticsearch server, read only, for the
    // templates and Chillin texts that lived there (see ElasticsearchExport). The search index
    // under --data-path is always rebuilt from the written order files and counted.
    public static class Program
    {
        public static int Main(string[] args)
        {
            var options = ParseArgs(args);
            if (options == null)
            {
                PrintUsage();
                return 1;
            }

            var source = new SqlOrderItemSource(options.ConnectionString);
            var writer = new OrderFileWriter(Path.Combine(options.DataPath, "orders"));

            // A search index left from an earlier run would be opened as is - start over so it's
            // built from the files this run writes.
            var indexDirectory = LuceneOrderItemSearcher.IndexDirectory(options.DataPath);
            if (Directory.Exists(indexDirectory))
                Directory.Delete(indexDirectory, true);

            MigrationResult result;
            using (var searcher = new LuceneOrderItemSearcher(options.DataPath))
            {
                var runner = new MigrationRunner(source, writer, options.SampleSize, searcher);
                result = runner.Run();
            }

            if (!string.IsNullOrEmpty(options.ElasticSearchUrl))
            {
                using var http = new HttpClient();
                var export = new ElasticsearchExport(http, options.ElasticSearchUrl);
                result.ExportedTemplateCount = export.ExportTemplates(options.ElasticSearchTemplatesIndex, new FileTemplateService(options.DataPath));
                result.ExportedChillinText = export.ExportChillinText(new FileChillinTextRepository(options.DataPath));
                if (result.ExportedTemplateCount == 0)
                    result.Errors.Add("Inga mallar hittades i Elasticsearch-indexet " + options.ElasticSearchTemplatesIndex + ".");
            }

            PrintReport(result);
            return result.Success ? 0 : 1;
        }

        private static void PrintReport(MigrationResult result)
        {
            Console.WriteLine();
            Console.WriteLine("=== Migreringsrapport ===");
            Console.WriteLine($"Lästa ordrar (källa):        {result.SourceOrderCount}");
            Console.WriteLine($"Skrivna filer:               {result.WrittenFileCount}");
            Console.WriteLine($"Loggposter (källa):          {result.SourceLogItemCount}");
            Console.WriteLine($"Bilagor (källa):             {result.SourceAttachmentCount}");
            Console.WriteLine($"Högsta NodeId:               {result.MaxNodeId}");
            Console.WriteLine($"Stickprovskontrollerade:     {string.Join(", ", result.SpotCheckedNodeIds)}");
            if (result.SearchIndexDocCount.HasValue)
                Console.WriteLine($"Ordrar i sökindexet:         {result.SearchIndexDocCount}");
            if (result.ExportedTemplateCount.HasValue)
                Console.WriteLine($"Mallar från Elasticsearch:   {result.ExportedTemplateCount}");
            if (result.ExportedChillinText.HasValue)
                Console.WriteLine($"Chillin-texter från ES:      {(result.ExportedChillinText.Value ? "ja" : "fanns inga")}");

            if (result.Success)
            {
                Console.WriteLine();
                Console.WriteLine("OK - alla kontroller gick igenom.");
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine("MISSLYCKADES - följande kontroller slog fel:");
                foreach (var error in result.Errors)
                    Console.WriteLine(" - " + error);
            }
        }

        private static MigrationOptions ParseArgs(string[] args)
        {
            var options = new MigrationOptions();

            for (var i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--connection-string":
                        options.ConnectionString = args[++i];
                        break;
                    case "--data-path":
                        options.DataPath = args[++i];
                        break;
                    case "--sample-size":
                        options.SampleSize = int.Parse(args[++i]);
                        break;
                    case "--elasticsearch-url":
                        options.ElasticSearchUrl = args[++i];
                        break;
                    case "--elasticsearch-templates-index":
                        options.ElasticSearchTemplatesIndex = args[++i];
                        break;
                    default:
                        Console.Error.WriteLine("Okänt argument: " + args[i]);
                        return null;
                }
            }

            if (string.IsNullOrEmpty(options.ConnectionString) || string.IsNullOrEmpty(options.DataPath))
                return null;
            if (!string.IsNullOrEmpty(options.ElasticSearchUrl) && string.IsNullOrEmpty(options.ElasticSearchTemplatesIndex))
                return null;

            return options;
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Användning: --connection-string <conn> --data-path <path> [--sample-size N] [--elasticsearch-url url --elasticsearch-templates-index namn]");
            Console.WriteLine("VIKTIGT: --connection-string ska peka på en lokalt återställd kopia (t.ex. från en bacpac), aldrig direkt på den skarpa databasen.");
        }
    }
}
