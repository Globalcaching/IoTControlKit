using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTControlKit.Models.Application
{
    [NPoco.TableName("DeviceProperty")]
    public class DeviceProperty: BasePoco
    {
        public long DeviceId { get; set; }
        public string NormalizedName { get; set; }
        public string Name { get; set; }
        public bool? Retained { get; set; }
        public bool? Settable { get; set; }
        public string DataType { get; set; }
        public string Unit { get; set; }
        public string Format { get; set; }
    }
}
