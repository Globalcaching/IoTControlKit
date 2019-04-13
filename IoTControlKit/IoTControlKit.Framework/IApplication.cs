using System;
using System.Collections.Generic;
using System.Text;

namespace IoTControlKit.Framework
{
    public interface IApplication
    {
        event SetDeviceProperties.SetDevicePropertyValueHandler SetDevicePropertyValue;
        string RootDataFolder { get; }
    }
}
