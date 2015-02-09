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
using umbraco.cms.businesslogic.web;
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

            try
            {
                var ms = ApplicationContext.Current.Services.MediaService;
                var attachmentsFolder = ms.CreateMedia("OrderItemAttachments", -1, "Folder");
                ms.Save(attachmentsFolder);

                var mga = MemberGroup.MakeNew("Administrator", umbraco.BusinessLogic.User.GetUser(0));
                var mgv = MemberGroup.MakeNew("Viewer", umbraco.BusinessLogic.User.GetUser(0));

                MemberType.MakeNew(umbraco.BusinessLogic.User.GetUser(0), "Standard");

                var cs = ApplicationContext.Current.Services.ContentService;
                var uh = new Umbraco.Web.UmbracoHelper(Umbraco.Web.UmbracoContext.Current);
                var rootContent = cs.GetByLevel(1).First();

                cs.PublishWithChildren(rootContent);

                var loginPage = uh.TypedContentAtXPath("//ChalmersILLLoginPage").First();

                Access.ProtectPage(false, rootContent.Id, loginPage.Id, loginPage.Id);
                Access.AddMembershipRoleToDocument(rootContent.Id, "Administrator");
                Access.AddMembershipRoleToDocument(rootContent.Id, "Viewer");

                umbraco.uQuery.SqlHelper.ExecuteNonQuery(
                    string.Format("INSERT INTO umbracoRelationType ([dual], parentObjectType, childObjectType, name, alias) VALUES ({0}, '{1}', '{2}', '{3}', '{4}')",
                        1,
                        "39EB0F98-B348-42A1-8662-E7EB18487560",
                        "C66BA18E-EAF3-4CFF-8A22-41B16D66A972",
                        "Member Locked",
                        "memberLocked"));
            }
            catch (Exception e)
            {
                LogHelper.Error<ChillinInitialConfiguration>("Error in ChillinInitialConfiguration.Execute: ", e);
                ret = false;
            }

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

            try
            {
                var ms = ApplicationContext.Current.Services.MediaService;
                var cs = ApplicationContext.Current.Services.ContentService;
                var oia = ms.GetChildren(-1).FirstOrDefault(m => m.Name == "OrderItemAttachments");
                if (oia != null)
                {
                    ms.Delete(oia);
                }

                var uh = new Umbraco.Web.UmbracoHelper(Umbraco.Web.UmbracoContext.Current);  
                var rootContent = uh.ContentAtXPath("//ChalmersILL").FirstOrDefault();
                if (rootContent != null)
                {
                    Access.RemoveMembershipRoleFromDocument(rootContent.Id, "Administrator");
                    Access.RemoveMembershipRoleFromDocument(rootContent.Id, "Viewer");
                    Access.RemoveProtection(rootContent.Id);
                    cs.UnPublish(rootContent);
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

                umbraco.uQuery.SqlHelper.ExecuteNonQuery(string.Format("DELETE FROM umbracoRelationType WHERE alias='{0}'", "memberLocked"));
            } 
            catch (Exception e)
            {
                LogHelper.Error<ChillinInitialConfiguration>("Error in ChillinInitialConfiguration.Undo: ", e);
                ret = false;
            }

            return ret;
        }
    }
}
