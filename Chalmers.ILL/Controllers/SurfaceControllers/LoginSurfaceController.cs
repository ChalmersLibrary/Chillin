using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using umbraco.cms.businesslogic.member;
using Umbraco.Web.Mvc;
using Umbraco.Web;
using Chalmers.ILL.Wrappers;
using System.Configuration;

namespace Chalmers.ILL.Controllers.SurfaceControllers
{
    public class LoginSurfaceController : SurfaceController
    {
        [HttpPost]
        public ActionResult HandleLogin(Models.LoginModel model)
        {
            if (ModelState.IsValid)
            {
                var m = Member.GetMemberFromLoginNameAndPassword(model.Login, model.Password);
                if (m != null)
                {
                    Member.AddMemberToCache(m);
                    MemberWrapper.AddMemberToCache(Response, m);
                    string redirectUrl = Umbraco.TypedContentAtXPath("//" + ConfigurationManager.AppSettings["umbracoOrderListPageContentDocumentType"]).First().Url + "?login=ok";
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