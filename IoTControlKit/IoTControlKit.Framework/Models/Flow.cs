using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTControlKit.Framework.Models
{
    [NPoco.TableName("Flow")]
    public class Flow: BasePoco
    {
        public string Name { get; set; }
        public bool Enabled { get; set; }
    }
}
