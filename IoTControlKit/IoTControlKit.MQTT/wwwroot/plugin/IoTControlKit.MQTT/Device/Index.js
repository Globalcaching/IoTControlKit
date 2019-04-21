viewModel.BaseTopic = ko.observable('').extend({ required: "" });
viewModel.MQTTType = ko.observable('');
viewModel.TcpServer = ko.observable('').extend({ required: "" });
viewModel.availableMQTTTypes = ko.observableArray(['homie']);

var selectedMqttClient;

function editMQTTProperty(controller, mqttClient) {
    selectedMqttClient = mqttClient;
    viewModel.Name(controller.Name);
    viewModel.BaseTopic(mqttClient.BaseTopic);
    viewModel.Enabled(controller.Enabled);
    viewModel.MQTTType(mqttClient.MQTTType);
    viewModel.TcpServer(mqttClient.TcpServer);
    $('#dialog-editMQTTProperty').appendTo('body').modal();
}

function saveMQTTProperty() {
    selectedItem.Name = viewModel.Name();
    selectedMqttClient.Name = viewModel.Name();

    selectedMqttClient.BaseTopic = viewModel.BaseTopic();
    selectedMqttClient.Enabled = viewModel.Enabled();
    selectedMqttClient.MQTTType = viewModel.MQTTType();
    selectedMqttClient.TcpServer = viewModel.TcpServer();

    saveController('IoTControlKit.MQTT', selectedItem, selectedMqttClient);
}

registerPlugin('IoTControlKit.MQTT',
    function (controller, mqttClient) {
        editMQTTProperty(controller, mqttClient);
    },
    null);
