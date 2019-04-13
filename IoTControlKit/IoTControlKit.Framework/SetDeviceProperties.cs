using System;
using System.Collections.Generic;
using System.Text;

namespace IoTControlKit.Framework
{
    public class SetDeviceProperties
    {
        public delegate void SetDevicePropertyValueHandler(NPoco.Database db, List<SetDeviceProperties> properties);

        public long DevicePropertyId { get; set; }
        public string Value { get; set; }
        public bool InternalOnly { get; set; } = false;
    }
}
