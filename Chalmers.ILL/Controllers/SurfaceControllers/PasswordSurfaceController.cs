using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using umbraco.cms.businesslogic.member;
using Umbraco.Web.Mvc;
using Umbraco.Web;
using System.Security.Cryptography;
using Chalmers.ILL.Models;
using Chalmers.ILL.Wrappers;

namespace Chalmers.ILL.Controllers.SurfaceControllers
{
    public class PasswordSurfaceController : SurfaceController
    {
        [HttpPost]
        public ActionResult ChangePassword(Models.PasswordModel model)
        {
            if (ModelState.IsValid)
            {
                // Get Member from LoginName and CurrentPassword provided in form/Model
                var m = Member.GetMemberFromLoginNameAndPassword(MemberWrapper.GetCurrentMemberLoginName(Request, Response), model.CurrentPassword);

                // If this computes to a real Member, change the password to NewPassword from form/Model
                if (m != null)
                {
                    m.ChangePassword(HashPassword(model.NewPassword));
                    m.Save();
                    Response.Redirect(CurrentPage.Url + "?success=true");
                }

                // Login+CurrentPassword doesn't match up to a Member
                else
                {
                    Response.Redirect(CurrentPage.Url + "?error=invalid-member");
                }
            }

            // Something is missing in the Model, CurrentPassword or NewPassword
            else
            {
                Response.Redirect(CurrentPage.Url + "?error=invalid-model");
            }

            return RedirectToCurrentUmbracoPage();
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