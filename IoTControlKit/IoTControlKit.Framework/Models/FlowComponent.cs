using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTControlKit.Framework.Models
{
    [NPoco.TableName("FlowComponent")]
    public class FlowComponent: BasePoco
    {
        public long FlowId { get; set; }
        public string Type { get; set; } //Trigger, Condition, Action, PassThrough
        public long? DevicePropertyId { get; set; }
        public string Value { get; set; }
        public long PositionX { get; set; }
        public long PositionY { get; set; }
    }
}
