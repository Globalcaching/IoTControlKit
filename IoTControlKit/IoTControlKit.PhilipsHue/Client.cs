using Q42.HueApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IoTControlKit.PhilipsHue
{
    public class Client : IDisposable
    {
        protected PhilipsHueClient _clientSetting { get; private set; }
        protected LocalHueClient _client { get; private set; }
        protected Bridge _bridge { get; private set; }
        protected Framework.Models.DeviceController _deviceController { get; private set; }

        public Client(PhilipsHueClient poco)
        {
            _clientSetting = poco;
        }

        public PhilipsHueClient ClientSetting => _clientSetting;

        public void Dispose()
        {
        }

        public void Start()
        {
            try
            {
                var ip = _clientSetting.TcpServer;
                if (_clientSetting.BridgeId != "Manual")
                {
                    //try finding bridge
                    var locator = new HttpBridgeLocator();
                    var bridges = locator.LocateBridgesAsync(TimeSpan.FromSeconds(5)).Result;
                    var b = (from a in bridges where a.BridgeId == _clientSetting.BridgeId select a).FirstOrDefault();
                    if (b != null)
                    {
                        ip = b.IpAddress;
                    }
                }
                _client = new LocalHueClient(ip, _clientSetting.AppKey);
                _bridge = _client.GetBridgeAsync().Result;

            }
            catch
            {
            }
        }
    }
}
