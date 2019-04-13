using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTControlKit.ViewModels.Flow
{
    public class FlowViewModel: BaseViewModel
    {
        public List<Framework.Models.Flow> Flows { get; set; }
        public List<Framework.Models.FlowComponent> FlowComponents { get; set; }
        public List<Framework.Models.FlowConnector> FlowConnectors { get; set; }
        public List<DevicePropertyViewModel> DeviceProperties { get; set; }
    }
}
