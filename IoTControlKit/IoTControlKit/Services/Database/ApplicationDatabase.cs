using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace IoTControlKit.Services.Database
{
    public class ApplicationDatabase : NPoco.Database
    {
        private BaseDatabaseService _service;

        public ApplicationDatabase(BaseDatabaseService service, DbConnection connection, NPoco.DatabaseType dbType) : base(connection, dbType)
        {
            _service = service;
            //if (dbType == NPoco.DatabaseType.SQLite)
            //{
                KeepConnectionAlive = false;
            //}
        }

        protected override void OnExecutingCommand(DbCommand cmd)
        {
#if DEBUG
            //System.Diagnostics.Debug.WriteLine(cmd.CommandText);
#endif
            base.OnExecutingCommand(cmd);
        }

        public override object Insert<T>(string tableName, string primaryKeyName, bool autoIncrement, T poco)
        {
            var result = base.Insert<T>(tableName, primaryKeyName, autoIncrement, poco);
            _service.OnRecordChanged(this, poco, BaseDatabaseService.RecordChange.Added);
            return result;
        }

        public override int Delete(string tableName, string primaryKeyName, object poco, object primaryKeyValue)
        {
            var result = base.Delete(tableName, primaryKeyName, poco, primaryKeyValue);
            _service.OnRecordChanged(this, poco, BaseDatabaseService.RecordChange.Deleted);
            return result;
        }

        public override int Update(string tableName, string primaryKeyName, object poco, object primaryKeyValue, IEnumerable<string> columns)
        {
            _service.OnRecordChanged(this, poco, BaseDatabaseService.RecordChange.Updating);
            var result = base.Update(tableName, primaryKeyName, poco, primaryKeyValue, columns);
            _service.OnRecordChanged(this, poco, BaseDatabaseService.RecordChange.Updated);
            return result;
        }
    }
}
