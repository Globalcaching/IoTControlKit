import { ElementWrapper, element, Element } from "./elements";
import { PagedList, PagedListStyling } from "pagedList";
import * as functions from "pagedList";
import { LocalStorageWorker } from "../../js/shared/storageHelper"

declare var $: any; // to use jquery (without typing)

let DefaultText = { 'TextTotal': "Total", 'TextFilter': 'Filter' }
let localStorage: LocalStorageWorker = new LocalStorageWorker()

export class PageListStyling {
    pagedList: any;
    classExpanded: string = 'cursor glyphicon glyphicon-triangle-bottom';
    classCollapsed: string = 'cursor glyphicon glyphicon-triangle-right';
    classAscending: string = 'glyphicon glyphicon-triangle-top';
    classDescending: string = 'glyphicon glyphicon-triangle-bottom';
    classButtonColumn: string = 'pagedList-buttonColumn';
    _rowStylesFunctions: any = []; // type: List[Callable[[item], string]] # functions which return string with styles as function of item (data of 1 row)
    _rowClassesFunctions: any = []; // type: List[Callable[[item], string]] # functions which return string with style classes as function of item (data of 1 row)

    constructor(pagedList) {
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

    tableClass = (className: string) => {
        let self = this;
        self.pagedList.table.attr('class', className);
        return self;
    }

    tabledStyle = (style: string) => {
        let self = this;
        self.pagedList.table.attr('style', style);
        return self;
    }

    setClassExpanded = (className: string) => {
        let self = this;
        self.classExpanded = className;
        return self;
    }

    setClassCollapsed = (className: string) => {
        let self = this;
        self.classCollapsed = className;
        return self;
    }

    setClassAscending = (className: string) => {
        let self = this;
        self.classAscending = className;
        return self;
    }

    setClassDescending = (className: string) => {
        let self = this;
        self.classDescending = className;
        return self;
    }

    setClassButtonColumn = (className: string) => {
        let self = this;
        self.classButtonColumn = className;
        return self;
    }
}

export class PagedListRow extends ElementWrapper {
    pagedList: PagedList;
    item: any;
    refreshFunctions: any = [];
    elementsToRemove: ElementWrapper[] = [] // List[ElementWrapper] # elements to remove on refresh
    subRows: any = [] // List[PagedListSubRow]

    constructor(pagedList: PagedList, item) {
        super(element('tr'));

        let self = this

        this.pagedList = pagedList;
        this.item = item;
        if (this.pagedList.onDragStartFunction != null) {
            this.attr("draggable", "true")
            this.element.ondragstart = function (event) {
                self.pagedList.onDragStartFunction(event, self.item)
            }
        }

        this.addToPagedList();
        this.render();
        this.refresh(this.item);
    }

    addToPagedList = () => {
        let self = this;
        self.pagedList.rows.push(self);
        self.pagedList.tbody.append(self);
    }

    remove = () => {
        let self = this;
        let index: number = self.pagedList.rows.indexOf(self);
        self.pagedList.rows.splice(index, 1);
        self.removeFromParent();
        while (self.subRows.length > 0) {
            self.subRows[0].remove();
        }
    }

    lengthInRows = () => {
        let self = this;
        return 1 + self.subRows.length;
    }

    positionInRows = () => {
        let self = this;
        let result: number = 0;
        for (let i = 0; i < self.pagedList.rows.length; i++) {
            let row = self.pagedList.rows[i];
            if (self == row) {
                break;
            }
            else {
                result += row.lengthInRows();
            }
        }
        return result;
    }

    positionInParent = () => {
        let self = this;
        return self.indexInParent();
    }

    render = () => {
        let column: any;
        for (column of this.pagedList.columns) {
            let col: PagedListColumn = <PagedListColumn>column;
            let td = Element('td');
            this.append(td);
            if (col.isVisible) {
                if (col.classesRows.length > 0) {
                    td.attr('class', col.classesRows.join(' '));
                }
                if (col.stylesRows.length > 0) {
                    td.attr('style', col.stylesRows.join(' '));
                }
                if (column.onExpandItemFunction != null) {
                    td.attr('style', 'white-space: nowrap;');
                    let buttonExpand: any = new ElementWrapper(element('span'));
                    buttonExpand.isExpanded = false;
                    let self = this;
                    let clsName = function (isExpanded: boolean) {
                        return isExpanded ? self.pagedList.styling.classExpanded : self.pagedList.styling.classCollapsed;
                    }
                    buttonExpand.element.className = clsName(buttonExpand.isExpanded);
                    let toggleExpand = function (buttonExpand: any, expandFunction: any, rowBefore: PagedListRow, event: any) {
                        buttonExpand.isExpanded = !buttonExpand.isExpanded;
                        buttonExpand.element.className = clsName(buttonExpand.isExpanded);
                        if (buttonExpand.isExpanded) {
                            buttonExpand.row = new PagedListSubRow(self.pagedList, rowBefore, expandFunction());
                            self.subRows.push(buttonExpand.row);
                        }
                        else {
                            buttonExpand.row.remove();
                            buttonExpand.row = null;
                        }
                        event.stopPropagation();
                    }
                    td.append(buttonExpand);

                    let refreshFunction = function (buttonExpand: ElementWrapper, toggleExpand, column, item) {
                        buttonExpand.element.onclick = toggleExpand.bind(null, buttonExpand, column.onExpandItemFunction.bind(null, item), self);
                    }
                    this.refreshFunctions.push(refreshFunction.bind(null, buttonExpand, toggleExpand, column));
                }

                if (column.itemToHtmlFunction != null) {
                    let htmlSpan = element('span');
                    td.element.appendChild(htmlSpan)
                    let refreshFunction = function (span: HTMLElement, column, item) {
                        span.innerHTML = column.itemToHtmlFunction(item);
                    }
                    this.refreshFunctions.push(refreshFunction.bind(null, htmlSpan, column));
                }
                if (column.itemToElementFunction != null) {
                    let self = this;
                    let refreshFunction = function (td: ElementWrapper, column, item) {
                        let columnElement = new ElementWrapper(column.itemToElementFunction(item));
                        self.elementsToRemove.push(columnElement);
                        td.append(columnElement);
                    }
                    this.refreshFunctions.push(refreshFunction.bind(null, td, column))
                }
            }
        }
        this.refreshFunctions.push(this.renderButtons)
    }

    renderButtons = (item) => {
        let self = this;
        if (self.pagedList.buttons.length > 0) {
            let td: ElementWrapper = null;
            if (self.pagedList.mergeButtonColumns) {
                td = Element('td').attr('class', self.pagedList.styling.classButtonColumn);
                self.append(td);
                self.elementsToRemove.push(td);
            }

            let button: any;
            for (button of self.pagedList.buttons) {
                let btn: PagedListButton = <PagedListButton>button;
                if (!self.pagedList.mergeButtonColumns) {
                    td = Element('td').attr("class", self.pagedList.styling.classButtonColumn);
                    self.append(td);
                    self.elementsToRemove.push(td);
                }

                if (btn._showif == null || btn._showif(item)) {
                    let buttonElement = button.getElement(item);
                    td.append(buttonElement);
                }
            }
        }
    }

    refresh = (item) => {
        let self = this;
        if (item != null) {
            self.item = item;
        }
        let style: string = '';
        let func: any;
        for (func of self.pagedList.styling._rowStylesFunctions) {
            style += func(self.item) + ' ';
        }
        self.attr('style', style);

        let styleClass: string = '';
        for (func of self.pagedList.styling._rowClassesFunctions) {
            styleClass += func(self.item) + ' ';
        }
        self.attr('class', styleClass);

        //if (this.pagedList._rowClassesFunction != null) {
        //    this.attr('class', this.pagedList._rowClassesFunction(self.item));
        //}

        let element: any;
        for (element of self.elementsToRemove) {
            element.removeFromParent();
        }
        this.elementsToRemove = [];
        for (func of self.refreshFunctions) {
            func(self.item);
        }
    }

    // make sorting of elements according sorting in pagedList.rows
    refreshPosition = () => {
        let self = this;
        let positionInParent: number = self.positionInParent();
        let positionInRows: number = self.positionInRows();

        if (positionInParent != positionInRows) {
            self.removeFromParent();
            let children = self.pagedList.tbody.children();
            if (children.length <= positionInRows) {
                self.pagedList.tbody.append(this);
                let subRow: any;
                for (subRow of self.subRows) {
                    self.pagedList.tbody.append(<PagedListRow>subRow);
                }
            }
            else {
                let existingNode = children[positionInRows];
                self.pagedList.tbody.insertBefore(this, existingNode)
                let subRow: any;
                for (subRow of self.subRows.reverse()) {
                    self.pagedList.tbody.insertBefore(subRow, existingNode);
                }
            }
        }
    }
}

export class PagedListSubRow extends ElementWrapper {
    pagedList: PagedList = null;
    rowBefore = null;
    elementToShow = null;

    constructor(pagedList: PagedList, rowBefore: PagedListRow, elementToShow) {
        super(element('tr'));
        this.pagedList = pagedList;
        this.rowBefore = rowBefore;
        this.elementToShow = elementToShow;
        this.render();
    }

    render = () => {
        let self = this;
        let td: any = Element('td');
        td.element.className = 'subPagedListTd';
        self.append(td);
        td.element.colSpan = this.pagedList.columns.length + self.pagedList.buttons.length;

        let table = Element('table');
        table.element.className = 'subPagedListTable';
        td.append(table);

        let subRow = Element('tr');
        table.append(subRow)

        let td1 = Element('td');
        td1.element.className = 'subPagedListCell1';
        subRow.append(td1);

        let td2 = Element('td');
        td2.element.className = 'subPagedListCell2';
        subRow.append(td2);
        td2.element.appendChild(self.elementToShow);

        self.rowBefore.element.parentNode.insertBefore(self.element, self.rowBefore.element.nextSibling);
        $(self.element).hide().fadeIn();
    }

    remove = () => {
        let self = this;
        self.removeFromParent()
        let index = self.rowBefore.subRows.indexOf(self);
        if (index > -1) {
            self.rowBefore.subRows.splice(index, 1);
        }
    }
}

// Button for PagedList.Buttons are shown for each row.
// Function onclick can be added which will be called with row- item as argument.
// Function showIf can be added to 
export class PagedListButton {
    id: number;
    name: string;
    styleClass: string;
    columnStyleAttribute: string
    _onclick = null;
    _showif = null;

    constructor(id: number, name: string, styleClass: string, columnStyleAttribute: string = null) {
        this.id = id;
        this.name = name;
        this.styleClass = styleClass
        this.columnStyleAttribute = columnStyleAttribute
    }

    onclick = (functionOnClick) => {
        let self = this;
        if (self._onclick != null) {
            console.error(".onclick on button " + self.id + " failed. Button has already an onclick-function.");
        }
        if (typeof (functionOnClick) != "function") {
            console.error(".onclick on button " + self.id + " failed. Passed argument is not a function.")
        }
        self._onclick = functionOnClick;
        return self;
    }

    onClick = (functionOnClick) => {
        let self = this;
        return self.onclick(functionOnClick);
    }

    showIf = (functionShowIf) => {
        let self = this;
        if (typeof (functionShowIf) != 'function') {
            console.error(".showIf on button " + self.id + " failed. Passed argument is not a function.");
        }

        self._showif = functionShowIf;
        return self;
    }

    showif = (functionShowIf) => {
        let self = this;
        return self.showIf(functionShowIf);
    }

    getElement = (item) => {
        let self = this;
        let result = Element('button');
        result.element.innerHTML = self.name;
        result.attr('class', self.styleClass);
        if (self._onclick != null) {
            result.element.onclick = self._onclick.bind(null, item);
        }

        return result;
    }

    copy = () => {
        let self = this;
        let result = new PagedListButton(self.id, self.name, self.styleClass, self.columnStyleAttribute);
        result._onclick = self._onclick;
        result._showif = self._showif;
        return result;
    }
}

export class PagedListColumn {
    id: string;
    header: string;
    sortable: boolean = false;
    filterEnabled: boolean = false;
    filterItems: any = null;
    itemToHtmlFunction: any = null; // function which accepts an item (the data for 1 row) and returns html string, which is the content of the table cell.
    itemToElementFunction: any = null; // function which accepts an item (the data for 1 row) and returns an html element, which is the content of the table cell.
    onExpandItemFunction: any = null; // function which accepts an item (the data for 1 row) and returns an html element, which must be shown below row when expand is clicked
    span: ElementWrapper = null; // element to show in header, and to click on for toggling sort
    toggleFigure: ElementWrapper = null; // figure (html element) to show sorting (arrow)
    showHideFigure: ElementWrapper = null; 
    getValueFunction: any = null; // function which returns the value of the sorting (input or select) element
    classesHeader: string[] = []; // html style classes to use for the header
    stylesHeader: string[] = []; // html styles to use for the header
    classesHeaderSpan: string[] = []; // html style classes to use for the span in the header
    stylesHeaderSpan: string[] = []; // html styles to use for the span in the header
    classesRows: string[] = []; // html style classes to use for the data rows (for all rows, item independent)
    stylesRows: string[] = []; // html styles to use for the data rows (for all rows, item independent)
    FilterItem: string[] = ['Text', 'Value'];
    filterInputElement: ElementWrapper = null
    isVisible: boolean = true
    pagedList: any

    constructor(id: string, header: string) {
        this.id = id;
        this.header = header;
        this.classesHeader.push("pagedListColumnHeader")
        this.classesRows.push("pagedListColumnRow")
    }

    addClassHeader = (className: string) => {
        let self = this;
        self.classesHeader.push(className);
        return self;
    }

    addStyleHeader = (style: string) => {
        let self = this;
        self.stylesHeader.push(style);
        return self;
    }

    addClassHeaderSpan = (className: string) => {
        this.classesHeaderSpan.push(className);
        return this;
    }

    addStyleHeaderSpan = (style: string) => {
        let self = this;
        self.stylesHeaderSpan.push(style);
        return self;
    }

    addClassRows = (className: string) => {
        let self = this;
        self.classesRows.push(className);
        return self;
    }

    addStyleRows = (style: string) => {
        let self = this;
        self.stylesRows.push(style);
        return self;
    }

    addClass = (className: string) => {
        let self = this;
        self.addClassHeader(className);
        self.addClassRows(className);
        return self
    }

    addStyle = (style: string) => {
        let self = this;
        self.addStyleHeader(style);
        self.addStyleRows(style);
        return self
    }

    enableSort = () => {
        let self = this;
        self.sortable = true;
        return self;
    }

    enableFilter = (items) => {
        let self = this;
        self.filterEnabled = true;
        if (items != null) {
            if (!items.length) {
                console.error(".enableFilter on column " + self.id + " failed. Argument must be an array or list.");
            }

            for (let item of items) {
                if (!functions.containsAll(item, self.FilterItem)) {
                    console.error(".enableFilter on column " + self.header + " failed. Each FilterItem must contain all fields: " + self.filterItems);
                }
            }

            self.filterItems = items;
        }
        return self;
    }

    itemToHtml = (itemToHtmlFunction) => {
        let self = this;
        if (typeof (itemToHtmlFunction) != 'function') {
            console.error(".itemToHtml on column " + self.header + " failed. Passed argument is not a function.");
        }
        self.itemToHtmlFunction = itemToHtmlFunction;
        return self;
    }

    itemToElement = (itemToEelementFunction) => {
        let self = this;
        if (typeof (itemToEelementFunction) != 'function') {
            console.error(".itemToelement on column " + self.header + " failed. Passed argument is not a function.");
        }
        self.itemToElementFunction = itemToEelementFunction;
        return self;
    }

    onExpandItem = (onExpandItem) => {
        let self = this;
        if (typeof (onExpandItem) != 'function') {
            console.error(".onExpandItem on column " + self.header + " failed. Passed argument is not a function.");
        }
        self.onExpandItemFunction = onExpandItem;
        return self;
    }

    clearFilter = () => {
        let self = this;
        if (self.filterInputElement != null) {
            if (self.filterItems == null || self.filterItems.length == 0) {
                (self.filterInputElement.element as HTMLInputElement).value = ''
            }
            else {
                (self.filterInputElement.element as HTMLInputElement).value = self.filterItems[0].Value
            }
        }
    }

    updateShowHideFilter = () => {
        let self = this;
        if (self.isVisible) {
            self.showHideFigure.attr('class', 'glyphicon glyphicon-eye-close')
            self.showHideFigure.attr('style', 'margin-left:2px;font-size:0.75em;')
            self.span.element.innerHTML = self.header
            if (self.filterInputElement != null) {
                self.filterInputElement.element.style.display = 'inline'
            }
        }
        else {
            self.showHideFigure.attr('class', 'glyphicon glyphicon-eye-open')
            self.showHideFigure.attr('style', 'margin-left:0px;font-size:0.75em;')
            self.span.element.innerHTML = ''
            if (self.filterInputElement != null) {
                self.filterInputElement.element.style.display = 'none'
            }
        }
    }

    getElements = (pagedList) => {
        let self = this;
        self.pagedList = pagedList
        let result: ElementWrapper[] = [];
        if (self.span == null) {
            self.span = Element('span');
            result.push(self.span);
            self.span.element.innerHTML = self.header;
            if (self.classesHeaderSpan.length > 0) {
                self.span.attr('class', self.classesHeaderSpan.join(" "));
            }
            if (self.stylesHeaderSpan.length > 0) {
                self.span.attr('style', self.stylesHeaderSpan.join(" "));
            }
            if (self.sortable) {
                self.span.attr('role', 'button');
                self.toggleFigure = Element('i');
                result.push(self.toggleFigure)
            }
            if (pagedList.enableShowHideColumns) {
                self.showHideFigure = Element('i')
                self.showHideFigure.attr('title', self.header)
                self.showHideFigure.element.onclick = function () {
                    self.isVisible = !self.isVisible
                    if (pagedList.filterId != null && pagedList.filterId != '') {
                        localStorage.add(`viscol_${pagedList.filterId}_${self.id}`, self.isVisible ? '1' : '0')
                    }
                    self.updateShowHideFilter()
                    self.pagedList.refresh(true)
                }
                if (pagedList.filterId != null && pagedList.filterId != '') {
                    let isVisStorage = localStorage.get(`viscol_${pagedList.filterId}_${self.id}`)
                    if (isVisStorage == '0') {
                        self.isVisible = false
                    }
                    else {
                        self.isVisible = true
                    }
                }
                result.push(self.showHideFigure)
            }
            if (self.filterEnabled) {
                result.push(Element('br'));
                let getValue = function (element) {
                    return $(element).val();
                }

                let storedValue: string = ''
                if (pagedList.filterId != '') {
                    let filterColumnValue = localStorage.get(`filterColumnValue_${pagedList.filterId}_${self.id}`)
                    if (filterColumnValue != null) {
                        if (filterColumnValue != '') {
                            storedValue = filterColumnValue.toString()
                        }
                    }
                }

                if (self.filterItems == null || self.filterItems.length == 0) {
                    let divel = Element('div').attr('width', '100%')
                    let input = Element('input', null, 'clearable')
                        .attr('style', 'min-width: 40px;')
                        .attr('width', '100%')
                        .attr('value', (storedValue != '') ? storedValue : '')
                        .attr('placeholder', DefaultText.TextFilter)
                    divel.append(input)
                    self.filterInputElement = input
                    result.push(divel)
                    self.getValueFunction = getValue.bind(null, input.element)
                    $(input.element).bind('input', pagedList.getData.bind(null, 1, true))
                    input.element.addEventListener("input", function (event) {
                        $(this)[tog(this.value)]('x');
                        if (pagedList.filterId != '') {
                            localStorage.add(`filterColumnValue_${pagedList.filterId}_${self.id}`, this.value)
                        }
                    })
                    input.element.addEventListener("mousemove", function (event) {
                        $(this)[tog(this.offsetWidth - 18 < event.clientX - this.getBoundingClientRect().left)]('onX');   
                    })
                    input.element.addEventListener("click", function (event) {
                        if ($(this).hasClass('onX')) {
                            event.preventDefault();
                            $(this).removeClass('x onX').val('').change();
                            if (pagedList.element.id != '') {
                                localStorage.remove(`filterColumnValue_${pagedList.filterId}_${self.id}`)
                            }
                            pagedList.refresh()
                        }
                    })

                    if (pagedList.filterId != '') {
                        $(input.element)[tog(storedValue)]('x');
                    }
                }
                else {
                    let select = Element('select').attr('width', '100%');
                    self.filterInputElement = select
                    result.push(select)
                    let filterItemToOption = function (filterItem) {
                        let r = Element('option').attr('value', filterItem.Value);
                        r.element.innerHTML = filterItem.Text;
                        return r;
                    }
                    let options: ElementWrapper[] = self.filterItems.map(filterItemToOption);
                    options[0].attr('selected', 'selected');
                    let option: any;
                    for (option of options) {
                        select.append(option);
                    }
                    self.getValueFunction = getValue.bind(null, select.element)
                    $(select.element).change(pagedList.getData.bind(null, 1, true))
                }
            }

            if (pagedList.enableShowHideColumns) {
                self.updateShowHideFilter()
            }

        }
        else {
            console.error("Column '" + self.id + "'.getElements() is called twice (for Paged-List in container with id " + pagedList.containerId + ").");
        }

        return result;
    }
}

function tog(v) { return v ? 'addClass' : 'removeClass'; }

export class Pager extends ElementWrapper {
    pagedList: PagedList = null;
    table: ElementWrapper = Element('table').attr('width', '100%');
    tr: ElementWrapper = Element('tr');
    td_left: ElementWrapper = Element('td');
    td_right: ElementWrapper = Element('td').attr('align', 'right');;
    numberList: ElementWrapper = Element('ul');
    textNodeTotal = document.createTextNode(DefaultText.TextTotal + " : ");
    count: ElementWrapper = Element('span');
    _hideCount: boolean = false;
    disabled: boolean = false;
    autoDisabled: boolean = false;// set to True if pagedList has only 1 page
    activeClass: string = 'active'

    constructor(container, pagedList: PagedList) {
        super(container);
        this.pagedList = pagedList;
        this.table.attr('style', 'height: 80px;');
        this.append(this.table);
        this.table.append(this.tr);
        this.tr.append(this.td_left);
        this.tr.append(this.td_right);
        this.td_left.append(this.numberList);
        this.td_right.element.appendChild(this.textNodeTotal);
        this.td_right.append(this.count);

        this.setPaginationClass('pagination');
        this.setCountClass('label label-default');
    }

    getTable = () => {
        let self = this;
        return self.table.element;
    }

    setPaginationClass = (className) => {
        let self = this;
        self.numberList.attr('class', className);
        return self;
    }

    setActiveClass = (className) => {
        let self = this;
        self.activeClass = className;
        return self;
    }

    setCountClass = (className) => {
        let self = this;
        self.count.attr('class', className);
        return self;
    }

    hideCount = () => { // to hide the counter (Total: ...)
        let self = this;
        self._hideCount = true;
        self.td_right.element.removeChild(self.textNodeTotal);
        self.td_right.element.removeChild(self.count.element);
        return self;
    }

    disable = () => {
        let self = this;
        self.disabled = true;
        self.attr('style', 'display: none;');
        return self;
    }

    enable = () => {
        let self = this;
        self.disabled = false;
        self.attr('style', 'display: block;');
        return self;
    }

    refresh = (currentPage: number, pageCount: number, itemCount: number) => {
        let self = this;
        if (self.disabled && self.autoDisabled && pageCount > 1) {
            self.enable();
            self.autoDisabled = false;
        }

        if (!self.disabled) {
            self.numberList.removeChilds();
            if (!self._hideCount) {
                self.count.element.innerHTML = itemCount.toString();
            }

            let maxPages = 5;
            let startPage = (Math.floor(currentPage / maxPages) * maxPages) + 1;
            if (currentPage % maxPages == 0) {
                startPage -= maxPages;
            }
            if (currentPage > maxPages) {
                self.addNumber(1, "<<")
                self.addNumber(startPage - 1, "<")
            }
            if (pageCount > 1) {
                let i = startPage;
                while (i < startPage + maxPages && i <= pageCount) {
                    let li: ElementWrapper = self.addNumber(i);
                    if (currentPage == i) {
                        li.attr('class', 'page-item active');
                    }
                    else {
                        li.attr('class', 'page-item');
                    }
                    i++;
                }
                if (startPage + maxPages <= pageCount) {
                    self.addNumber(startPage + maxPages, ">");
                    self.addNumber(pageCount, ">>");
                }
            }
        }
    }

    addNumber = (number: number, text: string = null) => {
        let self = this;
        let li = Element('li');
        self.numberList.append(li);
        let a = Element('a').attr('href', '#');
        a.attr('class', 'page-link');
        li.append(a);

        if (text != null) {
            a.element.innerHTML = text;
        }
        else {
            a.element.innerHTML = number.toString();
        }
        a.element.onclick = self.pagedList.getData.bind(null, number, true);
        return li;
    }
}