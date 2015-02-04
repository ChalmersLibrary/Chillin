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

namespace Chalmers.ILL.PackageActions
{
    class CreateMemberGroups : IPackageAction
    {
        public string Alias()
        {
            return "CreateMemberGroups";
        }

        public bool Execute(string packageName, System.Xml.XmlNode xmlData)
        {
            var ret = true;

            try
            {
                MemberGroup.MakeNew("Administrator", umbraco.BusinessLogic.User.GetUser(0));
                MemberGroup.MakeNew("Viewer", umbraco.BusinessLogic.User.GetUser(0));
            }
            catch (Exception e)
            {
                LogHelper.Error<CreateMediaRootFolder>("Error in CreateMemberGroups.Execute: ", e);
                ret = false;
            }

            return ret;
        }

        public System.Xml.XmlNode SampleXml()
        {
            var sample = "<Action runat=\"install\" undo=\"true\" alias=\"CreateMemberGroups\"></Action>";
            return helper.parseStringToXmlNode(sample);
        }

        public bool Undo(string packageName, System.Xml.XmlNode xmlData)
        {
            var ret = true;

            try
            {
                var mga = MemberType.GetByAlias("Administrator");
                var mgv = MemberType.GetByAlias("Viewer");
                mga.delete();
                mgv.delete();
            }
            catch (Exception e)
            {
                LogHelper.Error<CreateMediaRootFolder>("Error in CreateMemberGroups.Undo: ", e);
                ret = false;
            }

            return ret;
        }
    }
}
