using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTControlKit.Services.Schedulers
{
    public class BaseScheduler
    {
        public BaseScheduler()
        {
        }

        public virtual void Schedule(Dictionary<long, Framework.Models.DevicePropertyValue> allPropertyValues, HashSet<long> inputChangedPropertyValues, Dictionary<long, string> outputPropertyValues)
        {
        }
    }
}
