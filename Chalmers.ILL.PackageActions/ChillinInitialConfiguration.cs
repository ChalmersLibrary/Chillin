using System.Collections.Generic;
using System.Linq;
using System;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core.Models;
using umbraco.cms.businesslogic.packager.standardPackageActions;
using umbraco.interfaces;
using Umbraco.Core.Services;
using Umbraco.Core;
using Umbraco.Core.Logging;
using umbraco.cms.businesslogic.member;
using System.Web.Security;

namespace Chalmers.ILL.PackageActions
{
    public class ChillinInitialConfiguration : IPackageAction
    {
        public string Alias()
        {
            return "ChillinInitialConfiguration";
        }

        public bool Execute(string packageName, System.Xml.XmlNode xmlData)
        {
            var ret = true;
            LogHelper.Info<ChillinInitialConfiguration>("ChillinInitialConfiguration Execute START");
            try
            {
                var ms = ApplicationContext.Current.Services.MediaService;
                var attachmentsFolder = ms.CreateMedia("OrderItemAttachments", -1, "Folder");
                ms.Save(attachmentsFolder);

                var mga = MemberGroup.MakeNew("Administrator", umbraco.BusinessLogic.User.GetUser(0));
                var mgv = MemberGroup.MakeNew("Viewer", umbraco.BusinessLogic.User.GetUser(0));

                MemberType.MakeNew(umbraco.BusinessLogic.User.GetUser(0), "Standard");

                var cs = ApplicationContext.Current.Services.ContentService;
                var rootContent = cs.GetByLevel(1).First();
                cs.PublishWithChildren(rootContent);

                var uh = new Umbraco.Web.UmbracoHelper(Umbraco.Web.UmbracoContext.Current);
                uh.TypedContent(rootContent.Id);

                Access.AddMemberGroupToDocument(rootContent.Id, mga.Id);
                Access.AddMemberGroupToDocument(rootContent.Id, mgv.Id);
            }
            catch (Exception e)
            {
                LogHelper.Error<ChillinInitialConfiguration>("Error in ChillinInitialConfiguration.Execute: ", e);
                ret = false;
            }
            LogHelper.Info<ChillinInitialConfiguration>("ChillinInitialConfiguration Execute END");
            return ret;
        }

        public System.Xml.XmlNode SampleXml()
        {
            var sample = "<Action runat=\"install\" undo=\"true\" alias=\"CreateMediaRootFolder\"></Action>";
            return helper.parseStringToXmlNode(sample);
        }

        public bool Undo(string packageName, System.Xml.XmlNode xmlData)
        {
            var ret = true;
            LogHelper.Info<ChillinInitialConfiguration>("ChillinInitialConfiguration Undo START");
            try
            {
                var ms = ApplicationContext.Current.Services.MediaService;
                var oia = ms.GetChildren(-1).First(m => m.Name == "OrderItemAttachments");
                if (oia != null)
                {
                    ms.Delete(oia);
                }

                var mga = MemberType.GetByAlias("Administrator");
                if (mga != null)
                {
                    mga.delete();
                }

                var mgv = MemberType.GetByAlias("Viewer");
                if (mgv != null)
                {
                    mgv.delete();
                }

                var mt = MemberType.GetByAlias("Standard");
                if (mt != null)
                {
                    mt.delete();
                }

                var cs = ApplicationContext.Current.Services.ContentService;
                cs.UnPublish(cs.GetByLevel(1).First());
            } 
            catch (Exception e)
            {
                LogHelper.Error<ChillinInitialConfiguration>("Error in ChillinInitialConfiguration.Undo: ", e);
                ret = false;
            }
            LogHelper.Info<ChillinInitialConfiguration>("ChillinInitialConfiguration Undo END");
            return ret;
        }
    }
}
