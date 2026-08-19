using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using Chalmers.ILL.Members;
using System.Configuration;

namespace Chalmers.ILL.Controllers.SurfaceControllers
{
    public class LoginSurfaceController : Controller
    {
        IMemberInfoManager _memberInfoManager;

        public LoginSurfaceController(IMemberInfoManager memberInfoManager)
        {
            _memberInfoManager = memberInfoManager;
        }

        [HttpPost]
        public ActionResult HandleLogin(Models.LoginModel model)
        {
            if (ModelState.IsValid)
            {
                if (Membership.ValidateUser(model.Login, model.Password))
                {
                    FormsAuthentication.SetAuthCookie(model.Login, false);
                    _memberInfoManager.AddMemberToCache(Response, 0, model.Login, model.Login);
                    string redirectUrl;
                    if (Roles.IsUserInRole(model.Login, "Desk"))
                    {
                        redirectUrl = "/disk/?login=ok";
                    }
                    else
                    {
                        redirectUrl = ConfigurationManager.AppSettings["orderListPageUrl"] + "?login=ok";
                    }
                    Response.Redirect(redirectUrl);
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