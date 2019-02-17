using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NPoco;

namespace IoTControlKit.Data
{
    public class SQLiteDatabaseHelper : IDatabaseHelper
    {
        public Type ColumnDataType(Database db, string tableName, string columnName)
        {
            var result = typeof(DBNull);
            var l = db.Fetch<dynamic>(string.Format("pragma table_info({0})", tableName));
            if (l != null)
            {
                foreach (var o in l)
                {
                    if (string.Compare(o.name, columnName, false) == 0)
                    {
                        switch (o.type.ToLower().Split('(')[0])
                        {
                            case "nvarchar":
                                result = typeof(string);
                                break;
                            case "bit":
                                result = typeof(bool);
                                break;
                            case "int":
                            case "integer":
                                result = typeof(long);
                                break;
                            case "datetime":
                                result = typeof(DateTime);
                                break;
                            case "real":
                                result = typeof(float);
                                break;
                        }
                        break;
                    }
                }
            }
            return result;
        }

        public bool ColumnIsNullable(Database db, string tableName, string columnName)
        {
            var result = false;
            var l = db.Fetch<dynamic>(string.Format("pragma table_info({0})", tableName));
            if (l != null)
            {
                foreach (var o in l)
                {
                    if (string.Compare(o.name, columnName, false) == 0)
                    {
                        result = !o.notnull;
                        break;
                    }
                }
            }
            return result;
        }

        public bool ColumnExists(Database db, string tableName, string columnName)
        {
            bool result = false;
            var l = GetTableColumnNames(db, tableName);
            if (l != null)
            {
                foreach (var o in l)
                {
                    if (string.Compare(o, columnName, false) == 0)
                    {
                        result = true;
                        break;
                    }
                }
            }
            return result;
        }

        public void DeleteTable(Database db, string name)
        {
            db.Execute(string.Format("DROP TABLE IF EXISTS {0}", name));
        }

        public bool TableExists(Database db, string name)
        {
            bool result = false;
            var o = db.ExecuteScalar<string>("SELECT name FROM sqlite_master WHERE type='table' AND name=@0", name);
            result = o != null;
            return result;
        }

        public List<string> GetTableColumnNames(Database db, string name)
        {
            var result = new List<string>();
            using (var cmd = db.CreateCommand(db.Connection, System.Data.CommandType.Text, string.Format("pragma table_info({0})", name)))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    int nameIndex = reader.GetOrdinal("name");
                    while (reader.Read())
                    {
                        result.Add(reader.GetString(nameIndex));
                    }
                }
            }
            return result;
        }
    }
}
