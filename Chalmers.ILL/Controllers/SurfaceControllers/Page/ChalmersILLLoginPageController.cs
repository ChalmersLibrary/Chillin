using System.Web.Mvc;

namespace Chalmers.ILL.Controllers.SurfaceControllers.Page
{
    public class ChalmersILLLoginPageController : Controller
    {
        public ActionResult Index()
        {
            return View("~/Views/ChalmersILLLoginPage.cshtml");
        }
    }
}
