using IoTControlKit.Framework;
using IoTControlKit.PhilipsHue;
using Q42.HueApi;
using Q42.HueApi.Interfaces;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTControlKit.Plugin.PhilipsHue
{
    public class Plugin : IPlugin
    {
        public static Plugin Instance { get; private set; }

        public IApplication Application { get; private set; }
        public IDatabase Database { get; private set; }
        public ILogger Logger { get; private set; }

        private List<Client> _hueClients = new List<Client>();

        public Plugin()
        {
            Instance = this;
        }

        public string Name { get; } = "IoTControlKit.PhilipsHue";

        public bool Initialize(IApplication app, IDatabase database, ILogger logger)
        {
            Application = app;
            Database = database;
            Logger = logger;

            Database.ExecuteWithinTransaction((db, session) =>
            {
                db.Execute(@"create table if not exists PhilipsHueClient(
Id integer PRIMARY KEY,
DeviceControllerId integer nutt null REFERENCES DeviceController (Id),
AppKey nvarchar(255) not null,
BridgeId nvarchar(255) not null,
TcpServer nvarchar(255) not null UNIQUE
)");
                var pl = db.Query<PhilipsHueClient>().ToList();
                foreach (var p in pl)
                {
                    var c = new Client(p);
                    _hueClients.Add(c);
                }

            });

            return true;
        }

        public void Start()
        {
            foreach (var c in _hueClients)
            {
                c.Start();
            }
        }

        public object EditController(NPoco.Database db, Framework.Models.DeviceController controller)
        {
            PhilipsHueClient result = null;
            if (controller != null)
            {
                result = db.Query<PhilipsHueClient>().Where(x => x.DeviceControllerId == controller.Id).FirstOrDefault();
            }
            else
            {
                result = new PhilipsHueClient()
                {
                    //DeviceControllerId = controller.Id,
                    AppKey = "",
                    TcpServer = ""
                };

                //search for hub
                var locator = new HttpBridgeLocator();
                var bridges = locator.LocateBridgesAsync(TimeSpan.FromSeconds(5)).Result;
                lock (_hueClients)
                {
                    foreach (var b in bridges)
                    {
                        if (!(from a in _hueClients where a.ClientSetting.BridgeId == b.BridgeId select a).Any())
                        {
                            result.TcpServer = b.IpAddress;
                            result.BridgeId = b.BridgeId;
                        }
                    }
                }
            }
            return result;
        }

        public void SaveController(NPoco.Database db, Framework.Models.DeviceController controller, ExpandoObject pluginData)
        {
            var props = (IDictionary<string, object>)pluginData;
            var m = new PhilipsHueClient()
            {
                Id = (long)props["Id"],
                DeviceControllerId = controller.Id,
                AppKey = (string)props["AppKey"],
                BridgeId = (string)props["BridgeId"],
                TcpServer = (string)props["TcpServer"]
            };

            //todo: check values

            if (string.IsNullOrEmpty(m.AppKey))
            {
                var client = new LocalHueClient(m.TcpServer);
                if (string.IsNullOrEmpty(m.BridgeId))
                {
                    m.BridgeId = "Manual";
                }
                m.AppKey = client.RegisterAsync("IoTControlKit", "PhilipsHue").Result;
            }

            db.Save(m);

            Task.Run(() =>
            {
                lock (_hueClients)
                {
                    var orgClient = (from a in _hueClients where a.ClientSetting.Id == m.Id select a).FirstOrDefault();
                    if (orgClient != null)
                    {
                        orgClient.Dispose();
                        _hueClients.Remove(orgClient);
                    }
                    var newClient = new Client(m);
                    _hueClients.Add(newClient);
                    newClient.Start();
                }
            });
        }
    }
}
