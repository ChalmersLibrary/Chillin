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

namespace Chalmers.ILL.PackageActions
{
    public class CreateMediaRootFolder : IPackageAction
    {
        public string Alias()
        {
            return "CreateMediaRootFolder";
        }

        public bool Execute(string packageName, System.Xml.XmlNode xmlData)
        {
            var ret = true;

            try
            {
                var ms = ApplicationContext.Current.Services.MediaService;
                var attachmentsFolder = ms.CreateMedia("OrderItemAttachments", -1, "Folder");
                ms.Save(attachmentsFolder);
            }
            catch (Exception e)
            {
                LogHelper.Error<CreateMediaRootFolder>("Error in CreateMediaRootFolder.Execute: ", e);
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
                ms.Delete(ms.GetMediaByPath("OrderItemAttachments"));
            } 
            catch (Exception e)
            {
                LogHelper.Error<CreateMediaRootFolder>("Error in CreateMediaRootFolder.Undo: ", e);
                ret = false;
            }

            return ret;
        }
    }
}
