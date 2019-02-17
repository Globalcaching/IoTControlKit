using Microsoft.AspNetCore.Mvc.Rendering;
using IoTControlKit.Services;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Collections.Generic;

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

    public static bool DEBUG(this IHtmlHelper html)
    {
        var value = false;
#if (DEBUG)
        value = true;
#endif
        return value;
    }
}

