using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTControlKit.Services.Database
{
    public class ApplicationSQLiteDatabaseInstance : BaseDatabaseService
    {
        private NPoco.Database _ctxDatabase;
        private string _fileName;

        public ApplicationSQLiteDatabaseInstance(string filename)
        {
            _fileName = filename;
            InitDatabase();
        }

        public void Backup(ApplicationSQLiteDatabaseInstance target)
        {
            (_ctxDatabase.Connection as SqliteConnection).BackupDatabase(target._ctxDatabase.Connection as SqliteConnection);
        }

        public override string EscapeTableName(string tableName)
        {
            return NPoco.DatabaseType.SQLite.EscapeTableName(tableName);
        }

        public override string EscapeSqlIdentifier(string columnName)
        {
            return NPoco.DatabaseType.SQLite.EscapeSqlIdentifier(columnName);
        }

        private void InitDatabase()
        {
            _ctxDatabase = GetDatabase();
        }

        public override void OnRecordChanged(NPoco.Database db, object record, RecordChange action)
        {
            base.OnRecordChanged(db, record, action);
        }

        protected override NPoco.Database GetDatabase()
        {
            var con = new SqliteConnection(string.Format("data source={0}", _fileName));
            con.Open();
            return new ApplicationDatabase(this, con, NPoco.DatabaseType.SQLite);
        }

        public override void Execute(Action<NPoco.Database> action)
        {
            Execute(_ctxDatabase, action);
        }

        public override void Execute(Action<NPoco.Database, Guid> action)
        {
            Execute(_ctxDatabase, action);
        }

        public override void ClearTable(NPoco.Database db, string tableName)
        {
            db.Execute($"delete from {EscapeTableName(tableName)}");
        }

        public override void SetConstraintCheckForTable(NPoco.Database db, string tableName, bool enable)
        {
            if (enable)
            {
                db.Execute("PRAGMA foreign_keys = ON");
            }
            else
            {
                db.Execute("PRAGMA foreign_keys = OFF");
            }
        }


        public override T GetPage<T, T2>(int page, int pageSize, string sortOn, bool sortAsc, string defaultSort, NPoco.Sql sql)
        {
            T result;
            result = GetPage<T, T2>(_ctxDatabase, page, pageSize, sortOn, sortAsc, defaultSort, sql);
            return result;
        }

        public override bool ExecuteWithinTransaction(Action<NPoco.Database, Guid> action)
        {
            bool result = false;
            result = ExecuteWithinTransaction(_ctxDatabase, action);
            return result;
        }

        public override void Dispose()
        {
            if (_ctxDatabase != null)
            {
                _ctxDatabase.Dispose();
                _ctxDatabase = null;
            }
            base.Dispose();
        }
    }
}
