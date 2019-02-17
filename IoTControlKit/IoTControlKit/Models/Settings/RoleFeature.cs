using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTControlKit.Models.Settings
{
    [NPoco.TableName("RoleFeature")]
    [NPoco.PrimaryKey("Id")]
    public class RoleFeature : BasePoco
    {
        public long Id { get; set; }
        public long RoleId { get; set; }
        public long FeatureId { get; set; }
    }

}
