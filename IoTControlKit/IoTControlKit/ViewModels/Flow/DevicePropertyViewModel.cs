using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTControlKit.ViewModels.Flow
{
    public class DevicePropertyViewModel: Framework.Models.DeviceProperty
    {
        public string DeviceName { get; set; }
        public string ControllerName { get; set; }
    }
}
