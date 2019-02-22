using IoTControlKit.ViewModels.Device;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTControlKit.Services
{
    public partial class ApplicationService : BaseService
    {
        public void SaveMQTT(Models.Application.MQTTClient item)
        {
            Database.ExecuteWithinTransaction((db, session) =>
            {
                db.Save(item);
            });
        }

        public Models.Application.MQTTClient GetMQTT(long id)
        {
            Models.Application.MQTTClient result = null;
            Database.Execute((db) =>
            {
                result = db.Query<Models.Application.MQTTClient>().Where(x => x.Id == id).FirstOrDefault();
            });
            return result;
        }

        public DeviceControllerViewModel GetDeviceControllers(int page, int pageSize, string sortOn = "", bool sortAsc = true)
        {
            var sql = NPoco.Sql.Builder.Select("DeviceController.*")
                .From("DeviceController");
            return Database.GetPage<DeviceControllerViewModel, DeviceControllerViewModelItem>(page, pageSize, sortOn, sortAsc, "Name", sql);
        }

        public DeviceViewModel GetDevices(int page, int pageSize, long controllerId, string filterName, string sortOn = "", bool sortAsc = true)
        {
            var sql = NPoco.Sql.Builder.Select("Device.*")
                .From("Device")
                .Where("Device.DeviceControllerId=@0", controllerId);
            if (!string.IsNullOrEmpty(filterName))
            {
                sql = sql.Append("and Device.Name like @0", string.Format("%{0}%", filterName));
            }
            return Database.GetPage<DeviceViewModel, DeviceViewModelItem>(page, pageSize, sortOn, sortAsc, "Name", sql);
        }

        public DevicePropertyViewModel GetDeviceProperties(int page, int pageSize, long deviceId, string sortOn = "", bool sortAsc = true)
        {
            var sql = NPoco.Sql.Builder.Select("DeviceProperty.*")
                .Append(", DevicePropertyValue.Value")
                .From("DeviceProperty")
                .LeftJoin("DevicePropertyValue").On("DeviceProperty.Id=DevicePropertyValue.DevicePropertyId")
                .Where("DeviceProperty.DeviceId=@0", deviceId);
            return Database.GetPage<DevicePropertyViewModel, DevicePropertyViewModelItem>(page, pageSize, sortOn, sortAsc, "Name", sql);
        }
    }
}
