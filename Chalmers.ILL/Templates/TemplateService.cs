using Chalmers.ILL.Models;
using Examine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Umbraco.Core.Services;

namespace Chalmers.ILL.Templates
{
    public class TemplateService : ITemplateService
    {
        IContentService _contentService;
        ISearcher _templateSearcher;

        public TemplateService(IContentService contentService, ISearcher templateSearcher)
        {
            _contentService = contentService;
            _templateSearcher = templateSearcher;
        }

        public string GetTemplateData(int nodeId)
        {
            var searchCriteria = _templateSearcher.CreateSearchCriteria(Examine.SearchCriteria.BooleanOperation.Or);
            var results = _templateSearcher.Search(searchCriteria.Id(nodeId).Compile());

            if (results.Count() > 0)
            {
                return results.First().Fields["Data"];
            }

            throw new TemplateServiceException("Hittade ingen mall med ID=" + nodeId + ".");
        }

        public void SetTemplateData(int nodeId, string data)
        {
            var template = _contentService.GetById(nodeId);

            template.SetValue("data", data);
            _contentService.Save(template);
        }

        public List<Template> PopulateTemplateList(List<Template> list)
        {
            var searchCriteria = _templateSearcher.CreateSearchCriteria(Examine.SearchCriteria.BooleanOperation.Or);
            var results = _templateSearcher.Search(searchCriteria.NodeTypeAlias("ChalmersILLTemplate").Compile());

            foreach (var result in results)
            {
                var template = new Template();
                template.Id = result.Id;
                template.Description = result.Fields["Description"];
                template.Data = result.Fields["Data"];
                list.Add(template);
            }

            return list;
        }
    }
}