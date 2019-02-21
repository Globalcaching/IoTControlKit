using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTControlKit.ViewModels.Device
{
    public class DevicePropertyViewModelItem : Models.Application.DeviceProperty
    {
        public string Value { get; set; }
    }

    public class DevicePropertyViewModel: PagedListViewModel<DevicePropertyViewModelItem>
    {
    }
}
