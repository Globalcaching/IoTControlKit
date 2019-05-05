using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IoTControlKit.Framework;

namespace IoTControlKit.Plugin.MQTT
{
    public class Plugin : IPlugin
    {
        public static Plugin Instance { get; private set; }

        public IApplication Application { get; private set; }
        public IDatabase Database { get; private set; }
        public ILogger Logger { get; private set; }

        private List<Client> _mqttClients = new List<Client>();

        public Plugin()
        {
            Instance = this;
        }

        public string Name { get; } = "IoTControlKit.MQTT";

        public bool Initialize(IApplication app, IDatabase database, ILogger logger)
        {
            Application = app;
            Database = database;
            Logger = logger;

            Database.ExecuteWithinTransaction((db, session) =>
            {
                db.Execute(@"create table if not exists MQTTClient(
Id integer PRIMARY KEY,
DeviceControllerId integer nutt null REFERENCES DeviceController (Id),
Name nvarchar(255) not null UNIQUE,
BaseTopic nvarchar(255) not null,
MQTTType nvarchar(255) not null,
TcpServer nvarchar(255) not null UNIQUE
)");

                var pl = db.Query<MQTTClient>().ToList();
                if (!pl.Any())
                {
                    //create default MQTT client
                    var controller = new Framework.Models.DeviceController()
                    {
                        Enabled = true,
                        Name = "MQTT Homie",
                        NormalizedName = "MQTT_Homie",
                        Plugin = Name,
                        Ready = false,
                        State = null
                    };
                    db.Save(controller);
                    var c = new MQTTClient()
                    {
                        DeviceControllerId = controller.Id,
                        BaseTopic = "Homey/homie/#",
                        MQTTType = "homie",
                        Name = "Homey/homie",
                        TcpServer = "localhost"
                    };
                    db.Save(c);
                }
                foreach (var p in pl)
                {
                    var c = MQTT.ClientFactory.CreateClient(p);
                    if (c != null)
                    {
                        _mqttClients.Add(c);
                    }
                }
            });

            return true;
        }

        public void Start()
        {
            foreach (var c in _mqttClients)
            {
                c.Start();
            }
        }

        public object EditController(NPoco.Database db, Framework.Models.DeviceController controller)
        {
            MQTTClient result = null;
            if (controller != null)
            {
                result = db.Query<MQTTClient>().Where(x => x.DeviceControllerId == controller.Id).FirstOrDefault();
            }
            else
            {
                result = new MQTTClient()
                {
                    //DeviceControllerId = controller.Id,
                    BaseTopic = "Homey/homie/#",
                    MQTTType = "homie",
                    Name = "Homey/homie",
                    TcpServer = "localhost"
                };
            }
            return result;
        }

        public void SaveController(NPoco.Database db, Framework.Models.DeviceController controller, ExpandoObject pluginData)
        {
            var props = (IDictionary<string, object>)pluginData;
            var m = new MQTTClient()
            {
                Id = (long)props["Id"],
                DeviceControllerId = controller.Id,
                BaseTopic = (string)props["BaseTopic"],
                MQTTType = (string)props["MQTTType"],
                Name = controller.Name,
                TcpServer = (string)props["TcpServer"]
            };
            //todo: check values

            db.Save(m);

            Task.Run(() =>
            {
                lock (_mqttClients)
                {
                    var orgClient = (from a in _mqttClients where a.ClientSetting.Id == m.Id select a).FirstOrDefault();
                    if (orgClient != null)
                    {
                        orgClient.Dispose();
                        _mqttClients.Remove(orgClient);
                    }
                    var newClient = ClientFactory.CreateClient(m);
                    _mqttClients.Add(newClient);
                    newClient.Start();
                }
            });
        }
    }
}
