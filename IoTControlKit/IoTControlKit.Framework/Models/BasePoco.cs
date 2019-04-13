using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace IoTControlKit.Framework.Models
{
    [NPoco.PrimaryKey("Id")]
    public class BasePoco
    {
        public long Id { get; set; }

        public virtual bool CheckForNewName(NPoco.IDatabase db, string newName)
        {
            bool result = false;
            var IdProperty = this.GetType().GetTypeInfo().GetProperty("Id");
            var NameProperty = this.GetType().GetTypeInfo().GetProperty("Name");
            if (IdProperty != null && NameProperty != null)
            {
                result = !db.Fetch(this.GetType(), "where Id<>@0 and Name like @1", IdProperty.GetValue(this), newName).Any();
            }
            return result;
        }
    }

}
