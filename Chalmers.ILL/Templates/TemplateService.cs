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

        public IList<Template> GetManualTemplates()
        {
            var res = new List<Template>();
            var searchCriteria = _templateSearcher.CreateSearchCriteria(Examine.SearchCriteria.BooleanOperation.Or);
            var searchResult = _templateSearcher.Search(searchCriteria.NodeTypeAlias("ChalmersILLTemplate").Compile());
            foreach (var template in searchResult)
            {
                if (template.Fields.ContainsKey("Automatic") && 
                    template.Fields.ContainsKey("Description") && 
                    template.Fields.ContainsKey("Data") && 
                    !Convert.ToBoolean(Convert.ToInt32(template.Fields["Automatic"])))
                res.Add(new Template
                {
                    Id = template.Id,
                    Description = template.Fields["Description"],
                    Data =  template.Fields["Data"]
                });
            }
            return res;
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

        public string GetTemplateData(string nodeName)
        {
            var searchCriteria = _templateSearcher.CreateSearchCriteria(Examine.SearchCriteria.BooleanOperation.Or);
            var results = _templateSearcher.Search(searchCriteria.NodeName(nodeName).Compile());

            if (results.Count() > 0)
            {
                return results.First().Fields["Data"].ToString();
            }

            throw new TemplateServiceException("Hittade ingen mall med nodnamn=" + nodeName + ".");
        }

        public string GetTemplateData(int templateId, OrderItemModel orderItem)
        {
            var res = "Misslyckades med att ladda mall...";
            var searchCriteria = _templateSearcher.CreateSearchCriteria(Examine.SearchCriteria.BooleanOperation.Or);
            var results = _templateSearcher.Search(searchCriteria.Id(templateId).Compile());

            if (results.Count() > 0)
            {
                var templateName = results.First().Fields["nodeName"];
                var templateString = results.First().Fields["Data"];

                res = ReplaceMoustaches(templateName, templateString, orderItem);
            }

            return res;
        }

        public string GetTemplateData(string nodeName, OrderItemModel orderItem)
        {
            return ReplaceMoustaches(nodeName, GetTemplateData(nodeName), orderItem);
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

        public string GetPrettyLibraryNameFromLibraryAbbreviation(string libraryName)
        {
            var res = OrderItemModel.LIBRARY_UNKNOWN_PRETTY_STRING;
            if (libraryName != null && libraryName.Contains("hbib"))
            {
                res = OrderItemModel.LIBRARY_Z_PRETTY_STRING;
            }
            else if (libraryName != null && libraryName.Contains("lbib"))
            {
                res = OrderItemModel.LIBRARY_ZL_PRETTY_STRING;
            }
            else if (libraryName != null && libraryName.Contains("abib"))
            {
                res = OrderItemModel.LIBRARY_ZA_PRETTY_STRING;
            }
            return res;
        }

        #region Private methods

        private string ReplaceMoustaches(string templateName, string templateString, OrderItemModel orderItem)
        {
            var template = new StringBuilder(templateString);

            // Search for double moustaches in the template and replace these with the correct order item property value.
            var moustachePattern = new Regex("{{([a-zA-Z0-9:]+)}}");
            var matches = moustachePattern.Matches(template.ToString());

            foreach (Match match in matches)
            {
                var property = match.Groups[1].Value;

                if (property.StartsWith("T:")) // Other templates that should be injected.
                {
                    var injectedTemplateName = property.Split(':').Last() + "Template";
                    if (injectedTemplateName == templateName) // Do not allow injection of template into itself.
                    {
                        template.Replace("{{" + property + "}}", "{{Injection of template into itself is not allowed}}");
                    }
                    else
                    {
                        template.Replace("{{" + property + "}}", GetTemplateData(injectedTemplateName, orderItem));
                    }
                }
                else if (property.StartsWith("S:")) // Special variables that exists in awkward places.
                {
                    var varName = property.Split(':').Last();
                    if (varName == "HomeLibrary")
                    {
                        template.Replace("{{" + property + "}}", GetPrettyLibraryNameFromLibraryAbbreviation(orderItem.SierraInfo.home_library));
                    }
                }
                else
                {
                    var value = orderItem.GetType().GetProperty(property).GetValue(orderItem);

                    if (value is DateTime)
                    {
                        template.Replace("{{" + property + "}}", ((DateTime)value).ToString("yyyy-MM-dd"));
                    }
                    else
                    {
                        template.Replace("{{" + property + "}}", value.ToString());
                    }
                }
            }

            return template.ToString();
        }

        #endregion
    }
}