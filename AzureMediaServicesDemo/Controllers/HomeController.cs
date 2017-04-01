using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace AzureMediaServicesDemo.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// We get the uploaded file in the HttpPostedFileBase object and the hubid is to differentiate the users
        /// </summary>
        /// <param name="file"></param>
        /// <param name="hubid"></param>
        /// <returns></returns>
        public async Task<bool>Upload(HttpPostedFileBase file, string hubid)
        {
            MediaServcies.InitMediaServices();
            await MediaServcies.UploadMedia(file, hubid);
            return true;
        }

        public ActionResult About()
        {
            ViewBag.Message = "This is not production ready code. If you use this code in production.....do so at your own risk...YO!!";

            return View();
        }

    }
}