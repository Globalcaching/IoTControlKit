using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTControlKit.ViewModels.Localization
{
    public class LocalizationCultureViewModelItem : Models.Localization.LocalizationCulture
    {
    }

    public class LocalizationCultureViewModel : PagedListViewModel<LocalizationCultureViewModelItem>
    {
    }

}
