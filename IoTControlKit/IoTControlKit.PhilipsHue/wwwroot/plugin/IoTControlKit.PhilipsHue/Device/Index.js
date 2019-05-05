viewModel.philipsHue_TcpServer = ko.observable('').extend({ required: "" });

var selectedPhilipsHueClient;

function editPhilipsHueProperty(controller, philipsHueClient) {
    selectedPhilipsHueClient = philipsHueClient;
    if (controller != null) {
        viewModel.Name(controller.Name);
        viewModel.Enabled(controller.Enabled);
    }
    else {
        viewModel.Name('Philips Hue Hub');
        viewModel.Enabled(true);
    }
    viewModel.philipsHue_TcpServer(philipsHueClient.TcpServer);
    $('#dialog-editPhilipsHueProperty').appendTo('body').modal();
}

function savePhilipsHueController() {
    if (selectedItem == null) {
        selectedItem = {};
        selectedItem.Id = 0;
        selectedItem.Plugin = 'IoTControlKit.PhilipsHue';
    }
    selectedItem.Name = viewModel.Name();
    selectedItem.Enabled = viewModel.Enabled();

    selectedPhilipsHueClient.TcpServer = viewModel.philipsHue_TcpServer();

    saveController('IoTControlKit.PhilipsHue', selectedItem, selectedPhilipsHueClient);
}

registerPlugin('IoTControlKit.PhilipsHue',
    function (controller, philipsHueClient) {
        editPhilipsHueProperty(controller, philipsHueClient);
    },
    null);
