using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTControlKit.Models.Settings
{
    [NPoco.TableName("ApiToken")]
    [NPoco.PrimaryKey("Id")]
    public class ApiToken : BasePoco
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Token { get; set; }
        public bool Enabled { get; set; }
    }

}
