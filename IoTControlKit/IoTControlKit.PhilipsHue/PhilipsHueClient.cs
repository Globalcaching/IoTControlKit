using System;
using System.Collections.Generic;
using System.Text;

namespace IoTControlKit.PhilipsHue
{
    [NPoco.TableName("PhilipsHueClient")]
    public class PhilipsHueClient : Framework.Models.BasePoco
    {
        public long DeviceControllerId { get; set; }
        public string AppKey { get; set; }
        public string BridgeId { get; set; }
        public string TcpServer { get; set; }
    }
}
