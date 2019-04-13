using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTControlKit.Services
{
    public class PluginService : BaseService
    {
        private static PluginService _uniqueInstance = null;
        private static object _lockObject = new object();

        private PluginService()
        {
        }

        public static PluginService Instance
        {
            get
            {
                if (_uniqueInstance == null)
                {
                    lock (_lockObject)
                    {
                        if (_uniqueInstance == null)
                        {
                            _uniqueInstance = new PluginService();
                        }
                    }
                }
                return _uniqueInstance;
            }
        }
    }
}
