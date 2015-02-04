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
    class CreateMemberType : IPackageAction
    {
        public string Alias()
        {
            return "CreateMemberType";
        }

        public bool Execute(string packageName, System.Xml.XmlNode xmlData)
        {
            var ret = true;

            try
            {
                MemberType.MakeNew(umbraco.BusinessLogic.User.GetUser(0), "Standard");
            }
            catch (Exception e)
            {
                LogHelper.Error<CreateMediaRootFolder>("Error in CreateMemberType.Execute: ", e);
                ret = false;
            }

            return ret;
        }

        public System.Xml.XmlNode SampleXml()
        {
            var sample = "<Action runat=\"install\" undo=\"true\" alias=\"CreateMemberType\"></Action>";
            return helper.parseStringToXmlNode(sample);
        }

        public bool Undo(string packageName, System.Xml.XmlNode xmlData)
        {
            var ret = true;

            try
            {
                var mt = MemberType.GetByAlias("Standard");
                mt.delete();
            }
            catch (Exception e)
            {
                LogHelper.Error<CreateMediaRootFolder>("Error in CreateMemberType.Undo: ", e);
                ret = false;
            }

            return ret;
        }
    }
}
