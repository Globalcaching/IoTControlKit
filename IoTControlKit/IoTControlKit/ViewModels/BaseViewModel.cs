using IoTControlKit.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTControlKit.ViewModels
{

    public class BaseViewModel
    {
        [NPoco.Ignore]
        public NotificationService.Messages NotificationMessages { get; set; }

        [NPoco.Ignore]
        public bool? ResultSuccess { get; set; }

        [NPoco.Ignore]
        public object ResultData { get; set; }
    }
}
