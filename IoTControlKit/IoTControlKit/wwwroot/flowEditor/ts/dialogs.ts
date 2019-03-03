import { ElementWrapper, element, Element, selectElement } from "../../js/shared/elements"
import { PagedList } from "../../js/shared/pagedList"
import { FakeServer } from "../../js/shared/dataServer"
import * as models from "./models"

declare function htmlEncode(text: string): string

export class Dialog {

    title: ElementWrapper
    header: ElementWrapper
    modalDialog: ElementWrapper
    content: ElementWrapper
    buttonArea: ElementWrapper
    main: ElementWrapper

    constructor(title: string) { 
        let self = this
        let E = Element
        self.title = E('h4').attr("class", "modal-title").innerHTML(title)
        self.header = E('div').attr("class", "modal-header").append(
            self.title,
            E('button').attr("type", "button").attr("class", "close").attr("data-dismiss", "modal").attr("aria-label", "Close").append(
                E('span').attr("aria-hidden", "true").innerHTML("&times;"))            
        )
        self.content = E('div').attr("class", "modal-body")
        self.buttonArea = E('div').attr("class", "modal-footer")
        self.modalDialog = E('div').attr("class", "modal-dialog").attr("role", "document").attr('style','max-width:90%')
        self.main = E('div').attr("class", "modal fade").append(
            self.modalDialog.append(
                E('div').attr("class", "modal-content").append(
                    self.header,
                    self.content,
                    self.buttonArea
                )
            )
        )

        // Add event-listener on bootstrap hide, to be able to remove dialog from DOM.
        // (for the case that many dialogs are created during user activity)

        // bootstrap 3:
        $(self.main.element).on('hidden.bs.modal', function () {
            self.main.removeFromParent()
        })
    }

    setTitle = (title: string) => {
        let self = this
        self.title.innerHTML(title)
    }

    defaultHeightOverflow = () => {
        let self = this
        self.content.attr("style", "max-height:600px;overflow-y:auto")
    }

    show(self: Dialog, data: any = null) {
        $(self.main.element).appendTo('body').modal('show')
    }

    hide = () => {
        let self = this
        $(self.main.element).modal('hide')
    }

    addButton = (text: string, styleClass = 'btn btn-primary') => {
        let self = this
        let button = Element('button').attr("type", "button").attr("class", styleClass).innerHTML(text)
        self.buttonArea.append(button)
        return button
    }

    addButtonCancel = () => {
        let self = this
        self.addButton('Cancel', 'btn btn-primary').attr('data-dismiss', 'modal')
    }
}

export class InfoDialog extends Dialog {
    constructor() {
        super("")
        let self = this
        self.addButton('Close', 'btn btn-primary').attr('data-dismiss', 'modal')
        self.defaultHeightOverflow()
    }

    show_(title: string, text: string) {
        let self = this
        self.setTitle(title)
        self.content.innerHTML(text)
        super.show(self as Dialog)
    }
}

export class ConfirmDialog extends Dialog {

    buttonYes: HTMLElement = null
    constructor() {
        super("")
        let self = this
        self.buttonYes = self.addButton("Yes", 'btn btn-danger').element as HTMLElement
        let buttonNo = self.addButton('No', 'btn btn-primary').attr('data-dismiss', 'modal').element as HTMLElement
        buttonNo.onclick = self.cancel
        self.defaultHeightOverflow()
    }

    show_(title: string, text: string, action: () => void) {
        let self = this
        self.setTitle(title)
        self.content.innerHTML(text)
        self.buttonYes.onclick = function () {
            self.hide()
            action()
        }
        super.show(self as Dialog)
    }
    cancel = (ev: MouseEvent) => {
        let self = this
        self.buttonYes.onclick = null
        self.hide()
    }
}

export class ConfirmDeleteDialog extends ConfirmDialog {
    constructor() {
        super()
        this.defaultHeightOverflow()
    }

    show__(text: string, action: () => void) {
        super.show_("Confirm delete", text, action)

    }
}

export class SelectDevicePropertyDialog extends Dialog {

    deviceProperties: models.DevicePropertyViewModel[] = null
    list: PagedList = null
    action: (item: models.DevicePropertyViewModel) => void
    filter: (item: models.DevicePropertyViewModel) => boolean

    constructor() {
        super("")
        let self = this
        // add buttons
        let buttonCancel = self.addButton('Cancel', 'btn btn-primary').element as HTMLElement
        buttonCancel.onclick = function () {
            self.hide()
            self.action(null)
        }
        // add pagedlist
        self.list = new PagedList(self.content.element, null)
        self.list.pageSize = 10
        self.list.getBottomPager().disable()
        self.list.addColumn("ControllerName", "Controller")
            .itemToHtml(function (item) { return htmlEncode(item.ControllerName) })
            .enableFilter(null)
            .enableSort()
        self.list.addColumn("DeviceName", "Device")
            .itemToHtml(function (item) { return htmlEncode(item.DeviceName) })
            .enableFilter(null)
            .enableSort()
        self.list.addColumn("Name", "Property")
            .itemToHtml(function (item) { return htmlEncode(item.Name) })
            .enableFilter(null)
            .enableSort()
        self.list.addButton(1, "Select", "btn btn-primary btn-sm", null)
            .onclick(function (item) {
                self.hide()
                self.action(item)
            })
            .showIf(function (item) {
                if (self.filter == null) {
                    return true
                }
                else {
                    return self.filter(item)
                }
            })
    }

    show_(self: SelectDevicePropertyDialog, deviceProperties: models.DevicePropertyViewModel[], filter: (item: models.DevicePropertyViewModel) => boolean, action: (item: models.DevicePropertyViewModel) => void) {
        let title = "Select device property"
        self.setTitle(title)
        self.action = action
        self.filter = filter
        super.show(self as Dialog)
        self.deviceProperties = deviceProperties
        let fakeServer = self.list.getServer() as FakeServer
        fakeServer.addData(self.deviceProperties)
        self.list.refresh()
    }
}
