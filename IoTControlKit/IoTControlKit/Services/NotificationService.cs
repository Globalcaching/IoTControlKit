using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IoTControlKit.Services
{
    public class NotificationService : BaseService
    {
        private static NotificationService _uniqueInstance = null;
        private static object _lockObject = new object();

        private NotificationService()
        {
        }

        public static NotificationService Instance
        {
            get
            {
                if (_uniqueInstance == null)
                {
                    lock (_lockObject)
                    {
                        if (_uniqueInstance == null)
                        {
                            _uniqueInstance = new NotificationService();
                        }
                    }
                }
                return _uniqueInstance;
            }
        }

        public class Messages
        {
            public Messages()
            {
                ErrorMessages = new List<string>();
                WarningMessages = new List<string>();
                InfoMessages = new List<string>();
                SuccessMessages = new List<string>();
            }

            public List<string> ErrorMessages { get; set; }
            public List<string> WarningMessages { get; set; }
            public List<string> InfoMessages { get; set; }
            public List<string> SuccessMessages { get; set; }
        }


        private Messages GetMessages(HttpContext context, bool createIfNotExists, bool deleteIfExists)
        {
            var result = context.Session.GetObjectFromJson<Messages>("NotificationService.Messages");
            if (result == null && createIfNotExists)
            {
                result = new Messages();
                context.Session.SetObjectAsJson("NotificationService.Messages", result);

            }
            else if (result != null && deleteIfExists)
            {
                context.Session.Remove("NotificationService.Messages");
            }
            return result;
        }

        private void setMessages(HttpContext context, Messages messages)
        {
            context.Session.SetObjectAsJson("NotificationService.Messages", messages);
        }

        public Messages GetMessages(HttpContext context)
        {
            return GetMessages(context, false, true);
        }

        public void AddInfoMessage(HttpContext context, string message)
        {
            var messages = GetMessages(context, true, false);
            messages.InfoMessages.Add(message);
            setMessages(context, messages);
            SaveMessage("info", message, context);
        }

        public void AddInfoMessage(string message)
        {
            AddInfoMessage(System.Web.HttpContext.Current, message);
        }

        public void AddWarningMessage(HttpContext context, string message)
        {
            var messages = GetMessages(context, true, false);
            messages.WarningMessages.Add(message);
            setMessages(context, messages);
            SaveMessage("warning", message, context);
        }

        public void AddWarningMessage(string message)
        {
            AddWarningMessage(System.Web.HttpContext.Current, message);
        }

        public void AddSuccessMessage(HttpContext context, string message)
        {
            var messages = GetMessages(context, true, false);
            messages.SuccessMessages.Add(message);
            setMessages(context, messages);
            SaveMessage("success", message, context);
        }

        public void AddSuccessMessage(string message)
        {
            AddSuccessMessage(System.Web.HttpContext.Current, message);
        }

        public void AddErrorMessage(HttpContext context, string message)
        {
            var messages = GetMessages(context, true, false);
            messages.ErrorMessages.Add(message);
            setMessages(context, messages);
            SaveMessage("error", message, context);
        }

        public void AddErrorMessage(string message)
        {
            AddErrorMessage(System.Web.HttpContext.Current, message);
        }

        public void SaveMessage(string type, string message, HttpContext context)
        {
            var ipAddress = context.Features.Get<IHttpConnectionFeature>()?.RemoteIpAddress?.ToString();
            var currentUser = context.User?.Identity?.Name ?? string.Empty;
            var actionPath = context.Request.Path.Value;
        }
    }

}
