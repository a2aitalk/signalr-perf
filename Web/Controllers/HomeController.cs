using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Web.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        [Route("publisher")]
        public ActionResult Publisher()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        [Route("consumer")]
        public ActionResult Consumer()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}