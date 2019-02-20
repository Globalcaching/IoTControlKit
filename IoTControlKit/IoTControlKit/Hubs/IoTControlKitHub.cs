using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTControlKit.Hubs
{
    public class IoTControlKitHub: Hub
    {
        private static IHubContext<IoTControlKitHub> _context;

        private static IHubContext<IoTControlKitHub> HubContext
        {
            get
            {
                if (_context == null)
                {
                    _context = Program.HubContext;
                }
                return _context;
            }
        }

        public static void DataChanged(string[] tables)
        {
            try
            {
                HubContext.Clients.All.SendAsync("DataChanged", tables);
            }
            catch
            {
            }
        }
    }
}
