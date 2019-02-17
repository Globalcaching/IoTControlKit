using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace IoTControlKit.Services.Database
{
    public class SettingsDatabaseService : BaseDatabaseService
    {
        private static SettingsDatabaseService _uniqueInstance = null;
        private static object _lockObject = new object();

        private NPoco.Database _settingsDatabase;

        private const long _targetDatabaseVersion = 1;

        private SettingsDatabaseService()
        {
            InitDatabase();
        }

        public static SettingsDatabaseService Instance
        {
            get
            {
                if (_uniqueInstance == null)
                {
                    lock (_lockObject)
                    {
                        if (_uniqueInstance == null)
                        {
                            _uniqueInstance = new SettingsDatabaseService();
                        }
                    }
                }
                return _uniqueInstance;
            }
        }

        private void InitDatabase()
        {
            _settingsDatabase = GetDatabase();
            var jm = _settingsDatabase.ExecuteScalar<string>("PRAGMA journal_mode");
            if (string.Compare(jm, "wal", true) != 0)
            {
                _settingsDatabase.Execute("PRAGMA journal_mode = WAL");
            }
            ExecuteWithinTransaction(_settingsDatabase, (db, session) =>
            {
                var version = db.ExecuteScalar<long>("PRAGMA user_version");
                UpgradeDatabase(db, version);
                InitDatabaseContent(db);

            });
            Execute((db) =>
            {
                db.Execute("VACUUM");
            });
        }

        private bool UpgradeDatabase(NPoco.Database db, long fromVersion)
        {
            if (fromVersion > _targetDatabaseVersion)
            {
                //a version that we have no knowlegde of....the future!
                return false;
            }
            else if (fromVersion == _targetDatabaseVersion)
            {
                //database up to date for current version
                return true;
            }
            else
            {
                long newVersion = 0;
                do
                {
                    switch (fromVersion)
                    {
                        case 0: //initial clean version

                            db.Execute(@"create table if not exists ApplicationSetting(
Id integer PRIMARY KEY,
Name nvarchar(255) not null,
Value ntext
)");
                            db.Execute(@"create table if not exists Feature(
Id integer PRIMARY KEY,
Name nvarchar(255) not null UNIQUE,
Description ntext
)");

                            db.Execute(@"create table if not exists Role(
Id integer PRIMARY KEY,
Name nvarchar(255) not null UNIQUE,
Description ntext
)");

                            db.Execute(@"create table if not exists RoleFeature(
Id integer PRIMARY KEY,
RoleId integer not null,
FeatureId integer not null,
FOREIGN KEY(RoleId) REFERENCES Role(Id),
FOREIGN KEY(FeatureId) REFERENCES Feature(Id)
)");


                            db.Execute(@"create table if not exists User(
Id integer PRIMARY KEY,
Name nvarchar(255) not null UNIQUE,
DisplayName text,
RoleId integer not null,
PasswordHash nvarchar(255),
Enabled bit not null,
FOREIGN KEY(RoleId) REFERENCES Role(Id)
)");

                            db.Execute(@"create table if not exists ApiToken(
Id integer PRIMARY KEY,
Name nvarchar(255) not null UNIQUE,
Token nvarchar(255),
Enabled bit not null
)");
                            newVersion = 1;
                            break;
                    }
                    if (fromVersion != newVersion)
                    {
                        db.Execute(string.Format("PRAGMA user_version = {0}", newVersion));
                    }
                    fromVersion = newVersion;
                }
                while (newVersion != _targetDatabaseVersion);
                return true;
            }
        }

        private void InitDatabaseContent(NPoco.Database db)
        {
            foreach (var n in (Services.AuthorizationService.Feature[])Enum.GetValues(typeof(Services.AuthorizationService.Feature)))
            {
                db.Execute($"insert or ignore into Feature (Name, Description) values ('{n.ToString()}', '{Services.AuthorizationService.FeatureDescription[n]}')");
            }
            db.Execute("insert or ignore into Role (Name, Description) values ('Admin', 'Administrator')");
            db.Execute("insert or ignore into Role (Name, Description) values ('Guest', 'Guest')");

            //make sure the Admin has all features
            var adminRole = db.Query<Models.Settings.Role>().Where(x => x.Name == "Admin").FirstOrDefault();
            var guestRole = db.Query<Models.Settings.Role>().Where(x => x.Name == "Guest").FirstOrDefault();

            db.Execute("insert or ignore into User (Name, DisplayName, RoleId, PasswordHash, Enabled) values ('Guest', 'Guest', @0, null, 1)", guestRole.Id);

            var allAdminFeatures = db.Query<Models.Settings.RoleFeature>().Where(x => x.RoleId == adminRole.Id).ToList();
            var allGuestFeatures = db.Query<Models.Settings.RoleFeature>().Where(x => x.RoleId == guestRole.Id).ToList();
            var allFeatures = db.Fetch<Models.Settings.Feature>();
            var allUsers = db.Fetch<Models.Settings.User>();
            foreach (var feature in allFeatures)
            {
                if (!(from a in allAdminFeatures where a.FeatureId == feature.Id select a).Any())
                {
                    var f = new Models.Settings.RoleFeature();
                    f.RoleId = adminRole.Id;
                    f.FeatureId = feature.Id;
                    db.Insert(f);
                }


                //if there is no user, make sure that the guest has all features
                if (allUsers.Count == 1 && !(from a in allGuestFeatures where a.FeatureId == feature.Id select a).Any()) //guest is present by default
                {
                    var f = new Models.Settings.RoleFeature();
                    f.RoleId = guestRole.Id;
                    f.FeatureId = feature.Id;
                    db.Insert(f);
                }
            }
        }

        protected override NPoco.Database GetDatabase()
        {
            var fn = Path.Combine(Helpers.EnvironmentHelper.RootDataFolder, "settings.db3");
            var con = new SqliteConnection(string.Format("data source={0}", fn));
            con.Open();
            //con.CreateCollation("nocase", new Comparison<string>((a, b) => { return string.Compare(a, b, true); }));
            return new SettingsDatabase(this, con);
        }

        public override void Execute(Action<NPoco.Database> action)
        {
            Execute(_settingsDatabase, action);
        }

        public override T GetPage<T, T2>(int page, int pageSize, string sortOn, bool sortAsc, string defaultSort, NPoco.Sql sql)
        {
            T result;
            result = GetPage<T, T2>(_settingsDatabase, page, pageSize, sortOn, sortAsc, defaultSort, sql);
            return result;
        }

        public override bool ExecuteWithinTransaction(Action<NPoco.Database, Guid> action)
        {
            bool result = false;
            result = ExecuteWithinTransaction(_settingsDatabase, action);
            return result;
        }
    }

}
