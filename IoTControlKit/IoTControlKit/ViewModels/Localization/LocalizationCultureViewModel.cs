using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTControlKit.ViewModels.Localization
{
    public class LocalizationCultureViewModelItem : Models.Localization.LocalizationCulture
    {
    }

    public class LocalizationCultureViewModel : BaseViewModel
    {
        public List<LocalizationCultureViewModelItem> Items { get; set; }
        public long CurrentPage { get; set; }
        public long PageCount { get; set; }
        public long TotalCount { get; set; }
    }

}
