viewModel.BaseTopic = ko.observable('').extend({ required: "" });
viewModel.MQTTType = ko.observable('');
viewModel.TcpServer = ko.observable('').extend({ required: "" });
viewModel.availableMQTTTypes = ko.observableArray(['homie']);

function editMQTTProperty(item) {
    $.post('@Url.Action("GetMQTT", "Device")', { id: item.MQTTClientId }, function (result) {
        selectedItem = result;
        viewModel.Name(selectedItem.Name);
        viewModel.BaseTopic(selectedItem.BaseTopic);
        viewModel.Enabled(selectedItem.Enabled);
        viewModel.MQTTType(selectedItem.MQTTType);
        viewModel.TcpServer(selectedItem.TcpServer);
        $('#dialog-editMQTTProperty').appendTo('body').modal();
    });
}

function saveMQTTProperty() {
    selectedItem.Name = viewModel.Name();
    selectedItem.BaseTopic = viewModel.BaseTopic();
    selectedItem.Enabled = viewModel.Enabled();
    selectedItem.MQTTType = viewModel.MQTTType();
    selectedItem.TcpServer = viewModel.TcpServer();
    $.post('@Url.Action("SaveMQTT", "Device")', { item: selectedItem });
}
