using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTControlKit.ViewModels.Localization
{
    public class LocalizationTranslationViewModelItem : Models.Localization.LocalizationTranslation
    {
        public long CultureId { get; set; }
        public long OriginalTextId { get; set; }
        public string OriginalText { get; set; }
        public string LocalizationCultureName { get; set; }
    }

    public class LocalizationTranslationViewModel : PagedListViewModel<LocalizationTranslationViewModelItem>
    {
    }

}
