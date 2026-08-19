using Chalmers.ILL.Members;
using Chalmers.ILL.Models.Page;
using System.Web.Mvc;
using System.Web.Security;

namespace Chalmers.ILL.Controllers.SurfaceControllers.Page
{
    public class ChalmersILLLogoutPageController : Controller
    {
        IMemberInfoManager _memberInfoManager;

        public ChalmersILLLogoutPageController(IMemberInfoManager memberInfoManager)
        {
            _memberInfoManager = memberInfoManager;
        }

        public ActionResult Index()
        {
            var customModel = new ChalmersILLLogoutPageModel();
            _memberInfoManager.PopulateModelWithMemberData(Request, Response, customModel);

            if (User.Identity.IsAuthenticated)
            {
                _memberInfoManager.GetCurrentMemberId(Request, Response);
                FormsAuthentication.SignOut();
                _memberInfoManager.ClearMemberCache(Response);
            }

            return View(customModel);
        }
    }
}
