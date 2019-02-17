using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTControlKit.Models.Application
{
    [NPoco.TableName("DevicePropertyValue")]
    public class DevicePropertyValue: BasePoco
    {
        public long DevicePropertyId { get; set; }
        public string Value { get; set; }
        public string LastReceivedValue { get; set; }
        public string LastSetValue { get; set; }
        public DateTime? LastReceivedValueAt { get; set; }
        public DateTime? LastSetValueAt { get; set; }
    }
}
