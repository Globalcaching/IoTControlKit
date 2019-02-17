using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTControlKit.ViewModels.Authorization
{
    public class RolesViewModelItem : Models.Settings.Role
    {
    }

    public class RolesViewModel : BaseViewModel
    {
        public List<RolesViewModelItem> Items { get; set; }
        public long CurrentPage { get; set; }
        public long PageCount { get; set; }
        public long TotalCount { get; set; }
    }
}
