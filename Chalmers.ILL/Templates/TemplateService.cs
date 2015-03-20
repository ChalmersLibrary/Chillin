using Chalmers.ILL.Models;
using Examine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

        public string GetTemplateData(string nodeName, OrderItemModel orderItem)
        {
            var searchCriteria = _templateSearcher.CreateSearchCriteria(Examine.SearchCriteria.BooleanOperation.Or);
            var results = _templateSearcher.Search(searchCriteria.NodeName(nodeName).Compile());

            if (results.Count() > 0)
            {
                var template = new StringBuilder(results.First().Fields["Data"]);

                // Search for double moustaches in the template and replace these with the correct order item property value.
                var moustachePattern = new Regex("{{([a-zA-Z0-9]+)}}");
                var matches = moustachePattern.Matches(template.ToString());

                foreach (Match match in matches)
                {
                    var property = match.Groups[1].Value;
                    template.Replace("{{" + property + "}}", orderItem.GetType().GetProperty(property).GetValue(orderItem).ToString());
                }

                return template.ToString();
            }

            throw new TemplateServiceException("Hittade ingen mall med nodnamn=" + nodeName + ".");
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

            list.Sort((x1, x2) => x1.Description.CompareTo(x2.Description)); 

            return list;
        }
    }
}