using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace IoTControlKit.Helpers
{
    public static class ControllerHelper
    {
        public static string GetFilterValue(List<string> filterColumns, List<string> filterValues, string name)
        {
            string result = null;
            if (filterColumns != null && filterValues!=null && filterValues.Count == filterColumns.Count)
            {
                var pos = filterColumns.IndexOf(name);
                if (pos >= 0)
                {
                    result = filterValues[pos];
                }
            }
            return result;
        }

        public static string GetSortField(Dictionary<string, string> map, string field)
        {
            string result = null;
            if (map != null && field!=null)
            {
                result = map.FirstOrDefault(x => string.Equals(x.Key,
                         field,
                         StringComparison.OrdinalIgnoreCase)).Value;
            }
            return result;
        }

        public static ExpandoObject ToExpando(this object anonymousObject)
        {
            IDictionary<string, object> anonymousDictionary = new RouteValueDictionary(anonymousObject);
            IDictionary<string, object> expando = new ExpandoObject();
            foreach (var item in anonymousDictionary)
                expando.Add(item);
            return (ExpandoObject)expando;
        }
    }
}