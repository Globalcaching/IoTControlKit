using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTControlKit.Helpers
{
    public class DateTimeModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            if (context.Metadata.UnderlyingOrModelType == typeof(DateTime) || context.Metadata.UnderlyingOrModelType == typeof(DateTime?))
            {
                return new DateTimeModelBinder();
            }
            return null;
        }
    }
}
