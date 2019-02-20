using IoTControlKit.ViewModels.Device;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTControlKit.Services
{
    public partial class ApplicationService : BaseService
    {
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
    }
}
