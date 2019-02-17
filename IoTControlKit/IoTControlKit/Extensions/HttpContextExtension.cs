
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace System.Web
{
    /// <summary>
    /// Making System.Web.HttpContext.Current available again, for easy migration from .net 4.6 to .net core.
    /// This is not a advised design pattern. So it should be better to use HttpContext.Current as less as possible, and gradually remove it from the project.
    /// A better approach should be dependency injection.
    /// </summary>
    public static class HttpContext
    {
        private static IHttpContextAccessor _contextAccessor;

        public static Microsoft.AspNetCore.Http.HttpContext Current => _contextAccessor.HttpContext;

        internal static void Configure(IHttpContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
        }
    }
}

public static class HttpContextExtension
{
    public static void AddHttpContextAccessor(this IServiceCollection services)
    {
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
    }

    public static IApplicationBuilder UseStaticHttpContext(this IApplicationBuilder app)
    {
        var httpContextAccessor = app.ApplicationServices.GetRequiredService<IHttpContextAccessor>();
        System.Web.HttpContext.Configure(httpContextAccessor);
        return app;
    }

    public static string GetUserAgent(this Microsoft.AspNetCore.Http.HttpRequest request)
    {
        string result = null;
        result = request.GetHeaderValueAs<string>("User-Agent");
        if (string.IsNullOrEmpty(result))
        {
            result = request.GetHeaderValueAs<string>("User-agent");
        }
        if (string.IsNullOrEmpty(result))
        {
            result = request.GetHeaderValueAs<string>("USER-AGENT");
        }
        return result;
    }

    public static string GetIPAddress(this Microsoft.AspNetCore.Http.HttpRequest request, bool tryUseXForwardHeader)
    {
        string ip = null;

        // todo support new "Forwarded" header (2014) https://en.wikipedia.org/wiki/X-Forwarded-For

        // X-Forwarded-For (csv list):  Using the First entry in the list seems to work
        // for 99% of cases however it has been suggested that a better (although tedious)
        // approach might be to read each IP from right to left and use the first public IP.
        // http://stackoverflow.com/a/43554000/538763
        //
        if (tryUseXForwardHeader)
        {
            ip = request?.GetHeaderValueAs<string>("X-Forwarded-For").SplitCsv().FirstOrDefault();
        }

        // RemoteIpAddress is always null in DNX RC1 Update1 (bug).
        if (string.IsNullOrWhiteSpace(ip) && request?.HttpContext?.Connection?.RemoteIpAddress != null)
        {
            ip = request.HttpContext.Connection.RemoteIpAddress.ToString();
        }

        if (string.IsNullOrWhiteSpace(ip))
        {
            ip = request.GetHeaderValueAs<string>("REMOTE_ADDR");
        }

        // _httpContextAccessor.HttpContext?.Request?.Host this is the local host.

        if (string.IsNullOrWhiteSpace(ip))
        {
            //throw new Exception("Unable to determine caller's IP.");
        }

        return ip?.Replace("::ffff:", "");
    }

    public static T GetHeaderValueAs<T>(this Microsoft.AspNetCore.Http.HttpRequest request, string headerName)
    {
        StringValues values;

        if (request.HttpContext?.Request?.Headers?.TryGetValue(headerName, out values) ?? false)
        {
            string rawValues = values.ToString();   // writes out as Csv when there are multiple.

            if (!string.IsNullOrWhiteSpace(rawValues))
            {
                return (T)Convert.ChangeType(values.ToString(), typeof(T));
            }
        }
        return default(T);
    }

    public static List<string> SplitCsv(this string csvList, bool nullOrWhitespaceInputReturnsNull = false)
    {
        if (string.IsNullOrWhiteSpace(csvList))
        {
            return nullOrWhitespaceInputReturnsNull ? null : new List<string>();
        }

        return csvList
            .TrimEnd(',')
            .Split(',')
            .AsEnumerable<string>()
            .Select(s => s.Trim())
            .ToList();
    }

}



