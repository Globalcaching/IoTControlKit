using IoTControlKit.Helpers;
using IoTControlKit.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTControlKit.Controllers
{
    public class DeviceController: BaseController
    {
        public DeviceController(IHttpContextAccessor httpContextAccessor)
            : base(httpContextAccessor)
        {
        }

        public ActionResult Index()
        {
            return View(Services.BaseService.CompleteViewModel(null));
        }

        [HttpPost]
        public ActionResult GetDeviceControllers(int page, int pageSize, List<string> filterColumns, List<string> filterValues, string sortOn, bool? sortAsc)
        {
            return Json(ApplicationService.Instance.GetDeviceControllers(page, pageSize, sortOn: sortOn, sortAsc: sortAsc ?? true));
        }

        [HttpPost]
        public ActionResult GetDevices(long id, int page, int pageSize, List<string> filterColumns, List<string> filterValues, string sortOn, bool? sortAsc)
        {
            var filterName = ControllerHelper.GetFilterValue(filterColumns, filterValues, "Name");
            return Json(ApplicationService.Instance.GetDevices(page, pageSize, id, filterName, sortOn: sortOn, sortAsc: sortAsc ?? true));
        }

        [HttpPost]
        public ActionResult GetDeviceProperties(long id, int page, int pageSize, List<string> filterColumns, List<string> filterValues, string sortOn, bool? sortAsc)
        {
            return Json(ApplicationService.Instance.GetDeviceProperties(page, pageSize, id, sortOn: sortOn, sortAsc: sortAsc ?? true));
        }
    }
}
