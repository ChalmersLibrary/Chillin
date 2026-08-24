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
                        Response.Redirect(Request.Url.AbsolutePath + "?success=true");
                    }
                    catch (Exception)
                    {
                        Response.Redirect(Request.Url.AbsolutePath + "?error=invalid-member");
                    }
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
    }
}