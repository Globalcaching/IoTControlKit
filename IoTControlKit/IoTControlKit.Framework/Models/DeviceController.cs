using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTControlKit.Framework.Models
{
    /// <summary>
    /// A Device Controller is an endpoint which actually controls the device
    /// e.g. a Homey or Domotics
    /// A Device Controller can use multiple means of communications
    /// e.g. http requests or MQTT
    /// </summary>
    [NPoco.TableName("DeviceController")]
    public class DeviceController: BasePoco
    {
        public string Plugin { get; set; }
        public string NormalizedName { get; set; }
        public string Name { get; set; }
        public string State { get; set; }
        public bool Ready { get; set; }
        public bool Enabled { get; set; }
    }
}
