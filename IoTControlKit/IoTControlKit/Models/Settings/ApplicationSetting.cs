using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTControlKit.Models.Settings
{
    [NPoco.TableName("ApplicationSetting")]
    [NPoco.PrimaryKey("Id")]
    public class ApplicationSetting : BasePoco
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
    }

}
