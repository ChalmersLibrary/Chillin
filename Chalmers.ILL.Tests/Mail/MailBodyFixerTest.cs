﻿using Chalmers.ILL.Mail;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Chalmers.ILL.Tests.Mail
{
    [TestClass]
    public class MailBodyFixerTest
    {
        [TestMethod]
        public void RemoveHtmlAroundLinks_EmailWithAHrefLinks_RemovedProperly()
        {
            var mailBody = LoadMailTestData("reprints-desk-mail.txt");
            var res = MailBodyFixer.RemoveHtmlAroundLinks(mailBody);
            Assert.AreEqual(LoadMailTestData("reprints-desk-mail-links-removed.txt"), res);
        }

        [TestMethod]
        public void RemoveHtmlAroundLinks_TextEqualsHref_KeepOnlyOne()
        {
            var res = MailBodyFixer.RemoveHtmlAroundLinks(@"<a href=""https://lib.chalmers.se"">https://lib.chalmers.se</a>");
            Assert.AreEqual("https://lib.chalmers.se", res);
        }

        [TestMethod]
        public void RemoveHtmlAroundLinks_NoLinks_AreIdentical()
        {
            var res = MailBodyFixer.RemoveHtmlAroundLinks("Tjosan hejsan!");
            Assert.AreEqual(res, "Tjosan hejsan!");
        }

        [TestMethod]
        public void RemoveHtmlAroundLinks_EmptyString_AreIdentical()
        {
            var res = MailBodyFixer.RemoveHtmlAroundLinks("");
            Assert.AreEqual(res, "");
        }

        [TestMethod]
        public void RemoveHtmlAroundLinks_Null_EmptyString()
        {
            var res = MailBodyFixer.RemoveHtmlAroundLinks(null);
            Assert.AreEqual(res, "");
        }

        #region Private methods

        private string LoadMailTestData(string filename)
        {
            var res = String.Empty;

            var resourceName = "Chalmers.ILL.Tests.Mail.Data." + filename;
            using (var stm = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                if (stm != null)
                {
                    res = new StreamReader(stm).ReadToEnd();
                }
            }

            if (String.IsNullOrEmpty(res))
            {
                throw new Exception("Failed to load publication test data " + filename);
            }

            return res;
        }

        #endregion
    }
}
