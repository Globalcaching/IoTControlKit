using System;
using System.Collections.Generic;
using System.Text;

namespace IoTControlKit.Framework
{
    public interface IPlugin
    {
        string Name { get; }
        bool Initialize(IApplication app, IDatabase database, ILogger logger);
        void Start();
    }
}
