using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTControlKit.Controllers
{
    public class HomeController: BaseController
    {
        public HomeController(IHttpContextAccessor httpContextAccessor)
            : base(httpContextAccessor)
        {
        }

        public ActionResult Index()
        {
            return View(Services.BaseService.CompleteViewModel(null));
        }

    }
}
