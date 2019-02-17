using MQTTnet;
using MQTTnet.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTControlKit.Services.MQTT
{
    public class Client: IDisposable
    {
        protected Models.Application.MQTTClient _clientSetting { get; private set; }
        protected IMqttClient _mqttClient { get; private set; }
        protected bool _subscribed { get; private set; }

        public Client(Models.Application.MQTTClient poco)
        {
            _clientSetting = poco;
            _subscribed = false;
        }

        public virtual void Start()
        {
            var factory = new MqttFactory();
            _mqttClient = factory.CreateMqttClient();
            _mqttClient.Connected += _mqttClient_Connected;
            _mqttClient.Disconnected += _mqttClient_Disconnected;
            _mqttClient.ApplicationMessageReceived += _mqttClient_ApplicationMessageReceived;
            Connect();
        }

        private void _mqttClient_Disconnected(object sender, MqttClientDisconnectedEventArgs e)
        {
            _subscribed = false;
            //todo: retry
        }

        private void _mqttClient_Connected(object sender, MqttClientConnectedEventArgs e)
        {
            Subscribe();
        }

        public virtual string TopicSubscription => _clientSetting.BaseTopic;

        private void Connect()
        {
            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(_clientSetting.TcpServer)
                .Build();

            _mqttClient.ConnectAsync(options).Wait();
        }

        public void Subscribe()
        {
            _subscribed = true;
            _mqttClient.SubscribeAsync(new TopicFilterBuilder().WithTopic(TopicSubscription).Build()).Wait();
        }

        private void _mqttClient_ApplicationMessageReceived(object sender, MqttApplicationMessageReceivedEventArgs e)
        {
            ApplicationMessageReceived(sender, e);
        }

        protected virtual void ApplicationMessageReceived(object sender, MqttApplicationMessageReceivedEventArgs e)
        {
            Services.LoggerService.Instance.LogInformation($"MTTQ Client {_clientSetting.Name} => From={e.ClientId} -> Message: Topic={e.ApplicationMessage?.Topic}, Payload={(e.ApplicationMessage?.Payload == null ? "" : Encoding.UTF8.GetString(e.ApplicationMessage.Payload))}, QoS={e.ApplicationMessage?.QualityOfServiceLevel}, Retain={e.ApplicationMessage?.Retain}");
        }

        public void Dispose()
        {
            if (_subscribed)
            {
                _subscribed = false;
                _mqttClient.ApplicationMessageReceived -= _mqttClient_ApplicationMessageReceived;
            }
            _mqttClient.ApplicationMessageReceived += _mqttClient_ApplicationMessageReceived;
            _mqttClient.Dispose();
        }
    }
}
