viewModel.mqtt_BaseTopic = ko.observable('').extend({ required: "" });
viewModel.mqtt_MQTTType = ko.observable('');
viewModel.mqtt_TcpServer = ko.observable('').extend({ required: "" });
viewModel.mqtt_availableMQTTTypes = ko.observableArray(['homie']);

var selectedMqttClient;

function editMQTTProperty(controller, mqttClient) {
    selectedMqttClient = mqttClient;

    if (controller != null) {
        viewModel.Name(controller.Name);
        viewModel.Enabled(controller.Enabled);
    }
    else {
        viewModel.Name('MQTT Client');
        viewModel.Enabled(true);
    }

    viewModel.mqtt_BaseTopic(mqttClient.BaseTopic);
    viewModel.mqtt_MQTTType(mqttClient.MQTTType);
    viewModel.mqtt_TcpServer(mqttClient.TcpServer);
    $('#dialog-editMQTTProperty').appendTo('body').modal();
}

function saveMQTTController() {
    if (selectedItem == null) {
        selectedItem = {};
        selectedItem.Id = 0;
        selectedItem.Plugin = 'IoTControlKit.PhilipsHue';
    }
    selectedMqttClient.Name = viewModel.Name();

    selectedMqttClient.BaseTopic = viewModel.mqtt_BaseTopic();
    selectedMqttClient.Enabled = viewModel.Enabled();
    selectedMqttClient.MQTTType = viewModel.mqtt_MQTTType();
    selectedMqttClient.TcpServer = viewModel.mqtt_TcpServer();

    saveController('IoTControlKit.MQTT', selectedItem, selectedMqttClient);
}

registerPlugin('IoTControlKit.MQTT',
    function (controller, mqttClient) {
        editMQTTProperty(controller, mqttClient);
    },
    null);
