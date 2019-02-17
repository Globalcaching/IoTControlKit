using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System;
using System.Linq;

public static class DynamicExtension
{
    /// <summary>
    /// Try to cast param value to an ExpandoObject. If this is not possible, create an new ExpandoObject will all properties of param value.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static dynamic ToDynamic(this object value)
    {
        IDictionary<string, object> result = null;
        if (value is ExpandoObject)
        {
            result = (ExpandoObject)value;
        }
        else if (value != null)
        {
            result = new ExpandoObject();
            foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(value.GetType()))
            {
                result.Add(property.Name, property.GetValue(value));
            }
        }
        return result as ExpandoObject;
    }

    public static dynamic AddDynamic(this object value, object other)
    {
        IDictionary<string, object> result = value.ToDynamic();
        if (other != null)
        {
            foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(other.GetType()))
            {
                if (!result.ContainsKey(property.Name))
                {
                    result.Add(property.Name, property.GetValue(other));
                }
            }
        }
        return result as ExpandoObject;
    }

    public static bool HasProperty(this object value, string name)
    {
        if (value != null)
        {
            IDictionary<string, object> dyn = value.ToDynamic();
            if (dyn.ContainsKey(name))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Created a List with ExpandoObject's from this Enumeration where
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static List<ExpandoObject> EnumToExpando<T>()
    {
        return Enum.GetValues(typeof(T)).Cast<T>().Select(v => new { Id = Convert.ToInt32(v), Name = v.ToString() }.ToExpando()).ToList();
    }
}


