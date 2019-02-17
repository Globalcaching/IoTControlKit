using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTControlKit.Models.Localization
{
    [NPoco.TableName("LocalizationOriginalText")]
    [NPoco.PrimaryKey("Id")]
    public class LocalizationOriginalText
    {
        public long Id { get; set; }
        public string OriginalText { get; set; }
    }

}
