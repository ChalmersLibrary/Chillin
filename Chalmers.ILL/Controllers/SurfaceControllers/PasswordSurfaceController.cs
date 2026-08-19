using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using umbraco.cms.businesslogic.member;
using System.Security.Cryptography;
using Chalmers.ILL.Members;

namespace Chalmers.ILL.Controllers.SurfaceControllers
{
    public class PasswordSurfaceController : Controller
    {
        IMemberInfoManager _memberInfoManager;

        public PasswordSurfaceController(IMemberInfoManager memberInfoManager)
        {
            _memberInfoManager = memberInfoManager;
        }

        [HttpGet]
        public ActionResult RenderChangePasswordAction()
        {
            return PartialView("Settings/ChangePassword", new Models.PartialPage.Settings.ChangePassword());
        }

        [HttpPost]
        public ActionResult ChangePassword(Models.PartialPage.Settings.ChangePassword model)
        {
            if (ModelState.IsValid)
            {
                // Get Member from LoginName and CurrentPassword provided in form/Model
                var m = Member.GetMemberFromLoginNameAndPassword(_memberInfoManager.GetCurrentMemberLoginName(Request, Response), model.CurrentPassword);

                // If this computes to a real Member, change the password to NewPassword from form/Model
                if (m != null)
                {
                    m.ChangePassword(HashPassword(model.NewPassword));
                    m.Save();
                    Response.Redirect(Request.Url.AbsolutePath + "?success=true");
                }
                else
                {
                    Response.Redirect(Request.Url.AbsolutePath + "?error=invalid-member");
                }
            }
            else
            {
                Response.Redirect(Request.Url.AbsolutePath + "?error=invalid-model");
            }

            return Redirect(Request.Url.AbsolutePath);
        }

        // Compute Hash for provided NewPassword as it is stored hashed
        // From: http://silogic.co.uk/january-2013/change-member-password-in-umbraco.aspx
        string HashPassword(string password)
        {
            HMACSHA1 hash = new HMACSHA1();
            hash.Key = System.Text.Encoding.Unicode.GetBytes(password);
            string encodedPassword = Convert.ToBase64String(hash.ComputeHash(System.Text.Encoding.Unicode.GetBytes(password)));
            return encodedPassword;
        }
    }
}