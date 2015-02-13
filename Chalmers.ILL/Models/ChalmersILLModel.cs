using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using Umbraco.Core.Models;
using Umbraco.Web;
using Umbraco.Web.Models;

namespace Chalmers.ILL.Models
{
    public class ChalmersILLModel : RenderModel
    {
        public ChalmersILLModel() : this(new UmbracoHelper(UmbracoContext.Current).TypedContent(UmbracoContext.Current.PageId)) { }
        public ChalmersILLModel(IPublishedContent content, CultureInfo culture) : base(content, culture) { }
        public ChalmersILLModel(IPublishedContent content) : base(content) { }

        public int CurrentMemberId { get; set; }
        public string CurrentMemberText { get; set; }
        public string CurrentMemberLoginName { get; set; }
    }
}