using System.Web.Mvc;

namespace WebApp.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Hello";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Hello";

            return View();
        }
    }
}