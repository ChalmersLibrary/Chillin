using Chalmers.ILL.Members;
using Chalmers.ILL.Models.Page;
using System.Web.Mvc;

namespace Chalmers.ILL.Controllers.SurfaceControllers.Page
{
    public class ChalmersILLController : Controller
    {
        IMemberInfoManager _memberInfoManager;

        public ChalmersILLController(IMemberInfoManager memberInfoManager)
        {
            _memberInfoManager = memberInfoManager;
        }

        public ActionResult Index()
        {
            var customModel = new ChalmersILLModel();
            _memberInfoManager.PopulateModelWithMemberData(Request, Response, customModel);
            return View("~/Views/ChalmersILL.cshtml", customModel);
        }
    }
}
