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
        string GetTemplateData(string nodeName);
        string GetTemplateData(int nodeId);
        void SetTemplateData(int nodeId, string data);
        List<Template> PopulateTemplateList(List<Template> list);
    }
}
