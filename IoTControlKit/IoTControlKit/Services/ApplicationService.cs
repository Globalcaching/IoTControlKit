using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using IoTControlKit.Framework;
using NPoco;

namespace IoTControlKit.Services
{
    public partial class ApplicationService : BaseService, Framework.IApplication, Framework.IDatabase
    {
        private static ApplicationService _uniqueInstance = null;
        private static object _lockObject = new object();
        private List<Framework.IPlugin> _plugins = new List<Framework.IPlugin>();
        private Dictionary<string, Framework.IPlugin> _plugInByName = new Dictionary<string, IPlugin>();

        public DatabaseService Database { get; private set; }
        public Services.Schedulers.SchedulerService Schedular { get; private set; }

        public event Framework.SetDeviceProperties.SetDevicePropertyValueHandler SetDevicePropertyValue;

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

        public string RootDataFolder => throw new NotImplementedException();
        public List<Framework.IPlugin> Plugins => _plugins;

        public void OnSetDevicePropertyValue(List<SetDeviceProperties> properties)
        {
            if (SetDevicePropertyValue != null)
            {
                ApplicationService.Instance.Database.ExecuteWithinTransaction((db, session) =>
                {
                    SetDevicePropertyValue?.Invoke(db, properties);
                });
            }
        }

        public void Run()
        {
            Database = new DatabaseService();

            //for now...
            _plugins.Add(new IoTControlKit.Plugin.MQTT.Plugin());
            _plugins.Add(new IoTControlKit.Plugin.PhilipsHue.Plugin());

            foreach (var plugin in _plugins)
            {
                _plugInByName.TryAdd(plugin.Name, plugin);
                plugin.Initialize(this, this, LoggerService.Instance);
            }

            Schedular = new Schedulers.SchedulerService();

            foreach (var plugin in _plugins)
            {
                plugin.Start();
            }

            Schedular.Start();

            Database.DatabaseChanged += Database_DatabaseChanged;
        }

        private void Database_DatabaseChanged(DatabaseService.DatabaseChanges changes)
        {
            if (changes.Caller != this)
            {
                //nope at the moment
            }
        }

        public bool ExecuteWithinTransaction(Action<NPoco.Database, Guid> action, object caller = null, Action<bool> executeAfterTransaction = null, bool isUndoRedoAction = false)
        {
            return Database.ExecuteWithinTransaction(action, caller, executeAfterTransaction, isUndoRedoAction);
        }

        public void Execute(Action<NPoco.Database> action)
        {
            Database.Execute(action);
        }

        public object EditController(string pluginName, long? id)
        {
            object result = null;
            if (_plugInByName.TryGetValue(pluginName, out var plugin))
            {
                Database.Execute((db) =>
                {
                    Framework.Models.DeviceController dc = null;
                    if (id != null)
                    {
                        dc = db.Query<Framework.Models.DeviceController>().Where(x => x.Id == id).FirstOrDefault();
                    }
                    result = new
                    {
                        Controller = dc,
                        Plugin = plugin.EditController(db, dc)
                    };
                });
            }
            return result;
        }

        public void SaveController(string pluginName, Framework.Models.DeviceController controller, ExpandoObject pluginData)
        {
            if (_plugInByName.TryGetValue(pluginName, out var plugin))
            {
                Database.ExecuteWithinTransaction((db, session) =>
                {
                    controller.NormalizedName = controller.Name.Replace(" ", "_"); //for now
                    db.Save(controller);
                    plugin.SaveController(db, controller, pluginData);
                });
            }
        }
    }
}
