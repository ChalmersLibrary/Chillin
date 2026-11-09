using System.IO;
using Chalmers.ILL.Configuration;
using Chalmers.ILL.Models;
using Chalmers.ILL.Models.PartialPage.Settings;
using Newtonsoft.Json;

namespace Chalmers.ILL.Repositories
{
    // A single JSON document under DataPath/chillin_text.json, mirroring the single-document ES
    // index it replaced. Started as the isolated mode's fake (fas 6, isolerat läge steg A) and became
    // the only store when Elasticsearch was removed. A missing file gives an empty ChillinText
    // rather than crashing, since there's no seeding step guaranteed to have run yet.
    public class FileChillinTextRepository : IChillinTextRepository
    {
        private const string DocumentId = "1";
        private readonly string _dataPath;

        public FileChillinTextRepository(IChillinConfiguration config) : this(config.DataPath)
        {
        }

        public FileChillinTextRepository(string dataPath)
        {
            _dataPath = dataPath;
        }

        private string FilePath => Path.Combine(_dataPath, "chillin_text.json");

        public ChillinText ByTextField(string textField)
        {
            return All().Source;
        }

        public ChillinTextDto All()
        {
            if (!File.Exists(FilePath))
            {
                return new ChillinTextDto { Id = DocumentId, Source = new ChillinText() };
            }

            return new ChillinTextDto
            {
                Id = DocumentId,
                Source = JsonConvert.DeserializeObject<ChillinText>(File.ReadAllText(FilePath))
            };
        }

        public void Put(string id, ChillinText chillinText)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
            File.WriteAllText(FilePath, JsonConvert.SerializeObject(chillinText, Formatting.Indented));
        }
    }
}
