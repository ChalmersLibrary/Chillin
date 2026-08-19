using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using Umbraco.Web.Mvc;
using Umbraco.Web;
using Chalmers.ILL.Members;
using System.Configuration;

namespace Chalmers.ILL.Controllers.SurfaceControllers
{
    public class LoginSurfaceController : SurfaceController
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
                        redirectUrl = Umbraco.TypedContentAtXPath("//" + ConfigurationManager.AppSettings["umbracoOrderListPageContentDocumentType"]).First().Url + "?login=ok";
                    }
                    Response.Redirect(redirectUrl);
                }
                else
                {
                    Response.Redirect(CurrentPage.Url + "?error=invalid-member");
                }
            }
            else
            {
                Response.Redirect(CurrentPage.Url + "?error=invalid-model");
            }
            return RedirectToCurrentUmbracoPage();
        }
    }
}