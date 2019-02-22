using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MQTTnet;

namespace IoTControlKit.Services.MQTT
{
    public class HomieClient: Client
    {
        private Dictionary<string, Models.Application.Device> _devices;
        private Dictionary<long, Dictionary<string, Models.Application.DeviceProperty>> _properties;
        private Dictionary<long, Models.Application.DeviceProperty> _propertyLookup; //DevicePropertyId -> DeviceProperty
        private Dictionary<long, long> _propertyValues; //DevicePropertyId -> Id
        private int _skipTopicParts = 1;

        public HomieClient(Models.Application.MQTTClient poco)
            : base(poco)
        {
        }

        protected override void PrepareFirstConnect()
        {
            ApplicationService.Instance.Database.ExecuteWithinTransaction((db, session) =>
            {
                _devices = db.Fetch<Models.Application.Device>().ToDictionary(x => x.NormalizedName, x => x);
                _properties = new Dictionary<long, Dictionary<string, Models.Application.DeviceProperty>>();
                _propertyLookup = new Dictionary<long, Models.Application.DeviceProperty>();
                foreach (var d in _devices.Values)
                {
                    var propsForDevice = db.Query<Models.Application.DeviceProperty>().Where(x => x.DeviceId == d.Id).ToList();
                    _properties.Add(d.Id, propsForDevice.ToDictionary(x => x.NormalizedName, x => x));
                    foreach (var dp in propsForDevice)
                    {
                        _propertyLookup.Add(dp.Id, dp);
                    }
                }
                _propertyValues = db.Fetch<Models.Application.DevicePropertyValue>().ToDictionary(x => x.DevicePropertyId, x => x.Id);
            });
            _skipTopicParts = _clientSetting.BaseTopic.Split('/').Length-1;
        }

        protected override void SetDevicePropertyValue(NPoco.Database db, List<ApplicationService.SetDeviceProperties> properties)
        {
            if (_propertyLookup != null)
            {
                foreach (var p in properties)
                {
                    if (_propertyLookup.TryGetValue(p.DevicePropertyId, out var devprop))
                    {
                        var v = p.Value;
                        if (!string.IsNullOrEmpty(devprop.DataType))
                        {
                            switch (devprop.DataType.ToLower())
                            {
                                case "boolean":
                                    if (string.Compare(p.Value, "false", true) == 0
                                        || string.Compare(p.Value, "0", true) == 0
                                        || string.Compare(p.Value, "off", true) == 0)
                                    {
                                        v = "false";
                                    }
                                    else
                                    {
                                        v = "true";
                                    }
                                    break;
                                case "float":
                                    //todo
                                    break;
                                case "integer":
                                    //todo
                                    break;
                            }
                        }
                        Models.Application.DevicePropertyValue dpv = null;
                        if (!_propertyValues.TryGetValue(p.DevicePropertyId, out var pv))
                        {
                            dpv = new Models.Application.DevicePropertyValue()
                            {
                                DevicePropertyId = p.DevicePropertyId
                            };
                            db.Save(dpv);
                            _propertyValues.Add(p.DevicePropertyId, dpv.Id);
                        }
                        else
                        {
                            dpv = db.Query<Models.Application.DevicePropertyValue>().Where(x => x.Id == pv).First();
                        }
                        if (string.Compare(dpv.Value, v) != 0)
                        {
                            dpv.Value = v;
                            dpv.LastSetValue = v;
                            dpv.LastSetValueAt = DateTime.UtcNow;
                            db.Save(dpv);
                        }
                        if (!p.InternalOnly && devprop.Settable == true && _mqttClient.IsConnected)
                        {
                            var deviceName = db.ExecuteScalar<string>("select NormalizedName from Device where Id=@0", devprop.DeviceId);
                            try
                            {
                                _mqttClient.PublishAsync(new MqttApplicationMessage()
                                {
                                    Topic = $"{_clientSetting.BaseTopic.Replace("#", "")}{deviceName}/{devprop.NormalizedName}/set",
                                    QualityOfServiceLevel = MQTTnet.Protocol.MqttQualityOfServiceLevel.AtMostOnce,
                                    Retain = true,
                                    Payload = System.Text.UTF8Encoding.UTF8.GetBytes(dpv.Value)
                                }).Wait();
                            }
                            catch
                            {
                                //todo: revert?
                            }
                        }
                    }
                }
            }
        }

        protected override void ApplicationMessageReceived(object sender, MqttApplicationMessageReceivedEventArgs e)
        {
            base.ApplicationMessageReceived(sender, e);

            //topic assumption:
            //subscription: homie/# (device)
            //homie/X/Y/Z/$ (e.g. homie/lamp-eethoek-kastje/color/$datatype)
            //homie/X/Y/$ (e.g. homie/werkkamer-lamp/$type)
            //homie/X/$ (e.g. homie/homey/$name)
            //homie/$ (e.g. homie/$nodes)

            //X->Device, Y->DeviceProperty, Z->DevicePropertyValue
            var parts = e.ApplicationMessage.Topic.Split('/').Skip(_skipTopicParts).ToList();
            if (parts.Any() && parts[parts.Count-1] != "set")
            {
                ApplicationService.Instance.Database.ExecuteWithinTransaction((db, session) =>
                {
                    var payloadString = e.ApplicationMessage.ConvertPayloadToString();
                    if (parts[0].StartsWith("$"))
                    {
                        //special
                        //todo
                    }
                    else
                    {
                        if (!_devices.TryGetValue(parts[0], out var device))
                        {
                            device = new Models.Application.Device()
                            {
                                DeviceControllerId = _deviceController.Id,
                                NormalizedName = parts[0],
                                Enabled = true
                            };
                            db.Save(device);
                            _devices.Add(device.NormalizedName, device);
                        }
                        if (!device.Enabled)
                        {
                            device.Enabled = true;
                            db.Save(device);
                        }
                        if (parts.Count>1 && parts[1].StartsWith("$"))
                        {
                            //property of a device
                            //$name, $type, $properties
                            switch (parts[1])
                            {
                                case "$name":
                                    if (string.Compare(device.Name, payloadString) != 0)
                                    {
                                        device.Name = payloadString;
                                        db.Save(device);
                                    }
                                    break;
                                case "$type":
                                    if (string.Compare(device.DeviceType, payloadString) != 0)
                                    {
                                        device.DeviceType = payloadString;
                                        db.Save(device);
                                    }
                                    break;
                                case "$properties":
                                    //todo
                                    break;
                            }
                        }
                        else if (parts.Count > 1)
                        {
                            if (!_properties.TryGetValue(device.Id, out var properties))
                            {
                                properties = new Dictionary<string, Models.Application.DeviceProperty>();
                                _properties.Add(device.Id, properties);
                            }
                            if (!properties.TryGetValue(parts[1], out var deviceProperty))
                            {
                                deviceProperty = new Models.Application.DeviceProperty()
                                {
                                    DeviceId = device.Id,
                                    NormalizedName = parts[1]
                                };
                                db.Save(deviceProperty);
                                properties.Add(deviceProperty.NormalizedName, deviceProperty);
                                _propertyLookup.Add(deviceProperty.Id, deviceProperty);

                                var propertyValue = new Models.Application.DevicePropertyValue()
                                {
                                     DevicePropertyId = deviceProperty.Id                                      
                                };
                                db.Save(propertyValue);
                                _propertyValues.Add(deviceProperty.Id, propertyValue.Id);
                            }
                            if (parts.Count == 3 && parts[2].StartsWith("$"))
                            {
                                //property of a property (not the value)
                                switch (parts[2])
                                {
                                    case "$name":
                                        if (string.Compare(device.Name, payloadString) != 0)
                                        {
                                            deviceProperty.Name = payloadString;
                                            db.Save(deviceProperty);
                                        }
                                        break;
                                    case "$datatype":
                                        if (string.Compare(deviceProperty.DataType, payloadString) != 0)
                                        {
                                            deviceProperty.DataType = payloadString;
                                            db.Save(device);
                                        }
                                        break;
                                    case "$format":
                                        if (string.Compare(deviceProperty.Format, payloadString) != 0)
                                        {
                                            deviceProperty.Format = payloadString;
                                            db.Save(device);
                                        }
                                        break;
                                    case "$unit":
                                        if (string.Compare(deviceProperty.Unit, payloadString) != 0)
                                        {
                                            deviceProperty.Unit = payloadString;
                                            db.Save(device);
                                        }
                                        break;
                                    case "$settable":
                                        if (deviceProperty.Settable != (payloadString == "true"))
                                        {
                                            deviceProperty.Settable = (payloadString == "true");
                                            db.Save(device);
                                        }
                                        break;
                                    case "$retained":
                                        if (deviceProperty.Retained != (payloadString == "true"))
                                        {
                                            deviceProperty.Retained = (payloadString == "true");
                                            db.Save(device);
                                        }
                                        break;
                                }
                            }
                            else if (parts.Count == 2)
                            {
                                //value of property
                                if (!_propertyValues.TryGetValue(deviceProperty.Id, out var pvId))
                                {
                                    var propertyValue = new Models.Application.DevicePropertyValue()
                                    {
                                        DevicePropertyId = deviceProperty.Id
                                    };
                                    db.Save(propertyValue);
                                    _propertyValues.Add(deviceProperty.Id, propertyValue.Id);
                                    pvId = propertyValue.Id;
                                }
                                var pv = db.Query<Models.Application.DevicePropertyValue>().Where(x => x.Id == pvId).FirstOrDefault();
                                if (pv != null)
                                {
                                    pv.Value = payloadString;
                                    pv.LastReceivedValue = payloadString;
                                    pv.LastReceivedValueAt = DateTime.UtcNow;
                                    db.Save(pv);
                                }
                            }
                        }
                    }
                });
            }
        }
    }
}
