using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTControlKit.ViewModels.Device
{
    public class DeviceViewModelItem: Models.Application.Device
    {
    }

    public class DeviceViewModel: PagedListViewModel<DeviceViewModelItem>
    {
    }
}
