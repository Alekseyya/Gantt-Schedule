﻿using System.Web.Mvc;

namespace WebApp.Controllers
{
    [AllowAnonymous]
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