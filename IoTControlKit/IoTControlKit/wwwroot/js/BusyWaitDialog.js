var myApp;
var myAppDlgCounter = 0;
myApp = myApp || (function () {
    //var pleaseWaitDiv = $('<div class="modal hide" id="pleaseWaitDialog" data-backdrop="static" data-keyboard="false"><div class="modal-header"><h1>Busy...</h1></div><div class="modal-body"><div class="progress progress-striped active"><div class="bar" style="width: 100%;"></div></div></div></div>');
    var pleaseWaitDiv = $('<div class="modal" id="pleaseWaitDiv" role="dialog" style="display:none"><div class="modal-dialog" data-backdrop="static" data-keyboard="false"><div class="modal-content"><h1><center>Please wait</center></h1><div class="progress progress-striped active"><div class="progress-bar" role="progressbar" aria-valuenow="100" aria-valuemin="0" aria-valuemax="100" style="width: 100%;"></div></div></div></div>');
    return {
        showPleaseWait: function () {
            myAppDlgCounter++;
            if (myAppDlgCounter == 1) {
                pleaseWaitDiv.modal({ show: true, backdrop: 'static', keyboard: false });
            }
        },
        hidePleaseWait: function () {
            myAppDlgCounter--;
            if (myAppDlgCounter == 0) {
                pleaseWaitDiv.modal('hide');
            }
        }
    };
})();