require.config({
    paths: {
        'delayer': "/js/shared/delayer",
        'storageHelper': "/js/shared/storageHelper",
        'dataServer': "/js/shared/dataServer",
        'elements': "/js/shared/elements",
        'pagedList': "/js/shared/pagedList",
        'pagedList-elements': "/js/shared/pagedList-elements",
        'contextMenu': "/js/shared/contextMenu"
    }
});

function CreatePagedList(elementOrId, url, callbackFunc) {
    require(["pagedList"],
        function (pagedList) {
            result = new pagedList.PagedList(elementOrId, url);
            result.getStyling().rowStyles(function (item) {
                var styles = "";
                if (item['Enabled'] !== undefined && !item['Enabled']) {
                    styles = "text-decoration: line-through;";
                }

                return styles;
            });
            result.getStyling().rowClasses(function (item) {
                var className = "";

                if (item['__selected']) {
                    className = "pagelist-row-selected";
                }
                return className;
            });
            callbackFunc(result);
        });
}

function createFilterItemsFromList(list, textForAll) {
    var filterItems = list.map(
        function (item) {
            return { Text: item.Name, Value: item.Id };
        }
    );
    if (textForAll) { // textForAll is either nothing (undefined or null) or something like '@Html.T("-All-")'
        filterItems.splice(0, 0, { Text: textForAll, Value: "" });
    }
    return filterItems;
}


function htmlEncode(value) {
    return $('<div/>').text(value).html().replace(/\n/g, "<br />");
}


var CortexxCoreHubInstance = {
    callBacks: [],
    hub: null,
    hubAssigned: function () {
    },
    hubStarted: function () {
    },
    hubClosed: function () {
    },
    onDataChanged: function (tables, htmlElement, callback) {
        var index = _.find(CortexxCoreHubInstance.callBacks, function (o) { return o === callback; });
        if (index === undefined) {
            CortexxCoreHubInstance.callBacks.push({ tables: tables, htmlElement: htmlElement, callback: callback });
            if (htmlElement !== undefined) {
                $('body').on('DOMNodeRemoved', htmlElement, function (event) {
                    if (event.target === htmlElement || event.target.contains(htmlElement)) {
                        CortexxCoreHubInstance.deregisterOnClient(callBackFunction);
                    }
                });
            }
        }
    },
    deregisterOnDataChanged: function (callBackFunction) {
        var index = _.findIndex(CortexxCoreHubInstance.callBacks, function (o) { return o === callBackFunction; });
        if (index > -1) {
            CortexxCoreHubInstance.callBacks.splice(index, 1);
        }
    },
    dataHasChanged: function (tables) {
        for (var tableIndex = 0; tableIndex < tables.length; tableIndex++) {
            for (var index = 0; index < CortexxCoreHubInstance.callBacks.length; index++) {
                if (_.findIndex(CortexxCoreHubInstance.callBacks[index].tables, function (o) { return o === tables[tableIndex]; }) >= 0) {
                    CortexxCoreHubInstance.callBacks[index].callback();
                    break;
                }
            }
        }
    }
};

var __isUnloadingPage = false;

window.addEventListener('beforeunload', function () {
    __isUnloadingPage = true;
    return true;
}, false);

$(function () {
    CortexxCoreHubInstance.hub = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/IoTControlKitHub")
        .configureLogging(signalR.LogLevel.Information)
        .build();

    CortexxCoreHubInstance.hubAssigned();
    CortexxCoreHubInstance.hub.start().then(function () {
        CortexxCoreHubInstance.hub.on("dataChanged", (tables) => { CortexxCoreHubInstance.dataHasChanged(tables); });
        CortexxCoreHubInstance.hub.onclose(function (e) {
            if (!__isUnloadingPage) {
                CortexxCoreHubInstance.hubClosed();
            }
        });
        CortexxCoreHubInstance.hubStarted();
    }).catch(err => console.error(err.toString()));
});