using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTControlKit.Services
{
    public partial class ApplicationService : BaseService
    {
        public class SetDeviceProperties
        {
            public long DevicePropertyId { get; set; }
            public string Value { get; set; }
            public bool InternalOnly { get; set; } = false;
        }

        private static ApplicationService _uniqueInstance = null;
        private static object _lockObject = new object();
        private List<MQTT.Client> _mqttClients = new List<MQTT.Client>();

        public DatabaseService Database { get; private set; }
        public Services.Schedulers.SchedulerService Schedular { get; private set; }

        public delegate void SetDevicePropertyValueHandler(NPoco.Database db, List<SetDeviceProperties> properties);
        public event SetDevicePropertyValueHandler SetDevicePropertyValue;

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

        public void OnSetDevicePropertyValue(List<SetDeviceProperties> properties)
        {
            if (SetDevicePropertyValue != null)
            {
                ApplicationService.Instance.Database.ExecuteWithinTransaction((db, session) =>
                {
                    foreach (var evh in SetDevicePropertyValue.GetInvocationList())
                    {
                        try
                        {
                            evh.DynamicInvoke(db, properties);
                        }
                        catch
                        {
                        }
                    }
                });
            }
        }

        public void Run()
        {
            Database = new DatabaseService();
            Database.ExecuteWithinTransaction((db, session) =>
            {
                var dcl = db.Fetch<Models.Application.DeviceController>();
                foreach (var dc in dcl)
                {
                    dc.Ready = false;
                    db.Save(dc);
                }

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

            Schedular = new Schedulers.SchedulerService();

            foreach (var c in _mqttClients)
            {
                c.Start();
            }
            Schedular.Start();

            Database.DatabaseChanged += Database_DatabaseChanged;
        }

        private void Database_DatabaseChanged(DatabaseService.DatabaseChanges changes)
        {
            if (changes.Caller != this)
            {
                if (changes.AffectedTables.Contains("MQTTClient"))
                {
                    var cf = new ChangesFilter<Models.Application.MQTTClient>(changes);
                    foreach (var p in cf.Updated)
                    {
                        var m = (from a in _mqttClients where a.ClientSetting.Id == p.Id select a).FirstOrDefault();
                        if (m != null)
                        {
                            _mqttClients.Remove(m);
                            m.Dispose();
                        }
                        if (p.Enabled)
                        {
                            var c = MQTT.ClientFactory.CreateClient(p);
                            if (c != null)
                            {
                                _mqttClients.Add(c);
                                c.Start();
                            }
                        }
                    }
                    foreach (var p in cf.Deleted)
                    {
                        var m = (from a in _mqttClients where a.ClientSetting.Id == p.Id select a).FirstOrDefault();
                        if (m != null)
                        {
                            _mqttClients.Remove(m);
                            m.Dispose();
                        }
                    }
                    foreach (var p in cf.Added)
                    {
                        if (p.Enabled)
                        {
                            var c = MQTT.ClientFactory.CreateClient(p);
                            if (c != null)
                            {
                                _mqttClients.Add(c);
                                c.Start();
                            }
                        }
                    }
                }
            }
        }
    }
}
