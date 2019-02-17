using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace IoTControlKit.Services.Database
{
    public class LocalizationDatabaseService : BaseDatabaseService
    {
        private static LocalizationDatabaseService _uniqueInstance = null;
        private static object _lockObject = new object();

        private NPoco.Database _localizationDatabase;

        public event EventHandler DatabaseRestored;

        private LocalizationDatabaseService()
        {
            InitDatabase();
        }

        public static LocalizationDatabaseService Instance
        {
            get
            {
                if (_uniqueInstance == null)
                {
                    lock (_lockObject)
                    {
                        if (_uniqueInstance == null)
                        {
                            _uniqueInstance = new LocalizationDatabaseService();
                        }
                    }
                }
                return _uniqueInstance;
            }
        }

        private void InitDatabase()
        {
            _localizationDatabase = GetDatabase();
            var jm = _localizationDatabase.ExecuteScalar<string>("PRAGMA journal_mode");
            if (string.Compare(jm, "wal", true) != 0)
            {
                _localizationDatabase.Execute("PRAGMA journal_mode = WAL");
            }
            ExecuteWithinTransaction(_localizationDatabase, (db, session) =>
            {
                db.Execute(@"create table if not exists LocalizationCulture(
Id integer PRIMARY KEY,
Name text,
Description text
)");
                db.Execute(@"create table if not exists LocalizationOriginalText(
Id integer PRIMARY KEY,
OriginalText text
)");
                db.Execute(@"create table if not exists LocalizationTranslation(
Id integer PRIMARY KEY,
LocalizationCultureId integer not null,
LocalizationOriginalTextId integer not null,
TranslatedText text,
FOREIGN KEY(LocalizationCultureId) REFERENCES LocalizationCulture(Id),
FOREIGN KEY(LocalizationOriginalTextId) REFERENCES LocalizationOriginalText(Id)
)");
            });

            Execute((db) =>
            {
                db.Execute("VACUUM");
            });
        }

        public void Backup(string filename)
        {
            using (var con = new SqliteConnection(string.Format("data source={0}", filename)))
            {
                (_localizationDatabase.Connection as SqliteConnection).BackupDatabase(con);
            }
        }

        public void Restore(string filename)
        {
            Execute((db) =>
            {
                try
                {
                    db.Execute($"attach database '{filename}' as 'source'");

                    db.BeginTransaction(System.Data.IsolationLevel.Serializable);

                    try
                    {
                        db.Execute("delete from LocalizationTranslation");
                        db.Execute("delete from LocalizationOriginalText");
                        db.Execute("delete from LocalizationCulture;");

                        db.Execute("INSERT INTO main.LocalizationCulture(Id, Name, Description) SELECT Id, Name, Description FROM source.LocalizationCulture;");
                        db.Execute("INSERT INTO main.LocalizationOriginalText(Id, OriginalText) SELECT Id, OriginalText FROM source.LocalizationOriginalText;");
                        db.Execute("INSERT INTO main.LocalizationTranslation(Id, LocalizationCultureId, LocalizationOriginalTextId, TranslatedText) SELECT Id, LocalizationCultureId, LocalizationOriginalTextId, TranslatedText FROM source.LocalizationTranslation;");

                        db.CompleteTransaction();
                    }
                    catch
                    {
                        try
                        {
                            db.AbortTransaction();
                        }
                        catch
                        {
                        }
                    }
                    db.Execute($"detach database 'source'");
                }
                catch
                {
                }
            });
            DatabaseRestored?.Invoke(this, EventArgs.Empty);
        }


        protected override NPoco.Database GetDatabase()
        {
            var fn = Path.Combine(Helpers.EnvironmentHelper.RootDataFolder, "localization.db3");
            var con = new SqliteConnection(string.Format("data source={0}", fn));
            con.Open();
            return new LocalizationDatabase(this, con);
        }

        public override void Execute(Action<NPoco.Database> action)
        {
            Execute(_localizationDatabase, action);
        }

        public override T GetPage<T, T2>(int page, int pageSize, string sortOn, bool sortAsc, string defaultSort, NPoco.Sql sql)
        {
            T result;
            result = GetPage<T, T2>(_localizationDatabase, page, pageSize, sortOn, sortAsc, defaultSort, sql);
            return result;
        }

        public override bool ExecuteWithinTransaction(Action<NPoco.Database, Guid> action)
        {
            bool result = false;
            result = ExecuteWithinTransaction(_localizationDatabase, action);
            return result;
        }
    }

}
