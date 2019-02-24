import { ElementWrapper, element, Element, selectElement } from "./elements"
import { LocalStorageWorker } from "./storageHelper"

export let flowEditorInstance: flowEditor

let _mouseClientX: number = null
let _mouseClientY: number = null

export function createFlowEditor(container) {
    flowEditorInstance = new flowEditor(container)
    return flowEditorInstance
}

export class flowEditor extends ElementWrapper{
    containerId: string = ''
    flowEditorElement: ElementWrapper
    left: ElementWrapper
    splitterLeft: ElementWrapper
    middle: ElementWrapper
    right: ElementWrapper
    splitterRight: ElementWrapper
    localStorage: LocalStorageWorker = new LocalStorageWorker()

    constructor(container) {
        super(container)
        let self = this
        let E = Element
        if (Object.prototype.toString.call(container) == '[object String]') { // container is an id
            self.containerId = container;
            container = document.querySelector(this.containerId);
            if (container == null) {
                console.error("flowEditor cannot find container with id " + this.containerId);
            }
            super(container);
        }
        else {
            self.containerId = (container.id != '') ? container.id : 'Unknown id';
        }

        self.flowEditorElement = E('div')
        self.flowEditorElement.attr("id", "flowEditorArea")
        self.left = E('div').attr("class", "left")
        self.splitterLeft = E('div').attr("class", "splitter splitterLeft")
        self.middle = E('div').attr("class", "middle")
        self.right = E('div').attr("class", "right")
        self.splitterRight = E('div').attr("class", "splitter splitterRight")
        self.flowEditorElement.append(
            self.left,
            self.splitterLeft,
            self.middle,
            self.right, // before splitter, since floating from right
            self.splitterRight)
        let storedWidth = self.localStorage.get('splitterLeftWidth')
        if (storedWidth != null) {
            self.left.width(parseInt(storedWidth))
        }
        storedWidth = self.localStorage.get('splitterRightWidth')
        if (storedWidth != null) {
            self.right.width(parseInt(storedWidth))
        }
        self.enableSplitter(true)
        self.enableSplitter(false)

        self.append(self.flowEditorElement)

        document.onmousemove = function (event: MouseEvent) {
            _mouseClientX = event.clientX;
            _mouseClientY = event.clientY;
        }
        document.ondragover = function (event: MouseEvent) {
            _mouseClientX = event.clientX;
            _mouseClientY = event.clientY;
        }
        document.onresize = function (event: UIEvent) {
            self.onResize(self)
        }
        self.onResize(self)
    }

    onResize(self: flowEditor) {
        let parentWidth = Math.floor(self.flowEditorElement.element.parentElement.clientWidth)
        let wLeft = Math.ceil(self.left.element.offsetWidth + self.splitterLeft.element.offsetWidth + 0.5)
        let wRight = Math.ceil(self.splitterRight.element.offsetWidth + self.right.element.offsetWidth + 0.5)
        let wMiddle = parentWidth - wLeft - wRight
        // check if left and right do not overlap
        if (wMiddle < 10) {
            if (wLeft > wRight) {
                self.left.width(self.left.element.clientWidth + (wMiddle - 10))
            } else {
                self.right.width(self.right.element.clientWidth + (wMiddle - 10))
            }
            wMiddle = 10
        }
        self.middle.width(wMiddle)
    }

    enableSplitter(left: boolean, minWidth: number = 5.0) {
        let self = this
        let pos = { 'startMouseX': null, 'startValue': null, 'max': null, 'min': minWidth }
        let emptyElement = Element('span').attr('style', 'position: absolute; display: none; top: 0; left: 0; width: 0; height: 0;').element
        let ondragstart = function (event) {
            event.dataTransfer.setData("text/plain", "need it for FireFox");
            event.dataTransfer.setDragImage(emptyElement, 0, 0)
            let splitterWidth = self.splitterLeft.width()
            document.body.appendChild(emptyElement)
            //pos.startMouseX = event.clientX
            pos.startMouseX = _mouseClientX
            if (left) {
                pos.startValue = self.left.width()
                pos.max = self.element.parentElement.clientWidth - self.right.width() - pos.min - 2.0 * splitterWidth
            } else {
                pos.startValue = self.right.width()
                pos.max = self.element.parentElement.clientWidth - self.left.width() - pos.min - 2.0 * splitterWidth
            }
        }
        let ondrag = function (event) {
            //let clientX = event.clientX
            let clientX = _mouseClientX
            if (clientX > 0) { // last event position is negative, unknown reason
                let dx = clientX - pos.startMouseX
                if (left) {
                    let newWidth = Math.max(Math.min(pos.startValue + dx, pos.max), pos.min)
                    self.left.width(newWidth)
                    self.localStorage.add('splitterLeftWidth', newWidth.toString())
                } else {
                    let newWidth = Math.max(Math.min(pos.startValue - dx, pos.max), pos.min)
                    self.right.width(newWidth)
                    self.localStorage.add('splitterRightWidth', newWidth.toString())
                }
                self.onResize(self)
            }
        }
        let ondragend = function (event) {
            document.body.removeChild(emptyElement)
        }
        let splitter: ElementWrapper = null;
        if (left) {
            splitter = self.splitterLeft
        } else {
            splitter = self.splitterRight
        }
        splitter.attr('draggable', true)
        splitter.element.ondragstart = ondragstart
        splitter.element.ondrag = ondrag
        splitter.element.ondragend = ondragend
    }

}