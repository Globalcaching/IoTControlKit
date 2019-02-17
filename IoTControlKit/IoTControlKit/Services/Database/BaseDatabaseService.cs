using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTControlKit.Services.Database
{
    public class BaseDatabaseService : BaseService, IDisposable
    {
        public enum RecordChange
        {
            Added,
            Updated,
            Deleted,
            Updating
        }

        public delegate void RecordChangedHandler(NPoco.Database db, object record, RecordChange action);
        public event RecordChangedHandler RecordChanged;

        public Guid LastSession { get; set; }
        public string ExceptionLastCommand { get; set; }
        public string ExceptionLastCommandException { get; set; }

        public virtual void OnRecordChanged(NPoco.Database db, object record, RecordChange action)
        {
            RecordChanged?.Invoke(db, record, action);
        }

        protected virtual NPoco.Database GetDatabase()
        {
            return null;
        }

        public virtual void ClearTable(NPoco.Database db, string tableName)
        {
        }

        public virtual void SetConstraintCheckForTable(NPoco.Database db, string tableName, bool enable)
        {
        }

        public virtual void Execute(Action<NPoco.Database> action)
        {
            using (var db = GetDatabase())
            {
                Execute(db, action);
            }
        }

        public virtual void Execute(Action<NPoco.Database, Guid> action)
        {
            using (var db = GetDatabase())
            {
                Execute(db, action);
            }
        }

        public virtual string EscapeTableName(string tableName)
        {
            return tableName;
        }

        public virtual string EscapeSqlIdentifier(string columnName)
        {
            return columnName;
        }


        public void Execute(NPoco.Database db, Action<NPoco.Database> action)
        {
            try
            {
                action(db);
            }
            catch (Exception e)
            {
                ExceptionLastCommand = db.LastCommand;
                ExceptionLastCommandException = e.Message;
                throw;
            }
        }

        public void Execute(NPoco.Database db, Action<NPoco.Database, Guid> action)
        {
            try
            {
                action(db, Guid.Empty);
            }
            catch (Exception e)
            {
                ExceptionLastCommand = db.LastCommand;
                ExceptionLastCommandException = e.Message;
                throw;
            }
        }


        public virtual bool ExecuteWithinTransaction(Action<NPoco.Database, Guid> action)
        {
            bool result = false;
            using (var db = GetDatabase())
            {
                result = ExecuteWithinTransaction(db, action);
            }
            return result;
        }

        protected bool ExecuteWithinTransaction(NPoco.Database db, Action<NPoco.Database, Guid> action)
        {
            bool result = false;
            db.BeginTransaction(System.Data.IsolationLevel.Serializable);
            try
            {
                var session = Guid.NewGuid();
                LastSession = session;
                action(db, session);
                db.CompleteTransaction();
                result = true;
            }
            catch (Exception e)
            {
                try
                {
                    ExceptionLastCommand = db.LastCommand;
                    ExceptionLastCommandException = e.Message;
                    db.AbortTransaction();
                }
                catch
                {
                }
            }
            return result;
        }

        public virtual T GetPage<T, T2>(int page, int pageSize, string sortOn, bool sortAsc, string defaultSort, NPoco.Sql sql)
            where T : new()
        {
            T result;
            using (var db = GetDatabase())
            {
                result = GetPage<T, T2>(db, page, pageSize, sortOn, sortAsc, defaultSort, sql);
            }
            return result;
        }

        protected T GetPage<T, T2>(NPoco.Database db, int page, int pageSize, string sortOn, bool sortAsc, string defaultSort, NPoco.Sql sql)
            where T : new()
        {
            var result = new T();
            if (!string.IsNullOrWhiteSpace(sortOn))
            {
                var sort = sortAsc ? "Asc" : "Desc";
                sql = sql.OrderBy($"{sortOn} {sort}");
            }
            else if (!string.IsNullOrWhiteSpace(defaultSort))
            {
                sql = sql.OrderBy($"{defaultSort} asc");
            }
            var items = db.Page<T2>(page, pageSize, sql);
            typeof(T).GetProperty("Items").SetValue(result, items.Items, null);
            typeof(T).GetProperty("CurrentPage").SetValue(result, items.CurrentPage, null);
            typeof(T).GetProperty("PageCount").SetValue(result, items.TotalPages, null);
            typeof(T).GetProperty("TotalCount").SetValue(result, items.TotalItems, null);
            return result;
        }

        public virtual void Dispose()
        {
        }
    }

}
