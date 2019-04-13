using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTControlKit.ViewModels.Device
{
    public class DeviceControllerViewModelItem: Framework.Models.DeviceController
    {
    }

    public class DeviceControllerViewModel: PagedListViewModel<DeviceControllerViewModelItem>
    {
    }
}
