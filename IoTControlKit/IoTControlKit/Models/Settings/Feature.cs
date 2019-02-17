using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTControlKit.Models.Settings
{
    [NPoco.TableName("Feature")]
    [NPoco.PrimaryKey("Id")]
    public class Feature : BasePoco
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

}
