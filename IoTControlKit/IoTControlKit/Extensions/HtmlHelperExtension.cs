using Microsoft.AspNetCore.Mvc.Rendering;
using IoTControlKit.Services;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Collections.Generic;
using IoTControlKit;
using System.Linq;

public static class HtmlHelperExtension
{
    public static string CurrentCulture(this IHtmlHelper html)
    {
        return LocalizationService.Instance.CurrentCultureInfo.Name;
    }

    public static string T(this IHtmlHelper html, string key)
    {
        var result = LocalizationService.Instance[key].Replace("\n", "<br />");
        return result;
    }

    public static string T(this HtmlHelper html, string key, params object[] args)
    {
        return html.Encode(string.Format(LocalizationService.Instance[key], args)).Replace("\n","<br />");
    }

    public static string PluginFile(this IHtmlHelper html, IoTControlKit.Framework.IPlugin plugin, params string[] segments)
    {
        string result = null;
        var l = new List<string>() { Program.HostingEnvironment.WebRootPath, "plugin", plugin.Name };
        if (segments != null && segments.Any())
        {
            l.AddRange(segments);
        }
        var p = System.IO.Path.Combine(l.ToArray());
        if (System.IO.File.Exists(p))
        {
            result = System.IO.File.ReadAllText(p);
        }
        else
        {
            p = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetAssembly(typeof(IoTControlKit.Services.ApplicationService)).Location).Replace(@"file:\", "").Replace(@"file:/", "");
            l = new List<string>() { p, "wwwroot", "plugin", plugin.Name };
            if (segments != null && segments.Any())
            {
                l.AddRange(segments);
            }
            p = System.IO.Path.Combine(l.ToArray());
            if (System.IO.File.Exists(p))
            {
                result = System.IO.File.ReadAllText(p);
            }
        }
        return result;
    }
}

