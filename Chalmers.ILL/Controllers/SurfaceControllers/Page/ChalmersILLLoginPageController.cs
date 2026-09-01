using System.Web.Mvc;

namespace Chalmers.ILL.Controllers.SurfaceControllers.Page
{
    [AllowAnonymous]
    public class ChalmersILLLoginPageController : Controller
    {
        public ActionResult Index()
        {
            return View("~/Views/ChalmersILLLoginPage.cshtml");
        }
    }
}
