using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTControlKit.Services
{
    public partial class DatabaseService : BaseService, IDisposable
    {
        public class DatabaseChanges
        {
            public HashSet<string> AffectedTables { get; set; } = new HashSet<string>();
            public Guid Session { get; set; }
            public object Caller { get; set; }
            public List<object> Added { get; set; } = new List<object>();
            public List<object> Updated { get; set; } = new List<object>();
            public List<object> Deleted { get; set; } = new List<object>();
        }

        private DatabaseChanges _databaseChanges = null;
        private Services.Database.BaseDatabaseService _dbInstance;
        private List<Action<bool>> _executionAfterTransaction = new List<Action<bool>>();
        private Database.UndoRedoHandler _undoRedoHandler = new Database.UndoRedoHandler();
        private bool _logTransactionForUndoRedo = false;
        private HashSet<Type> _excludedForUndo = new HashSet<Type>() {
        };

        public delegate void DatabaseChangedHandler(DatabaseChanges changes);
        public event DatabaseChangedHandler DatabaseChanged;

        public bool SuppressAllUndoExcludes { get; set; } = false;

        public bool TransactionInProgress { get; private set; }
        public bool CanUndo
        {
            get
            {
                var result = false;
                lock(this)
                {
                    result = _undoRedoHandler.CanUndo;
                }
                return result;
            }
        }

        public bool CanRedo
        {
            get
            {
                var result = false;
                lock (this)
                {
                    result = _undoRedoHandler.CanRedo;
                }
                return result;
            }
        }

        public bool UndoRedoEnabled
        {
            get
            {
                return _logTransactionForUndoRedo;
            }
            set
            {
                //prevent changing during transaction
                lock (this)
                {
                    if (_logTransactionForUndoRedo != value)
                    {
                        SuppressAllUndoExcludes = false;
                        _logTransactionForUndoRedo = value;
                        _undoRedoHandler = new Database.UndoRedoHandler();
                        //Hubs.IoTControlKitHub.DatabaseUndoRedoStatus(_logTransactionForUndoRedo, CanUndo, CanRedo);
                    }
                }
            }
        }

        public DatabaseService()
        {
            _dbInstance = GetNewDatabaseInstance();
            _dbInstance.RecordChanged += _dbInstance_RecordChanged;
        }

        public void Dispose()
        {
            lock(this)
            {
                if (_dbInstance != null)
                {
                    _dbInstance.Dispose();
                    _dbInstance = null;
                }
            }
        }


        public string EscapeTableName(string tableName)
        {
            return _dbInstance?.EscapeTableName(tableName) ?? tableName;
        }

        public string EscapeSqlIdentifier(string columnName)
        {
            return _dbInstance?.EscapeSqlIdentifier(columnName) ?? columnName;
        }

        public void OnDatabaseChanged(DatabaseChanges changes)
        {
            var c = changes;
            if (DatabaseChanged != null)
            {
                foreach (var evh in DatabaseChanged.GetInvocationList())
                {
                    try
                    {
                        evh.DynamicInvoke(this, changes);
                    }
                    catch
                    {
                    }
                }
            }
        }

        private void _dbInstance_RecordChanged(NPoco.Database db, object record, Services.Database.BaseDatabaseService.RecordChange action)
        {
            if (_databaseChanges != null)
            {
                var pd = db.PocoDataFactory.ForType(record.GetType());
                var tn = pd.TableInfo.TableName;
                if (!_databaseChanges.AffectedTables.Contains(tn))
                {
                    _databaseChanges.AffectedTables.Add(tn);
                }
                switch (action)
                {
                    case Services.Database.BaseDatabaseService.RecordChange.Added:
                        _databaseChanges.Added.Add(record);
                        if (_logTransactionForUndoRedo && (SuppressAllUndoExcludes || !_excludedForUndo.Contains(record.GetType())))
                        {
                            _undoRedoHandler.AddPoco(record);
                        }
                        break;
                    case Services.Database.BaseDatabaseService.RecordChange.Deleted:
                        _databaseChanges.Deleted.Add(record);
                        if (_logTransactionForUndoRedo && (SuppressAllUndoExcludes || !_excludedForUndo.Contains(record.GetType())))
                        {
                            _undoRedoHandler.DeletePoco(record);
                        }
                        break;
                    case Services.Database.BaseDatabaseService.RecordChange.Updating:
                        if (_logTransactionForUndoRedo && (SuppressAllUndoExcludes || !_excludedForUndo.Contains(record.GetType())))
                        {
                            //get original poco
                            var orgPoco = db.Fetch(record.GetType(), $"select * from {pd.TableInfo.TableName} where Id=@0", ((Models.Application.BasePoco)record).Id).First();
                            _undoRedoHandler.UpdatePoco(orgPoco, record);
                        }
                        break;
                    case Services.Database.BaseDatabaseService.RecordChange.Updated:
                        _databaseChanges.Updated.Add(record);
                        break;
                }
            }
        }

        public T GetPage<T, T2>(int page, int pageSize, string sortOn, bool sortAsc, string defaultSort, NPoco.Sql sql) where T : new()
        {
            T result = default(T);
            lock (this)
            {
                result = _dbInstance.GetPage<T, T2>(page, pageSize, sortOn, sortAsc, defaultSort, sql);
            }
            return result;
        }

        public void Execute(Action<NPoco.Database> action)
        {
            lock (this)
            {
                try
                {
                    _dbInstance?.Execute(action);
                }
                catch
                {
                    LoggerService.Instance.LogError($"SQL ERROR: {_dbInstance.ExceptionLastCommandException}");
                    LoggerService.Instance.LogError($"AT SQL COMMAND: {_dbInstance.ExceptionLastCommand}");
                    throw;
                }
            }
        }

        public bool Undo()
        {
            var result = false;
            if (CanUndo)
            {
                result = ExecuteWithinTransaction((db, session) =>
                {
                    _undoRedoHandler.Undo(db);
                }, isUndoRedoAction: true);
            }
            return result;
        }

        public bool Redo()
        {
            var result = false;
            if (CanRedo)
            {
                result = ExecuteWithinTransaction((db, session) =>
                {
                    _undoRedoHandler.Redo(db);
                }, isUndoRedoAction: true);
            }
            return result;
        }

        private bool _isOnDatabaseChangedInProgress = false;
        private object _caller = null;
        public bool ExecuteWithinTransaction(Action<NPoco.Database, Guid> action, object caller = null, Action<bool> executeAfterTransaction = null, bool isUndoRedoAction = false)
        {
            var result = false;
            lock(this)
            {
                if (_isOnDatabaseChangedInProgress)
                {
                    //Not allowed, because this would create change behaviours
                    throw new Exception("ExecuteWithinTransaction: executing a transaction within database changed event not allowed!");
                }
                if (TransactionInProgress)
                {
                    if (_caller != caller)
                    {
                        throw new Exception("ExecuteWithinTransaction: transaction of other caller is in progress!");
                    }
                    else
                    {
                        if (executeAfterTransaction != null)
                        {
                            _executionAfterTransaction.Add(executeAfterTransaction);
                        }

                        _dbInstance?.Execute(action);
                        result = true;
                        return result;
                    }
                }
                else
                {
                    if (executeAfterTransaction != null)
                    {
                        _executionAfterTransaction.Add(executeAfterTransaction);
                    }

                    _caller = caller;
                }
                TransactionInProgress = true;
                try
                {
                    var undoState = _undoRedoHandler.CanUndo;
                    var redoState = _undoRedoHandler.CanRedo;
                    bool undoRedoActivated = (_logTransactionForUndoRedo && !isUndoRedoAction);
                    if (undoRedoActivated)
                    {
                        _undoRedoHandler.StartTransaction();
                    }
                    _databaseChanges = new DatabaseChanges();
                    result = _dbInstance?.ExecuteWithinTransaction(action) ?? false;
                    if (!result && _dbInstance != null)
                    {
                        LoggerService.Instance.LogError($"SQL ERROR: {_dbInstance.ExceptionLastCommandException}");
                        LoggerService.Instance.LogError($"AT SQL COMMAND: {_dbInstance.ExceptionLastCommand}");
                    }
                    else
                    {
                        if (undoRedoActivated)
                        {
                            _undoRedoHandler.EndTransaction();
                        }
                        if (_logTransactionForUndoRedo)
                        {
                            if (_undoRedoHandler.CanUndo != undoState || _undoRedoHandler.CanRedo != redoState)
                            {
                                //Hubs.IoTControlKitHub.DatabaseUndoRedoStatus(true, _undoRedoHandler.CanUndo, _undoRedoHandler.CanRedo);
                            }
                        }
                    }
                    _databaseChanges.Session = _dbInstance?.LastSession ?? Guid.NewGuid();
                    _databaseChanges.Caller = caller;
                    foreach (var afterAction in _executionAfterTransaction)
                    {
                        try
                        {
                            afterAction(result);
                        }
                        catch
                        {
                        }
                    }
                    _executionAfterTransaction.Clear();
                    if (_databaseChanges.AffectedTables.Any())
                    {
                        LoggerService.Instance.LogInformation($"ExecuteWithinTransaction: Added {_databaseChanges.Added.Count}, Deleted {_databaseChanges.Deleted.Count}, Updated {_databaseChanges.Updated.Count} objects in tables {string.Join(",", _databaseChanges.AffectedTables)} ");
                        _isOnDatabaseChangedInProgress = true;
                        OnDatabaseChanged(_databaseChanges);
                        _isOnDatabaseChangedInProgress = false;
                    }
                }
                finally
                {
                    _caller = null;
                    TransactionInProgress = false;
                }
            }
            return result;
        }
    }
}
