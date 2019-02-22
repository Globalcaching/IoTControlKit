using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTControlKit.Services.Schedulers
{
    public class FlowScheduler: BaseScheduler
    {
        public FlowScheduler()
        {
        }

        public override void Schedule(Dictionary<long, Models.Application.DevicePropertyValue> allPropertyValues, HashSet<long> inputChangedPropertyValues, Dictionary<long, string> outputPropertyValues)
        {
        }
    }
}
