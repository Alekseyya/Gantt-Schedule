using System.Web.Mvc;
using Core.Data;
using X.PagedList;

namespace WebApp.Controllers
{
    [Authorize]
    public class WorkPackagePlanVersionController : Controller
    {
        private readonly IWPPlanRepository _wpPlanRepository;

        public WorkPackagePlanVersionController(IWPPlanRepository wpPlanRepository)
        {
            _wpPlanRepository = wpPlanRepository;
        }

        [HttpGet]
        public ActionResult Index(int? page)
        {
            int pageSize = 20;
            int pageNumber = (page ?? 1);
            return View(_wpPlanRepository.GetTemporaryPlans().ToPagedList(pageNumber, pageSize));
        }
    }
}