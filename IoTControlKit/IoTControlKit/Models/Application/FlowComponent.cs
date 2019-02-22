using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTControlKit.Models.Application
{
    [NPoco.TableName("FlowComponent")]
    public class FlowComponent: BasePoco
    {
        public string Guid { get; set; }
        public string Type { get; set; } //Trigger, Condition, Action, PassThrough
        public long DevicePropertyId { get; set; }
        public string Value { get; set; }
    }
}
