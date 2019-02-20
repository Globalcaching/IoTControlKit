using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace IoTControlKit.ViewModels
{
    public class PagedListViewModel<T> : BaseViewModel
    {
        public List<T> Items { get; set; } // object CAN (but does not have to) be a (dynamic object created from) PagedListViewModelItem
        public long CurrentPage { get; set; }
        public long PageCount { get; set; }
        public long TotalCount { get; set; }
    }
}