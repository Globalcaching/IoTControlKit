using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTControlKit.Helpers
{
    public class DateTimeModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }
            var stringValue = bindingContext.ValueProvider.GetValue(bindingContext.ModelName).FirstValue;
            if ((bindingContext.ModelType == typeof(DateTime) || bindingContext.ModelType == typeof(DateTime?)) && string.IsNullOrEmpty(stringValue))
            {
                bindingContext.Result = ModelBindingResult.Success(null);
                return Task.CompletedTask;
            }
            DateTime result;
            if (DateTime.TryParse(stringValue, out result))
            {
                bindingContext.Result = ModelBindingResult.Success(result.ToUniversalTime());
            }
            else
            {
                bindingContext.Result = ModelBindingResult.Failed();
            }
            return Task.CompletedTask;
        }

    }
}
