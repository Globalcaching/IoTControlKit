using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTControlKit.ViewModels.Device
{
    public class DeviceControllerViewModelItem: Models.Application.DeviceController
    {
    }

    public class DeviceControllerViewModel: PagedListViewModel<DeviceControllerViewModelItem>
    {
    }
}
