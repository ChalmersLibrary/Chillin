using Chalmers.ILL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chalmers.ILL.Templates
{
    public interface ITemplateService
    {
        IList<Template> GetManualTemplates();
        string GetTemplateData(int nodeId);
        string GetTemplateData(string nodeName);
        string GetTemplateData(string nodeName, OrderItemModel orderItem);
        string GetTemplateData(int templateId, OrderItemModel orderItem);
        void SetTemplateData(int nodeId, string data);
        List<Template> PopulateTemplateList(List<Template> list);
        string GetPrettyLibraryNameFromLibraryAbbreviation(string libraryName);
    }
}
