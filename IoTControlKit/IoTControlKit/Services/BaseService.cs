using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTControlKit.Services
{
    public class BaseService
    {
        protected string _T(string key, params object[] args)
        {
            return string.Format(LocalizationService.Instance[key], args);
        }

        public static ViewModels.BaseViewModel CompleteViewModel(ViewModels.BaseViewModel model)
        {
            var result = model ?? new ViewModels.BaseViewModel();
            return result;
        }

    }

}
