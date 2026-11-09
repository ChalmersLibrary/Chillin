using System.Collections.Generic;
using System.IO;
using System.Linq;
using Chalmers.ILL.Configuration;
using Chalmers.ILL.Models;
using Newtonsoft.Json;

namespace Chalmers.ILL.Templates
{
    // One JSON file per template under DataPath/templates, named by Id. Started as the isolated
    // mode's fake (fas 6, isolerat läge steg A) and became the only template store when
    // Elasticsearch was removed. Everything except loading/saving is shared logic in
    // TemplateServiceBase (filtering, sorting, moustache substitution).
    public class FileTemplateService : TemplateServiceBase
    {
        private readonly string _dataPath;

        public FileTemplateService(IChillinConfiguration config) : this(config.DataPath)
        {
        }

        public FileTemplateService(string dataPath)
        {
            _dataPath = dataPath;
        }

        private string TemplatesDirectory => Path.Combine(_dataPath, "templates");
        private string TemplatePath(int id) => Path.Combine(TemplatesDirectory, id + ".json");

        protected override IEnumerable<Template> LoadAllTemplates()
        {
            if (!Directory.Exists(TemplatesDirectory))
                return System.Array.Empty<Template>();

            return Directory.GetFiles(TemplatesDirectory, "*.json")
                .Select(f => JsonConvert.DeserializeObject<Template>(File.ReadAllText(f)))
                .ToList();
        }

        // For the one-time move of the templates out of Elasticsearch (Chalmers.ILL.Migration).
        public void Import(Template template) => SaveTemplate(template);

        protected override void SaveTemplate(Template template)
        {
            Directory.CreateDirectory(TemplatesDirectory);
            File.WriteAllText(TemplatePath(template.Id), JsonConvert.SerializeObject(template, Formatting.Indented));
        }
    }
}
