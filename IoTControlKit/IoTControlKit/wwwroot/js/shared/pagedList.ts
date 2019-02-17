import { ElementWrapper, element, Element } from "./elements";
import { PagedListButton, PagedListColumn, Pager, PagedListRow } from "pagedList-elements";
import { Delayer } from "delayer";
import { DataServer, AjaxServer, FakeServer } from "dataServer";

let version = '0.0.0.3';

declare var _T: any

class ScrollPosition {
    doc = document.documentElement;
    _left: number;
    _top: number;
    constructor() {
    }

    left = () => {
        return (window.pageXOffset || this.doc.scrollLeft) - (this.doc.clientLeft || 0);
    }
    top = () => {
        return (window.pageYOffset || this.doc.scrollTop) - (this.doc.clientTop || 0);
    }

    save = () => {
        this._left = this.left();
        this._top = this.top();
    }

    restore = () => {
        window.scrollTo(this._left, this._top);
    }
}

let scrollPosition = new ScrollPosition();

function namedTuple(fields, values = null) {
    let result = {};
    for (let field of fields) {
        result[field] = null;
    }

    if (values != null) {
        for (let i = 0; i < values.length; i++) {
            result[fields[i]] = values[i];
        }
    }

    return result;
}

function containsMore(object, fields) {
    for (let key of Object.keys(object)) {
        if (fields.indexOf(key) == -1) {
            return true;
        }
    }
    return false;
}

export function containsAll(object: any, fields) {
    for (let f of fields) {
        if (!object.hasOwnProperty(f)) {
            return false;
        }
    }
    return true;
}

let DefaultButtons: PagedListButton[] = [];

export function addDefaultButton(id: number, name: string, styleClass: string) {
    let result = new PagedListButton(id, name, styleClass);
    DefaultButtons.push(result);
    return result;
}

export class PagedList extends ElementWrapper {
    SendData =  [ 'page', 'pageSize', 'filterColumns', 'filterValues', 'sortOn', 'sortAsc' ];
    ReceiveData = { 'Items': 'Items', 'CurrentPage': 'CurrentPage', 'PageCount': 'PageCount', 'TotalCount': 'TotalCount' };

    topPager = new Pager(element('div'), this);
    tableContainer = Element('div')
    table = Element('table')
    thead = Element('thead') 
    tbody = Element('tbody')
    headerRendered = false
    rows: PagedListRow[] = []
    bottomPager = new Pager(element('div'), this)
    receiveData = null; //# object with field as in ReceiveData
    buttons: PagedListButton[] = []
    columns: PagedListColumn[] = []
    sorting = { 'columnIndex': -1, 'ascending': true } // initial values, no sorting
    pageSize: number = 20;  // default value
    mergeButtonColumns: boolean = false
    _onPageRefreshed = null
    _onPageRefreshing = null
    refreshDelayer: Delayer = new Delayer(500) // to make sure the refresh function does not get called too many times (for example caused by callbacks on server triggers)
    refreshStaticServerDelayer: Delayer = new Delayer(5)
    styling: PagedListStyling
    containerId: string = ''
    _server: DataServer = null
    onDragStartFunction: any = null
    filterId: string = ''
    enableShowHideColumns: boolean = false

    constructor(container, url: string) {
        super(container);
        if (Object.prototype.toString.call(container) == '[object String]') { // container is an id
            this.containerId = container;
            container = document.querySelector(this.containerId);
            if (container == null) {
                console.error("Paged-List cannot find container with id " + this.containerId);
            }
            super(container);
        }
        else {
            this.containerId = (container.id != '') ? container.id : 'Unknown id';
        }
        this.table.append(this.thead);
        this.table.append(this.tbody);
        this.tableContainer.append(this.table)
        this.append(this.topPager, this.tableContainer, this.bottomPager)

        if (url == null || url == "") {
            this._server = new FakeServer(); // type: DataServer
        }
        else {
            this._server = new AjaxServer(url); // type: DataServer
        }
        this.styling = new PagedListStyling(this)
        this.styling.tableClass('table table-striped table-hover')
    }

    getStyling = () => {
        let self = this;
        return self.styling;
    }

    addColumn = (id: string, header = id) => {
        let self = this;
        let column = new PagedListColumn(id, header.toString());
        self.columns.push(column);
        return column;
    }

    setUrl = (url: string) => {
        let self = this;
        self._server.url = url;
        return self;
    }

    getUrl = () => {
        let self = this;
        if (self._server != null) {
            return self._server.url;
        }
        return '';
    }

    onPageRefreshed = (func = null) => {
        let self = this;
        if (typeof (func) == 'function') {
            self._onPageRefreshed = func;
        }
        else if (func == null) {
            self._onPageRefreshed = null;
        }
        else {
            console.error(".onPageRefreshed on Paged-List for container with id " + self.containerId + " failed. Passed argument is not a function.");
        }
    }

    onPageRefreshing = (func = null) => {
        let self = this;
        if (typeof (func) == 'function') {
            self._onPageRefreshing = func;
        }
        else if (func == null) {
            self._onPageRefreshing = null;
        }
        else {
            console.error(".onPageRefreshing on Paged-List for container with id " + self.containerId + " failed. Passed argument is not a function.");
        }
    }

    addButton = (id: number, name: string, styleClass: string, columnStyleAttribute: string) => {
        let self = this;
        let button: PagedListButton = null;
        if (styleClass == null) {
            button = new PagedListButton(id, id.toString(), name, columnStyleAttribute);
        }
        else {
            button = new PagedListButton(id, name, styleClass, columnStyleAttribute);
        }

        self.buttons.push(button);
        return button;
    }

    getTopPager = () => {
        let self = this;
        return self.topPager;
    }

    getBottomPager = () => {
        let self = this;
        return self.bottomPager;
    }

    getRow = (item) => {
        let self = this;
        return _.find(self.rows, function (o) { return o.item == item })
    }

    hideCount = () => { // to hide the counter (Total: ...)
        let self = this;
        self.topPager.hideCount();
        self.bottomPager.hideCount();
        return self;
    }

    disablePagination = () => {
        let self = this;
        self.topPager.disable();
        self.bottomPager.disable();
        return self;
    }

    addDefaultButtons = (ids: string) => {
        let self = this;
        let button: any;
        for (button of DefaultButtons) {
            if (ids.indexOf(button.id) > -1) {
                let newButton = button.copy();
                if (self[newButton.id] == null) {
                    self[newButton.id] = newButton
                    self.buttons.push(newButton)
                }
                else {
                    console.error("Paged-List for container with id " + self.containerId + " cannot add default button '" + newButton.id + "' since it already exists.");
                }
            }
        }

        let n: string;
        //for (n of ids) {
            if (DefaultButtons.findIndex(b => b.id.toString() == ids) == -1) {
                console.error("Paged-List for container with id " + self.containerId + " cannot add default button '" + n + "' since this isn't a default button.");
            }
        //}
    }

    clearFilters = () => {
        let self = this;
        for (let col of self.columns) {
            col.clearFilter()
        }
    }


    renderHeader = () => {
        let self = this;
        if (self.columns.length == 0) {
            console.error("Paged-List for container with id " + self.containerId + " cannot render header. It does not contain columns.")
        } 

        let tr = Element('tr');
        self.thead.append(tr);

        for (let i = 0; i < self.columns.length; i++) {
            let column = self.columns[i];
            let th = Element('th');
            tr.append(th);

            if (column.classesHeader.length > 0) {
                th.attr('class', column.classesHeader.join(' '));
            }
            if (column.stylesHeader.length > 0) {
                th.attr('style', column.stylesHeader.join(' '));
            }

            let elements = column.getElements(self);
            let e: any;
            for (e of elements) {
                th.append(e);
            }
            column.span.element.onclick = self.toggleSort.bind(null, i);
        }

        if (self.buttons.length > 0) {
            for (let button of self.buttons) {
                let th = Element('th').attr('class', self.styling.classButtonColumn)
                tr.append(th);
                if (self.mergeButtonColumns) {
                    break;
                }
                if (button.columnStyleAttribute != null) {
                    th.attr("style", button.columnStyleAttribute);
                }
            }
        }
        self.headerRendered = true;
    }

    toggleSort = (columnIndex: number) => {
        let self = this;
        let column = self.columns[columnIndex];
        if (column.sortable) {
            if (self.sorting.columnIndex >= 0) {
                self.columns[self.sorting.columnIndex].toggleFigure.attr('class', '');
            }
            if (self.sorting.columnIndex == columnIndex) {
                if (self.sorting.ascending) {
                    self.sorting.ascending = false;
                }
                else {
                    self.sorting.columnIndex = -1;
                }
            }
            else {
                self.sorting.columnIndex = columnIndex;
                self.sorting.ascending = true;
            }

            if (self.sorting.columnIndex >= 0) {
                if (self.sorting.ascending) {
                    column.toggleFigure.attr('class', self.styling.classAscending)
                }
                else {
                    column.toggleFigure.attr('class', self.styling.classDescending)
                }
            }
            self.getData(1, true);
        }
    }

    onDragStart = (onDragStartFunction) => {
        let self = this;
        if (typeof (onDragStartFunction) != 'function') {
            console.error(".onDragStart failed. Passed argument is not a function.");
        }
        self.onDragStartFunction = onDragStartFunction;
        return self;
    }

    render = (data, fullPage: boolean) => {
        let self = this;
        if (self.columns.length == 0) {
            console.error("Paged-List for container with id " + self.containerId + " cannot render. It does not contain columns.");
        }

        if (!containsAll(data, self.ReceiveData)) {
            console.error("Paged-List for container with id " + self.containerId + " cannot render. Received data does not contain all required fields: " + self.ReceiveData + ".");
        }

        if (self._onPageRefreshing != null) {
            self._onPageRefreshing();
        }

        if (data.CurrentPage > data.PageCount && data.PageCount > 0) {
            self.getData(data.PageCount);
            return;
        }

        self.receiveData = data;

        // render header if not done yet
        if (!self.headerRendered) {
            self.renderHeader();
        }

        // set property 'Id' of item if item has property 'id' but not 'Id'
        if (!fullPage) {
            let item: any;
            for (item of data.Items) {
                if (item.hasOwnProperty('id') && !item.hasOwnProperty('Id')) {
                    item['Id'] = item['id'];
                }
            }
        }

        // if not all items in data have property 'Id', force to rerender full page
        if (!fullPage && !data.Items.every(item => item.hasOwnProperty('Id'))) {
            fullPage = true;
        }

        // remove all current rows
        if (fullPage) {
            scrollPosition.save()
            while (self.rows.length > 0) {
                self.rows[0].remove();
            }
        }

        // remove all current rows which are not in data
        if (!fullPage) {
            let i: number = 0;
            while (i < self.rows.length) {
                let row: PagedListRow = self.rows[i];
                if (!data.Items.find(item => item == row.item)) {
                    row.remove();
                }
                else {
                    i++;
                }
            }
        }

        // refresh top- and bottom-pagers
        self.topPager.refresh(data.CurrentPage, data.PageCount, data.TotalCount);
        self.bottomPager.refresh(data.CurrentPage, data.PageCount, data.TotalCount);

        // add or update rows for all items in data
        for (let i = 0; i < data.Items.length; i++) {
            let item = data.Items[i];
            if (fullPage) {
                new PagedListRow(self, item)
            }
            else {
                let index = self.rows.findIndex(r => r.item.Id == item.Id)
                if (index > -1) {
                    let row = self.rows[index]
                    row.refresh(item)
                    // sorting according data.Items:
                    self.rows.splice(index, 1);
                    self.rows.push(row)
                }
                else {
                    new PagedListRow(self, item)
                }
            }
        }

        // refresh sorting according self.rows
        if (!fullPage) {
            let row: any;
            for (row of self.rows) {
                (<PagedListRow>row).refreshPosition();
            }
        }

        if (fullPage) {
            setTimeout(function () { scrollPosition.restore() }, 0);
        }

        if (self._onPageRefreshed != null) {
            self._onPageRefreshed();
        }
    }

    getData = (page, fullPage = false) => {
        let self = this;
        if (!self.headerRendered) {
            self.renderHeader();
        }

        let sendData: any = namedTuple(self.SendData, [page, self.pageSize, [], [], '', true ]);
        let column: any;
        for (column of self.columns) {
            if ((<PagedListColumn>column).filterEnabled) {
                if (sendData.filterColumns == undefined) {
                    sendData.filterColumns = [];
                }
                if (sendData.filterValues == undefined) {
                    sendData.filterValues = [];
                }
                sendData.filterColumns.push(column.id);
                sendData.filterValues.push(column.getValueFunction());
            }   
        }
        if (self.sorting.columnIndex >= 0) {
            sendData.sortOn = self.columns[self.sorting.columnIndex].id;
            sendData.sortAsc = self.sorting.ascending;
        }
        let onSucces = function(data)  {
            self.render.bind(null, data, fullPage)();
        }
        if (self._server.url == '') {
            self._server.getPageData(sendData, onSucces, self.getDataError)
            //self.refreshStaticServerDelayer.execute(self._server.getPageData.bind(null, sendData, onSucces, self.getDataError));
        }
        else {
            self.refreshDelayer.execute(self._server.getPageData.bind(null, sendData, onSucces, self.getDataError));
        }
    }

    getDataError = (data, errorText) => {
        let self = this;
        console.error("Paged-List for container with id = " + self.containerId + " didn't receive data. Error: " + errorText + ".");
    }

    refresh = (fullPage = false, pageNumber: number = null) => {
        let self = this;
        if (pageNumber != null) {
            self.getData(pageNumber, fullPage)
        }
        else {
            if (self.receiveData == null) {
                self.getData(1, fullPage)
            }
            else {
                self.getData(self.receiveData.CurrentPage, fullPage)
            }
        }
    }

    refreshItem = (item, newItem) => {
        let self = this;
        let r = self.getRow(item)
        if (r != null) {
            r.refresh(newItem)
        }
    }

    getServer = () => {
        let self = this;
        return self._server;
    }

    fakeServer = () => {
        let self = this;
        self._server = new FakeServer();
        return self._server;
    }

    ajaxServer = (url: string) => {
        let self = this;
        self._server = new AjaxServer(url);
        return self._server;
    }

    addRowListener = (event, func, useCapture: boolean = false) => {
        let self = this;
        let newFunction = function(ev) {
            let rowFound: PagedListRow;
            let row: any;
            for (row of self.rows) {
                if (row.element.contains(ev.target)) {
                    rowFound = row;
                    break;
                }
            }

            if (rowFound != null) {
                func(rowFound.item, ev);
            }
        }

        let result = newFunction;
        this.tbody.element.addEventListener(event, result, useCapture);
        return result;
    }

    removeRowListener = (event, func, useCapture: boolean = false) => {
        let self = this;
        self.tbody.element.removeEventListener(event, func, useCapture);
    }
}

export class PagedListStyling {
    pagedList: PagedList;
    _rowStylesFunctions: any = []; // type: List[Callable[[item], string]] # functions which return string with styles as function of item (data of 1 row)
    _rowClassesFunctions: any = []; // type: List[Callable[[item], string]]# functions which return string with style classes as function of item (data of 1 row)
    classExpanded: string = 'cursor glyphicon glyphicon-triangle-bottom';
    classCollapsed: string = 'cursor glyphicon glyphicon-triangle-right';
    classAscending: string = 'glyphicon glyphicon-triangle-top';
    classDescending: string = 'glyphicon glyphicon-triangle-bottom';
    classButtonColumn: string = 'pagedList-buttonColumn';

    constructor(pagedList: PagedList) {
        this.pagedList = pagedList;
    }

    rowStyles = (func) => {
        let self = this;
        if (typeof (func) == 'function') {
            self._rowStylesFunctions.push(func);
        }
        else if (func == null) {
            self._rowStylesFunctions = [];
        }
        else {
            console.error(".rowStyles on Paged-List for container with id " + self.pagedList.containerId + " failed. Passed argument is not a function.");
        }
        return self;
    }

    rowClasses = (func) => {
        let self = this;
        if (typeof (func) == 'function') {
            self._rowClassesFunctions.push(func);
        }
        else if (func == null) {
            self._rowClassesFunctions = [];
        }
        else {
            console.error(".rowClasses on Paged-List for container with id " + self.pagedList.containerId + " failed. Passed argument is not a function.");
        }
        return self;
    }

    tableClass = (styleClass) => {
        let self = this;
        self.pagedList.table.attr('class', styleClass);
        return self;
    }

    tableStyle = (style) => {
        let self = this;
        self.pagedList.table.attr('style', style);
        return self;
    }

    setClassExpanded = (styleClass) => {
        let self = this;
        self.classExpanded = styleClass;
        return self;
    }

    setClassCollapsed = (styleClass) => {
        let self = this;
        self.classCollapsed = styleClass;
        return self;
    }

    setClassAscending = (styleClass) => {
        let self = this;
        self.classAscending = styleClass;
        return self;
    }

    setClassDescending = (styleClass) => {
        let self = this;
        self.classDescending = styleClass;
        return self;
    }

    setClassButtonColumn = (styleClass) => {
        let self = this;
        self.classButtonColumn = styleClass;
        return self;
    }
}