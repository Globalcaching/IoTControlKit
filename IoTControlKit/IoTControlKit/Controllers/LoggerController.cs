using IoTControlKit.Helpers;
using IoTControlKit.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace IoTControlKit.Controllers
{
    public class LoggerController: BaseController
    {
        public LoggerController(IHttpContextAccessor httpContextAccessor)
            : base(httpContextAccessor)
        {
        }

        public ActionResult Index(long? id)
        {
            if (!AuthorizationService.Instance.FeatureAllowed(CurrentUser, AuthorizationService.Feature.Logging)) return RedirectToAction("Index", "Home");

            var m = BaseService.CompleteViewModel(null);
            m.ResultData = Services.LoggerService.Instance.LastLogs;
            return View(m);
        }

        public ActionResult GetLastLogs(long lastId)
        {
            if (!AuthorizationService.Instance.FeatureAllowed(CurrentUser, AuthorizationService.Feature.Logging)) return null;
            return Json(LoggerService.Instance.GetLastLogs(lastId));
        }

        public ActionResult SetLogLevel(long? id, string level)
        {
            if (!AuthorizationService.Instance.FeatureAllowed(CurrentUser, AuthorizationService.Feature.Logging)) return null;

            LoggerService.Instance.SetLogLevel(level);
            return Json(BaseService.CompleteViewModel(null));
        }

        public ActionResult Download(long? id)
        {
            if (!AuthorizationService.Instance.FeatureAllowed(CurrentUser, AuthorizationService.Feature.Logging)) return null;

            return new FileCallbackResult(new MediaTypeHeaderValue("application/octet-stream"), async (outputStream, _) =>
            {
                //compiler satisfaction
                if (id == -2)
                {
                    await Task.Run(() =>
                    {
                        //nope
                    });
                }
                LoggerService.Instance.Download(outputStream);
            })
            {
                FileDownloadName = "iotcontrolkitlog.zip"
            };
        }

    }
}
