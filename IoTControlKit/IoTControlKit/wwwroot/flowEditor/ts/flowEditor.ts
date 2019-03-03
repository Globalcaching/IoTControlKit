import { ElementWrapper, element, Element, selectElement } from "../../js/shared/elements"
import { LocalStorageWorker } from "../../js/shared/storageHelper"
import * as models from "./models"
import * as dialogs from "./dialogs"

declare var joint: any
declare function uuidv4(): string

export let flowEditorInstance: flowEditor

let _mouseClientX: number = null
let _mouseClientY: number = null

export function createFlowEditor(container, m: models.FlowViewModel) {
    flowEditorInstance = new flowEditor(container, m)
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
    toolBox: ElementWrapper
    canvas: ElementWrapper
    localStorage: LocalStorageWorker = new LocalStorageWorker()
    paper: any
    graph: any

    flows: models.Flow[]
    flowComponents: models.FlowComponent[]
    flowConnectors: models.FlowConnector[]
    deviceProperties: models.DevicePropertyViewModel[]
    activeFlow: models.Flow

    constructor(container, m: models.FlowViewModel) {
        super(container)
        let self = this

        self.flows = m.Flows
        self.flowComponents = m.FlowComponents
        self.flowConnectors = m.FlowConnectors
        self.deviceProperties = m.DeviceProperties

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
        self.toolBox = E('ul')
        let tbItem = E('li').attr('class', 'list-group')
        tbItem
            .attr('class', "list-group-item")
            .attr('style', 'cursor: pointer')
            .attr('draggable', 'true')
            .attr('title', 'Add a trigger that initiates a flow')
            .innerText('Trigger');
        (tbItem.element as HTMLLIElement).ondragstart = function (ev) {
            self.onToolboxItemDragStart(self, ev, "trigger")
        };
        self.toolBox.append(tbItem)

        tbItem = E('li').attr('class', 'list-group')
        tbItem
            .attr('class', "list-group-item")
            .attr('style', 'cursor: pointer')
            .attr('draggable', 'true')
            .attr('title', 'Add a condition that controls a flow')
            .innerText('Condition');
        (tbItem.element as HTMLLIElement).ondragstart = function (ev) {
            self.onToolboxItemDragStart(self, ev, "condition")
        };
        self.toolBox.append(tbItem)

        tbItem = E('li').attr('class', 'list-group')
        tbItem
            .attr('class', "list-group-item")
            .attr('style', 'cursor: pointer')
            .attr('draggable', 'true')
            .attr('title', 'Add an action that should be executed')
            .innerText('Action');
        (tbItem.element as HTMLLIElement).ondragstart = function (ev) {
            self.onToolboxItemDragStart(self, ev, "action")
        };
        self.toolBox.append(tbItem)

        tbItem = E('li').attr('class', 'list-group')
        tbItem
            .attr('class', "list-group-item")
            .attr('style', 'cursor: pointer')
            .attr('draggable', 'true')
            .attr('title', 'Add a pass through')
            .innerText('Pass Through');
        (tbItem.element as HTMLLIElement).ondragstart = function (ev) {
            self.onToolboxItemDragStart(self, ev, "passthrough")
        };
        self.toolBox.append(tbItem)

        self.canvas = E('div').attr('id', 'paper');
        (self.canvas.element as HTMLDivElement).ondragover = function (ev: DragEvent) {
            self.onPaperOnDragOver(ev)
        };
        (self.canvas.element as HTMLDivElement).ondrop = function (ev: DragEvent) {
            self.onPaperOnDrop(ev)
        };
        self.middle.append(self.canvas)
        self.left.append(self.toolBox)
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

        joint.shapes.devs.Model = joint.shapes.basic.Generic.extend(_.extend({}, joint.shapes.basic.PortsModelInterface, {

            markup: '<g class="rotatable"><g class="scalable"><rect class="body"/></g><text class="label"/><g class="inPorts"/><g class="outPorts"/></g>',
            portMarkup: '<g class="port port<%= id %>"><circle class="port-body"/><text class="port-label"/></g>',

            defaults: joint.util.deepSupplement({

                type: 'devs.Model',
                size: { width: 1, height: 1 },

                inPorts: [],
                outPorts: [],

                attrs: {
                    '.': { magnet: false },
                    '.body': {
                        width: 150, height: 250,
                        stroke: 'black'
                    },
                    '.port-body': {
                        r: 10,
                        magnet: true,
                        stroke: 'black'
                    },
                    text: {
                        fill: 'black',
                        'pointer-events': 'none'
                    },
                    '.label': { text: 'Model', 'ref-x': 10, 'ref-y': .2, 'ref': '.body' },

                    // CHANGED: find better positions for port labels
                    '.inPorts .port-label': { y: -20, dx: 4 },
                    '.outPorts .port-label': { y: 25, dx: 4 }
                    //
                }

            }, joint.shapes.basic.Generic.prototype.defaults),

            getPortAttrs: function (portName, index, total, selector, type) {
                var attrs = {};

                var portClass = 'port' + index;
                var portSelector = selector + '>.' + portClass;
                var portLabelSelector = portSelector + '>.port-label';
                var portBodySelector = portSelector + '>.port-body';

                attrs[portLabelSelector] = { text: portName };
                attrs[portBodySelector] = { port: { id: portName || _.uniqueId(type), type: type } };

                // CHANGED: swap x and y ports coordinates ('ref-y' => 'ref-x')
                attrs[portSelector] = { ref: '.body', 'ref-x': (index + 0.5) * (1 / total) };
                // ('ref-dx' => 'ref-dy')
                if (selector === '.outPorts') { attrs[portSelector]['ref-dy'] = 0; }
                //

                return attrs;
            }
        }));

        self.graph = new joint.dia.Graph;
        self.paper = new joint.dia.Paper({
            el: $('#paper'),
            width: 12000, height: 12000, gridSize: 1,
            model: self.graph,
            defaultLink: new joint.dia.Link({
                attrs: { '.marker-target': { d: 'M 10 0 L 0 5 L 10 10 z' } }
            }),
            validateConnection: function (cellViewS, magnetS, cellViewT, magnetT, end, linkView) {
                // Prevent linking from input ports.
                if (magnetS && magnetS.getAttribute('type') === 'input') return false;
                // Prevent linking from output ports to input ports within one element.
                if (cellViewS === cellViewT) return false;
                // Prevent linking to input ports.
                return magnetT && magnetT.getAttribute('type') === 'input';
            },
            // Enable marking available cells & magnets
            markAvailable: true//,
            //snapLinks: { radius: 75 }
        });


        if (self.flows.length == 0) {
            self.createNewFlow(self)
        }
        else {
            self.setActiveFlow(self, self.flows[0])
        }
    }

    createNewFlow(self: flowEditor) {
        let newFlow = new models.Flow()
        newFlow.Guid = uuidv4()
        self.flows.push(newFlow)
        self.setActiveFlow(self, newFlow)
    }

    setActiveFlow(self: flowEditor, flow: models.Flow) {
        if (self.activeFlow != null) {
            //todo
            //remove graph content
        }
        self.activeFlow = flow
        _.forEach(self.flowComponents, function (item: models.FlowComponent) {
            if (item.Guid == self.activeFlow.Guid) {
                //todo
            }
        })
    }


    onToolboxItemDragStart(self: flowEditor, event: DragEvent, toolBoxItem: string) {
        event.stopPropagation()
        event.dataTransfer.effectAllowed = 'move'
        // setup some dummy drag-data to ensure dragging for some browsers:
        event.dataTransfer.setData("text", toolBoxItem)
    }

    onPaperOnDragOver(event: DragEvent) {
        event.preventDefault();
    }

    onPaperOnDrop(event: DragEvent) {
        let self = this
        event.preventDefault();
        var data = event.dataTransfer.getData("text");
        if (data == 'trigger') {
            let dlg = new dialogs.SelectDevicePropertyDialog()
            dlg.show_(dlg, self.deviceProperties, null, function (item) {
            })
        }
        else if (data == 'condition') {
            let dlg = new dialogs.SelectDevicePropertyDialog()
            dlg.show_(dlg, self.deviceProperties, null, function (item) {
            })
        }
        else if (data == 'action') {
            let dlg = new dialogs.SelectDevicePropertyDialog()
            let subset = _.filter(self.deviceProperties, function (item) { return item.Settable })
            dlg.show_(dlg, subset, function (item) {
                return item.Settable == true
            }
            , function (item) {
            })
        }
        else if (data == 'passthrough') {

        }
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