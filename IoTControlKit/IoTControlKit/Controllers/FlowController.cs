using IoTControlKit.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTControlKit.Controllers
{
    public class FlowController: BaseController
    {
        public FlowController(IHttpContextAccessor httpContextAccessor)
            : base(httpContextAccessor)
        {
        }

        public ActionResult Index()
        {
            var m = Services.BaseService.CompleteViewModel(null);
            m.ResultData = ApplicationService.Instance.GetFlowViewModel();
            return View(m);
        }

    }
}
