using IoTControlKit.Services;
using IoTControlKit.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace IoTControlKit.Controllers
{
    public class BaseController : Controller
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public readonly Models.Settings.User CurrentUser;

        public BaseController(IHttpContextAccessor httpContextAccessor)
        {
            this._httpContextAccessor = httpContextAccessor;

            CurrentUser = AuthorizationService.Instance.InitializeSession(_httpContextAccessor.HttpContext.Request.Cookies["user"]);

            if (LocalizationService.Instance.CurrentCulture == null)
            {
                string languageValueFromContext = _httpContextAccessor.HttpContext.Request.Cookies["language"];
                if (languageValueFromContext == null)
                {
                    LocalizationService.Instance.CurrentCulture = LocalizationService.Instance.GetLocalizationCulture("en-US");
                    LocalizationService.Instance.CurrentCultureInfo = new System.Globalization.CultureInfo("en-US");
                }
                else
                {
                    var allCultures = LocalizationService.Instance.AvailableCultures;
                    var c = (from a in allCultures where a.Name == languageValueFromContext select a).FirstOrDefault();
                    if (c != null)
                    {
                        LocalizationService.Instance.CurrentCulture = LocalizationService.Instance.GetLocalizationCulture(c.Name);
                        LocalizationService.Instance.CurrentCultureInfo = new System.Globalization.CultureInfo(c.Name);
                    }
                    else
                    {
                        //oeps...
                        LocalizationService.Instance.CurrentCulture = LocalizationService.Instance.GetLocalizationCulture("en-US");
                        LocalizationService.Instance.CurrentCultureInfo = new System.Globalization.CultureInfo("en-US");
                        RemoveCookie("language");
                    }
                }
            }
            LocalizationService.Instance.Initialize();
        }

        protected void SetLanguageId(int id)
        {
            var allCultures = LocalizationService.Instance.AvailableCultures;
            var c = (from a in allCultures where a.Id == id select a).FirstOrDefault();
            if (c != null)
            {
                SetCookie("language", c.Name, null);
                LocalizationService.Instance.CurrentCulture = LocalizationService.Instance.GetLocalizationCulture(c.Name);
                LocalizationService.Instance.CurrentCultureInfo = new System.Globalization.CultureInfo(c.Name);
                LocalizationService.Instance.Initialize();
            }
        }

        protected void SetAuthorizationCookie(string value)
        {
            SetCookie("user", value, null);
        }

        protected void DeleteAuthorizationCookie()
        {
            RemoveCookie("user");
        }

        protected void RemoveCookie(string key)
        {
            Response.Cookies.Delete(key);
        }

        protected void SetCookie(string key, string value, int? expireTime)
        {
            CookieOptions option = new CookieOptions();

            if (expireTime.HasValue)
            {
                option.Expires = DateTime.Now.AddMinutes(expireTime.Value);
            }
            else
            {
                option.Expires = DateTime.Now.AddDays(90);
            }

            Response.Cookies.Append(key, value, option);
        }

        protected string _T(string key, params object[] args)
        {
            return string.Format(LocalizationService.Instance[key], args);
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var controller = RouteData.Values["controller"].ToString();
            if (controller != "Logger")
            {
                var action = RouteData.Values["action"].ToString();
                LoggerService.Instance.LogInformation(null, $"Controller action: {controller}.{action}");
            }
        }

        public override JsonResult Json(object data) // string contentType, System.Text.Encoding contentEncoding, JsonRequestBehavior behavior)
        {
            if (data == null)
            {
                data = new BaseViewModel();
            }
            if (data is BaseViewModel)
            {
                (data as BaseViewModel).NotificationMessages = NotificationService.Instance.GetMessages(HttpContext);
            }

            return base.Json(data);
        }
    }
}
