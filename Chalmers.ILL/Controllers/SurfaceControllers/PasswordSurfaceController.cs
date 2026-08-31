using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using Chalmers.ILL.Members;

namespace Chalmers.ILL.Controllers.SurfaceControllers
{
    public class PasswordSurfaceController : Controller
    {
        // The form posts here directly (not to the settings page itself), so redirects must name
        // the settings page explicitly instead of reusing Request.Url.AbsolutePath.
        const string SettingsPageUrl = "/bestaellningar/instaellningar/";

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
                var loginName = _memberInfoManager.GetCurrentMemberLoginName(Request, Response);

                // Validate the current password via the configured membership provider
                if (Membership.ValidateUser(loginName, model.CurrentPassword))
                {
                    try
                    {
                        var user = Membership.GetUser(loginName);
                        user.ChangePassword(model.CurrentPassword, model.NewPassword);
                        Response.Redirect(SettingsPageUrl + "?success=true");
                    }
                    catch (Exception)
                    {
                        Response.Redirect(SettingsPageUrl + "?error=invalid-member");
                    }
                }
                else
                {
                    Response.Redirect(SettingsPageUrl + "?error=invalid-member");
                }
            }
            else
            {
                Response.Redirect(SettingsPageUrl + "?error=invalid-model");
            }

            return Redirect(SettingsPageUrl);
        }
    }
}