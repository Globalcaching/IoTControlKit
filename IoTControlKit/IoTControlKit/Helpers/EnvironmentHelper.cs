using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTControlKit.Helpers
{
    public class EnvironmentHelper
    {
        private static object _lockObject = new object();

        private static string _rootDataFolder = null;
        public static string RootDataFolder
        {
            get
            {
                if (_rootDataFolder == null)
                {
                    lock (_lockObject)
                    {
                        if (_rootDataFolder == null)
                        {
                            _rootDataFolder = Program.Configuration["Data:Path:RootDataFolder"] ?? @"c:\IoTControlKitData";
                        }
                    }
                }
                return _rootDataFolder;
            }
        }


    }
}
