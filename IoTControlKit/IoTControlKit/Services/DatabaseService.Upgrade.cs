using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace IoTControlKit.Services
{
    public partial class DatabaseService : BaseService
    {
        private const long _targetDatabaseVersion = 1;

        private Services.Database.BaseDatabaseService GetNewDatabaseInstance()
        {
            Services.Database.BaseDatabaseService result = null;

            var fn = Path.Combine(Helpers.EnvironmentHelper.RootDataFolder, "application.db3");
            result = new Services.Database.ApplicationSQLiteDatabaseInstance(fn);

            result.ExecuteWithinTransaction((db, session) =>
            {
                var version = db.ExecuteScalar<long>("PRAGMA user_version");
                UpgradeDatabase(db, version);
                InitDatabaseContent(db);
            });

            return result;
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

                            db.Execute(@"create table if not exists MQTTClient(
Id integer PRIMARY KEY,
Name nvarchar(255) not null UNIQUE,
BaseTopic nvarchar(255) not null,
Enabled bit not null,
MQTTType nvarchar(255) not null,
TcpServer nvarchar(255) not null UNIQUE
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
            db.Execute("insert or ignore into MQTTClient (Name, BaseTopic, Enabled, MQTTType, TcpServer) values ('Local', 'homie/#',  1, 'homie', 'localhost')");
        }
    }
}
