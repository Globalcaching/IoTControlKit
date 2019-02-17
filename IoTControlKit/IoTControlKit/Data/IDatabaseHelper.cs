using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTControlKit.Data
{
    public interface IDatabaseHelper
    {
        bool TableExists(NPoco.Database db, string name);
        bool ColumnExists(NPoco.Database db, string tableName, string columnName);
        void DeleteTable(NPoco.Database db, string name);
        Type ColumnDataType(NPoco.Database db, string tableName, string columnName);
        bool ColumnIsNullable(NPoco.Database db, string tableName, string columnName);
        List<string> GetTableColumnNames(NPoco.Database db, string name);
    }
}
