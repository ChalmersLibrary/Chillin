using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Chalmers.ILL.Models.PartialPage
{
    public class ChalmersILLEditTemplatesModel
    {
        public List<Template> Templates { get; set; }

        public ChalmersILLEditTemplatesModel()
        {
            Templates = new List<Template>();
        }
    }
}