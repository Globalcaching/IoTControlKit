export class DataServer {
    url: string = '';
    constructor() {
    }

    getPageData = (data, onSuccess, onFailure) => {
        console.error("Server.getPageData should be overridden.");
    }
}

export class AjaxServer extends DataServer {
    constructor(url: string) {
        super();
        this.url = url;
    }

    getPageData = (data, onSucces, onFailure) => {
        let self = this;
        $.ajax({
            'type': 'POST',
            'url': self.url,
            'data': data,
            'success': onSucces,
            'error': onFailure
        })
    }
}

// TODO TODO TODO
export class FakeServer extends DataServer {
    data: any = null
    filteredData: any = null

    constructor() {
        super();
        this.data = [];
    }


    getMaxId = () => {
        let self = this;
        let result: number = 0;
        let item: any;
        for (item of self.data) {
            if (item['Id'] > result) {
                result = item['Id'];
            }
        }
    }

    addData = (items) => {
        let self = this;
        let item: any;
        for (item of items) {
            self.data.push(item);
        }
    }

    getData = () => {
        let self = this;
        return self.data;
    }

    clearData = () => {
        let self = this;
        self.data = [];
    }

    getItem = (id: number) => {
        let self = this;
        let item: any;
        for (item of self.data) {
            if (item['Id'] = id) {
                return item;
            }
        }
        return null;
    }

    deleteItem = (id: number) => {
        let self = this;
        let i: number = 0;
        while (i < self.data.length) {
            if (self.data[i]['Id'] == id) {
                self.data.splice(i, 1)
                break
            }
            i++;
        }
    }

    static getNestedValue = (obj, fields) => {
        if (obj == null || fields.length == 0) {
            return obj;
        }

        return FakeServer.getNestedValue(obj[fields[0]], fields.slice(1));
    }

    // returns list of functions which accepts an data item and returns true/false.
    getFilters = (filterColumns, filterValues) => {
        let result = [];
        let passFilter = function (field, value, item) {
            let itemValue = FakeServer.getNestedValue(item, field.split('.'));
            if (itemValue == null) {
                if (value == 'NULL') {
                    return true
                }
                return false;
            }

            if (Object.prototype.toString.call(itemValue) == '[object Number]') {
                if (isNaN(value)) {
                    return false
                }
                else {
                    let result = itemValue == parseFloat(value);
                    return result
                }
            }
            else {
                let match = itemValue.toString().toLowerCase().indexOf(value.toString().toLowerCase());
                return (match >= 0);
            }
        }

        for (let i = 0; i < filterColumns.length; i++) {
            if (!(filterValues[i] == '')) {
                result.push(passFilter.bind(null, filterColumns[i], filterValues[i]));
            }
        }

        return result;
    }

    // format: SendData = { 'page', 'pageSize', 'filterColumns', 'filterValues', 'sortOn', 'sortAsc' }
    getPageData = (data, onSuccess, onFailure) => {
        let self = this;
        let items = [];
        let pagedItems = []

        // filtering
        let filters = self.getFilters(data.filterColumns, data.filterValues);
        let item: any;
        for (item of self.data) {
            if (filters.every(passFilter => passFilter(item))) {
                items.push(item);
            }
        }

        // sorting
        if (data.sortOn != undefined && data.sortOn != '') {
            var fields = data.sortOn.split('.');
            let compare = function (a, b) {
                let aValue = FakeServer.getNestedValue(a, fields);
                let bValue = FakeServer.getNestedValue(b, fields);
                if (aValue != null && bValue != null) {
                    let isNumber = function (v) {
                        return Object.prototype.toString.call(v) == '[object Number]';
                    }

                    if (isNumber(aValue) && isNumber(bValue)) {
                        return aValue - bValue;
                    }
                    else {
                        return aValue.toString().localeCompare(bValue.toString());
                    }
                }
                else {
                    if (aValue == null && bValue == null) {
                        return 0;
                    }
                    else if (aValue == null) {
                        return -1;
                    }
                    else {
                        return 1;
                    }
                }
            }
            if (data.sortAsc) {
                items.sort(function (a, b) { return compare(a, b); });
            }
            else {
                items.sort(function (a, b) { return compare(b, a); });
            }
        }
        let totalCount: number = items.length;
        self.filteredData = items

        // paging
        let nrOfPages: number = Math.max(1, Math.ceil(items.length / data.pageSize));
        let page: number = (data.page > nrOfPages) ? nrOfPages : (data.page < 1) ? 1 : data.page;
        let indexFrom: number = (page - 1) * data.pageSize;
        let indexTo: number = indexFrom + data.pageSize;
        pagedItems = items.slice(indexFrom, indexTo);

        let result = {};
        result['Items'] = pagedItems;
        result['CurrentPage'] = page;
        result['PageCount'] = nrOfPages;
        result['TotalCount'] = totalCount;
        onSuccess(result);
    }
}