using IoTControlKit.ViewModels.Flow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTControlKit.Services
{
    public partial class ApplicationService : BaseService
    {
        public FlowViewModel GetFlowViewModel()
        {
            FlowViewModel result = new FlowViewModel();
            Database.Execute((db) =>
            {
                result.Flows = db.Fetch<Models.Application.Flow>();
                result.FlowComponents = db.Fetch<Models.Application.FlowComponent>();
                result.FlowConnectors = db.Fetch<Models.Application.FlowConnector>();

                var sql = NPoco.Sql.Builder.Select("DeviceProperty.*")
                    .Append(", Device.Name as DeviceName")
                    .Append(", DeviceController.Name as ControllerName")
                    .From("DeviceProperty")
                    .InnerJoin("Device").On("DeviceProperty.DeviceId=Device.Id")
                    .InnerJoin("DeviceController").On("Device.DeviceControllerId=DeviceController.Id");
                result.DeviceProperties = db.Fetch<DevicePropertyViewModel>(sql);
            });
            return result;
        }
    }
}
