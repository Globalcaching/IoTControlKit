using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTControlKit.Models.Settings
{
    [NPoco.TableName("User")]
    [NPoco.PrimaryKey("Id")]
    public class User : BasePoco
    {
        public long Id { get; set; }
        public long RoleId { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string PasswordHash { get; set; }
        public bool Enabled { get; set; }
    }

}
