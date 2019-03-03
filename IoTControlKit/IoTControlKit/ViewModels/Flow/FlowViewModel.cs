using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTControlKit.ViewModels.Flow
{
    public class FlowViewModel: BaseViewModel
    {
        public List<Models.Application.Flow> Flows { get; set; }
        public List<Models.Application.FlowComponent> FlowComponents { get; set; }
        public List<Models.Application.FlowConnector> FlowConnectors { get; set; }
        public List<DevicePropertyViewModel> DeviceProperties { get; set; }
    }
}
