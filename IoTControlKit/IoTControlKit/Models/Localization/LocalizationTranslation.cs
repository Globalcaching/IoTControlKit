using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTControlKit.Models.Localization
{
    [NPoco.TableName("LocalizationTranslation")]
    [NPoco.PrimaryKey("Id")]
    public class LocalizationTranslation
    {
        public long Id { get; set; }
        public long LocalizationCultureId { get; set; }
        public long LocalizationOriginalTextId { get; set; }
        public string TranslatedText { get; set; }
    }

}
