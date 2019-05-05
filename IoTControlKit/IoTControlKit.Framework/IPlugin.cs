using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;

namespace IoTControlKit.Framework
{
    public interface IPlugin
    {
        string Name { get; }
        bool Initialize(IApplication app, IDatabase database, ILogger logger);
        void Start();
        object EditController(NPoco.Database db, Framework.Models.DeviceController controller);
        void SaveController(NPoco.Database db, Framework.Models.DeviceController controller, ExpandoObject pluginData);
    }
}
