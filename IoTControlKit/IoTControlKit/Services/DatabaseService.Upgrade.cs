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
        private const long _targetDatabaseVersion = 2;

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

                            db.Execute(@"create table if not exists DeviceController(
Id integer PRIMARY KEY,
MQTTClientId integer REFERENCES MQTTClient (Id),
NormalizedName nvarchar(255),
Name nvarchar(255),
State nvarchar(255),
Ready bit not null
)");

                            db.Execute(@"create table if not exists Device(
Id integer PRIMARY KEY,
DeviceControllerId integer not null REFERENCES DeviceController (Id),
NormalizedName nvarchar(255),
Name nvarchar(255),
DeviceType nvarchar(255),
Enabled bit not null
)");

                            db.Execute(@"create table if not exists DeviceProperty(
Id integer PRIMARY KEY,
DeviceId integer not null REFERENCES Device (Id),
NormalizedName nvarchar(255),
Name nvarchar(255),
Retained bit,
Settable bit,
DataType nvarchar(255),
Unit nvarchar(255),
Format nvarchar(255)
)");

                            db.Execute(@"create table if not exists DevicePropertyValue(
Id integer PRIMARY KEY,
DevicePropertyId integer not null REFERENCES DeviceProperty (Id),
Value nvarchar(255),
LastReceivedValue nvarchar(255),
LastSetValue nvarchar(255),
LastReceivedValueAt datetime,
LastSetValueAt datetime
)");

                            newVersion = 1;
                            break;
                        case 1:
                            db.Execute(@"create table if not exists Flow(
Id integer PRIMARY KEY,
Guid nvarchar(255) not null,
Name nvarchar(255) not null,
Enabled bit not null
)");

                            db.Execute(@"create table if not exists FlowComponent(
Id integer PRIMARY KEY,
FlowId integer not null REFERENCES Flow (Id),
Guid nvarchar(255) not null,
Type nvarchar(255) not null,
DevicePropertyId integer REFERENCES DeviceProperty (Id),
Value nvarchar(255) not null,
PositionX integer not null,
PositionY integer not null
)");

                            db.Execute(@"create table if not exists FlowConnector(
Id integer PRIMARY KEY,
Guid nvarchar(255) not null,
TargetFlowComponentd integer not null REFERENCES FlowComponent (Id),
SourceFlowComponentd integer not null REFERENCES FlowComponent (Id),
SourcePort nvarchar(255) not null
)");

                            newVersion = 2;
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
            db.Execute("insert or ignore into MQTTClient (Name, BaseTopic, Enabled, MQTTType, TcpServer) values ('Local', 'Homey/homie/#',  1, 'homie', 'localhost')");
        }
    }
}
