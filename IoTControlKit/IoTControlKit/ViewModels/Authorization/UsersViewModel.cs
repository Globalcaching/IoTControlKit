using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTControlKit.ViewModels.Authorization
{
    public class UsersViewModelItem : Models.Settings.User
    {
        public string RoleName { get; set; }
    }

    public class UsersViewModel: BaseViewModel
    {
        public List<UsersViewModelItem> Items { get; set; }
        public long CurrentPage { get; set; }
        public long PageCount { get; set; }
        public long TotalCount { get; set; }
    }
}
