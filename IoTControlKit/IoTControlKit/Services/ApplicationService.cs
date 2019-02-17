using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTControlKit.Services
{
    public class ApplicationService : BaseService
    {
        private static ApplicationService _uniqueInstance = null;
        private static object _lockObject = new object();
        private List<MQTT.Client> _mqttClients = new List<MQTT.Client>();

        public DatabaseService Database { get; private set; }

        private ApplicationService()
        {
        }

        public static ApplicationService Instance
        {
            get
            {
                if (_uniqueInstance == null)
                {
                    lock (_lockObject)
                    {
                        if (_uniqueInstance == null)
                        {
                            _uniqueInstance = new ApplicationService();
                        }
                    }
                }
                return _uniqueInstance;
            }
        }

        public void Run()
        {
            Database = new DatabaseService();
            Database.Execute((db) =>
            {
                var pl = db.Query<Models.Application.MQTTClient>().Where(x => x.Enabled).ToList();
                foreach (var p in pl)
                {
                    var c = MQTT.ClientFactory.CreateClient(p);
                    if (c != null)
                    {
                        _mqttClients.Add(c);
                    }
                }
            });

            foreach (var c in _mqttClients)
            {
                c.Start();
            }

            Database.DatabaseChanged += Database_DatabaseChanged;
        }

        private void Database_DatabaseChanged(DatabaseService.DatabaseChanges changes)
        {
            if (changes.Caller != this)
            {
                if (changes.AffectedTables.Contains("MQTTClient"))
                {
                    //todo
                }
            }
        }
    }
}
