using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTControlKit.Services
{
    public class ChangesFilter<T>
    {
        public List<T> Added { get; set; }
        public List<T> Updated { get; set; }
        public List<T> Deleted { get; set; }

        public ChangesFilter(DatabaseService.DatabaseChanges changes)
        {
            Added = (from a in changes.Added where a.GetType() == typeof(T) select (T)a).ToList();
            Updated = (from a in changes.Updated where a.GetType() == typeof(T) select (T)a).ToList();
            Deleted = (from a in changes.Deleted where a.GetType() == typeof(T) select (T)a).ToList();
        }
    }
}
