using IoTControlKit.Helpers;
using IoTControlKit.Models.Localization;
using IoTControlKit.Services;
using IoTControlKit.ViewModels;
using IoTControlKit.ViewModels.Localization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTControlKit.Controllers
{
    public class LocalizationController: BaseController
    {
        public LocalizationController(IHttpContextAccessor httpContextAccessor)
            : base(httpContextAccessor)
        {
        }

        public ActionResult Index()
        {
            if (!AuthorizationService.Instance.FeatureAllowed(CurrentUser, AuthorizationService.Feature.ChangeLocalization)) return RedirectToAction("Index", "Home");
            return View(BaseService.CompleteViewModel(null));
        }

        public ActionResult LocalizationTranslations(long id)
        {
            var m = BaseService.CompleteViewModel(null);
            m.ResultData = (from a in LocalizationService.Instance.AvailableCultures where a.Id == id select a).FirstOrDefault();
            return View(m);
        }

        [HttpPost]
        public ActionResult ChangeLanguage(int id)
        {
            base.SetLanguageId(id);
            return Json(true);
        }

        [HttpPost]
        public ActionResult GetLocalizationCultures(int page, int pageSize, List<string> filterColumns, List<string> filterValues, string sortOn, bool? sortAsc)
        {
            return Json(LocalizationService.Instance.GetLocalizationCultures(page, pageSize, sortOn: sortOn, sortAsc: sortAsc ?? true));
        }

        [HttpPost]
        public ActionResult SaveLocalizationCulture(LocalizationCulture item)
        {
            if (!AuthorizationService.Instance.FeatureAllowed(CurrentUser, AuthorizationService.Feature.ChangeLocalization)) return null;
            LocalizationService.Instance.SaveLocalizationCulture(item);
            return Json(null);
        }

        [HttpPost]
        public ActionResult DeleteLocalizationCulture(LocalizationCulture item)
        {
            if (!AuthorizationService.Instance.FeatureAllowed(CurrentUser, AuthorizationService.Feature.ChangeLocalization)) return null;
            LocalizationService.Instance.DeleteLocalizationCulture(item);
            return Json(null);
        }

        [HttpPost]
        public ActionResult SaveLocalizationTranslation(LocalizationTranslationViewModelItem item)
        {
            if (!AuthorizationService.Instance.FeatureAllowed(CurrentUser, AuthorizationService.Feature.ChangeLocalization)) return null;
            LocalizationService.Instance.SaveLocalizationTranslation(item);
            return Json(null);
        }

        [HttpPost]
        public ActionResult GetLocalizationTranslations(int page, int pageSize, long id, List<string> filterColumns, List<string> filterValues, string sortOn, bool? sortAsc)
        {
            var filterOrg = ControllerHelper.GetFilterValue(filterColumns, filterValues, "OriginalText");
            var filterTrans = ControllerHelper.GetFilterValue(filterColumns, filterValues, "TranslatedText");
            sortOn = ControllerHelper.GetSortField(new Dictionary<string, string>()
            {
                { "OriginalText", "LocalizationOriginalText.OriginalText" }
                , { "TranslatedText", "LocalizationTranslation.TranslatedText" }
            }, sortOn);

            return Json(LocalizationService.Instance.GetLocalizationTranslations(page, pageSize, id, filterOrg: filterOrg, filterTrans: filterTrans, sortOn: sortOn, sortAsc: sortAsc ?? true));
        }

        public ActionResult ExportLocalizationCultureText(long id)
        {
            var item = (from a in LocalizationService.Instance.AvailableCultures where a.Id == id select a).FirstOrDefault();
            return File(System.Text.UTF8Encoding.UTF8.GetBytes(LocalizationService.Instance.ExportLocalizationCultureText(id)), "application/octet-stream", $"{item.Name}.xml");
        }

        public ActionResult ImportLocalizationCultureText()
        {
            if (!AuthorizationService.Instance.FeatureAllowed(CurrentUser, AuthorizationService.Feature.ChangeLocalization)) return null;
            using (var tmp = new TemporaryFile(true))
            {
                var f = Services.FileHandlingService.Instance.UploadFiles(Request, System.IO.Path.GetDirectoryName(tmp.Path), System.IO.Path.GetFileName(tmp.Path));
                LocalizationService.Instance.ImportLocalizationCultureText(System.IO.File.ReadAllText(tmp.Path));
            }
            return Json(null);
        }
    }
}
