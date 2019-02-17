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
                if (item['Enabled']) {
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
