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
    properties: ElementWrapper
    valueInput: ElementWrapper
    localStorage: LocalStorageWorker = new LocalStorageWorker()
    paper: any
    graph: any
    uniqueId: number = -1
    activeCellView: any = null
    ignoreInputChange: boolean = false

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

        self.properties = E('div')
        let btn = E('button')
        btn.attr('class', 'btn btn-primary col-md-12')
        btn.innerText('Delete');
        (btn.element as HTMLButtonElement).onclick = function () {
            if (self.activeCellView != null) {
                self.deleteComponent(self, self.activeCellView)
            }
        }
        self.properties.append(btn)
        let div = E('div')
        self.properties.append(div)
        let div2 = E('div')
        div2.attr('class', 'col-md-12')
        div2.innerText('Value')
        div.append(div2)
        div2 = E('div')
        div2.attr('class', 'col-md-12')
        div.append(div2)
        self.valueInput = E('input')
        self.valueInput.attr('style','width:100%;')
        div2.append(self.valueInput)
        self.right.append(self.properties);

        (self.valueInput.element as HTMLInputElement).oninput = function () {
            if (!self.ignoreInputChange) {
                self.setActiveCellViewValue(self, $(self.valueInput.element).val())
            }
        }

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
                if (magnetS && magnetS.getAttribute('port-group') === 'in') return false;
                // Prevent linking from output ports to input ports within one element.
                if (cellViewS === cellViewT) return false;
                // Prevent linking to input ports.
                return magnetT && magnetT.getAttribute('port-group') === 'in';
            },
            // Enable marking available cells & magnets
            markAvailable: true,
            //snapLinks: { radius: 75 }
            defaultRouter: {
                name: 'metro',
                args: {
                    padding: 10
                }
            }
        })

        self.paper.on('cell:pointerclick', function (cellView, evt, x, y) {
            self.setActiveCellView(self, cellView);
        })
        self.paper.on('blank:pointerclick', function (evt, x, y) {
            self.setActiveCellView(self, null);
        })
        self.paper.on('link:connect', function (connection, z, c, v, b) {
            let newCon = new models.FlowConnector()
            newCon.Guid = uuidv4()
            newCon.Id = self.getUniqueId(self)
            newCon.SourceFlowComponentd = connection.sourceView.model.attr('flowComponentId')
            newCon.TargetFlowComponentd = connection.targetView.model.attr('flowComponentId')
            newCon.SourcePort = connection.sourceMagnet.getAttribute('port')
            connection.model.attr('flowConnectorId', newCon.Id.toString())
            self.flowConnectors.push(newCon)
        })
        self.graph.on('remove', function (cell) {
            if (cell.isLink()) {
                let connectorId = cell.attributes.attrs.flowConnectorId
                _.remove(self.flowConnectors, function (o: models.FlowConnector) { return o.Id == connectorId })
            }
        })
        self.graph.on('change:position', function (cell) {
            if (!cell.isLink()) {
                let x = cell.changed.position.x
                let y = cell.changed.position.y
                let componentId = cell.attributes.attrs.flowComponentId
                let flowComponent = _.find(self.flowComponents, function (o) { return o.Id == componentId })
                flowComponent.PositionX = x
                flowComponent.PositionY = y
            } 
        })

        if (self.flows.length == 0) {
            self.createNewFlow(self)
        }
        else {
            self.setActiveFlow(self, self.flows[0])
        }

        self.setActiveCellView(self, null)
    }

    deleteComponent(self: flowEditor, activeCell) {
        if (activeCell == self.activeCellView) {
            self.setActiveCellView(self, null)
        }
        let componentId = activeCell.model.attr('flowComponentId')
        _.remove(self.flowConnectors, function (o: models.FlowConnector) { return o.SourceFlowComponentd == componentId || o.TargetFlowComponentd == componentId })
        _.remove(self.flowComponents, function (o: models.FlowComponent) { return o.Id == componentId })

        let links = self.graph.getConnectedLinks(activeCell.model);
        for (var k = 0; k < links.length; k++) {
            links[k].remove();
        }
        activeCell.model.remove();
    }

    setActiveCellViewValue(self: flowEditor, v: string) {
        if (self.activeCellView != null) {
            let componentId = self.activeCellView.model.attr('flowComponentId')
            let flowComponent = _.find(self.flowComponents, function (o) { return o.Id == componentId })
            flowComponent.Value = v
            let deviceProperty: models.DevicePropertyViewModel = null
            if (flowComponent.DevicePropertyId != null) {
                deviceProperty = _.find(self.deviceProperties, function (o: models.DevicePropertyViewModel) { return o.Id == flowComponent.DevicePropertyId })
            }
            self.activeCellView.model.attr('.label/text', self.getInfoTextFromProperty(self, flowComponent, deviceProperty))
        }
    }

    setActiveCellView(self: flowEditor, activeCell) {
        self.ignoreInputChange = true
        if (self.activeCellView != null) {
            self.activeCellView.unhighlight();
            self.activeCellView = null;
        }
        self.activeCellView = activeCell;
        if (self.activeCellView != null) {
            let componentId = activeCell.model.attr('flowComponentId')
            let flowComponent = _.find(self.flowComponents, function (o) { return o.Id == componentId })
            let deviceProperty: models.DevicePropertyViewModel = null
            if (flowComponent.DevicePropertyId != null) {
                deviceProperty = _.find(self.deviceProperties, function (o: models.DevicePropertyViewModel) { return o.Id == flowComponent.DevicePropertyId })
            }
            if (deviceProperty == null) {
                $(self.valueInput.element).hide()
            }
            else {
                switch (deviceProperty.DataType) {
                    case 'color':
                        //todo
                        break
                    case 'enum':
                        //todo
                        break
                    default:
                        self.valueInput.attr('type','text')
                        $(self.valueInput.element).show()
                        $(self.valueInput.element).val(flowComponent.Value)
                        break
                }
            }

            self.activeCellView.highlight();
            $(self.properties.element).show()
        }
        else {
            $(self.properties.element).hide()
        }
        self.ignoreInputChange = false
    }

    getUniqueId(self: flowEditor) {
        self.uniqueId--
        return self.uniqueId
    }

    createNewFlow(self: flowEditor) {
        let newFlow = new models.Flow()
        newFlow.Id = self.getUniqueId(self)
        newFlow.Guid = uuidv4()
        newFlow.Name = 'New flow';
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
            if (item.FlowId == self.activeFlow.Id) {
                //todo
            }
        })
    }

    getInfoTextFromProperty(self: flowEditor, item: models.FlowComponent, deviceProperty: models.DevicePropertyViewModel) {
        let infoText = ''
        if (deviceProperty != null) {
            infoText = 'Controller: ' + deviceProperty.ControllerName + '\n';
            infoText += 'Device: ' + deviceProperty.DeviceName + '\n';
            infoText += 'Property: ';
            if (deviceProperty.Name.startsWith(deviceProperty.DeviceName)) {
                infoText += deviceProperty.Name.substr(deviceProperty.DeviceName.length)
            }
            else {
                infoText += deviceProperty.Name
            }
            if (deviceProperty.DataType == 'float') {
                infoText += '\nValue (' + deviceProperty.Format + '): ' + item.Value;
            }
        }
        else if (item.Type == "PassThrough") {
            infoText = "Pass Through"
        }
        return infoText
    }

    drawComponent(self: flowEditor, item: models.FlowComponent) {
        let deviceProperty: models.DevicePropertyViewModel = _.find(self.deviceProperties, function (o: models.DevicePropertyViewModel) { return o.Id == item.DevicePropertyId })
        let pos = { x: item.PositionX, y: item.PositionY }
        let size = { width: 200, height: 90 }
        let inPorts = []
        let outputOrientation: number = 0
        let outPorts = []

        if (item.Type == "Trigger") {
        }
        else if (item.Type == "Condition") {
            inPorts = ['in']
        }
        else if (item.Type == "Action") {
            inPorts = ['in']
        }
        else if (item.Type == "PassThrough") {
            inPorts = ['In']
        }

        if (item.Type == "Action") {
        }
        else if (item.Type == "PassThrough") {
            outPorts = ['Out']
        }
        else if (deviceProperty.DataType == 'enum') {
            outputOrientation = 90
            outPorts = deviceProperty.Format.split(",")
        }
        else if (deviceProperty.DataType == 'boolean') {
            outPorts = ['true', 'false']
        }
        else {
            outPorts = ['<', '<=', '=', '=>', '>']
        }

        let label = {
            text: joint.util.breakText(self.getInfoTextFromProperty(self, item, deviceProperty), {
                width: 200,
                height: 90
            }), 'ref-x': .5, 'ref-y': .2 }
        let m = new joint.shapes.devs.Model({
            position: pos,
            size: size,
            inPorts: inPorts,
            outPorts: outPorts,
            ports: {
                groups: {
                    'in': {
                        position: "top",
                        attrs: {
                            '.port-body': {
                                fill: '#16A085',
                                magnet: 'passive'
                            }
                        },
                        label: {
                            // label layout definition:
                            position: {
                                name: 'manual', args: {
                                    y: -20,
                                    attrs: { '.': { 'text-anchor': 'middle' } }
                                }
                            }
                        }
                    },
                    'out': {
                        position: "bottom",
                        attrs: {
                            '.port-body': {
                                fill: '#E74C3C'
                            }
                        },
                        label: {
                            // label layout definition:
                            position: {
                                name: 'manual', args: {
                                    y: 25,
                                    angle: outputOrientation,
                                    attrs: { '.': { 'text-anchor': 'middle' } }
                                }
                            }
                        }
                    }
                }
            },
            attrs: {
                flowComponentId: item.Id,
                '.label': label,
                rect: { fill: '#CBE2F5' },
                text: { 'font-size': 12 }
            }
        });
        m.addTo(self.graph)
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
        let mouseX = _mouseClientX
        let mouseY = _mouseClientY
        let addComponentFunc = function (item: models.DevicePropertyViewModel, compType: string) {
            let component = new models.FlowComponent()
            if (item != null) {
                component.DevicePropertyId = item.Id
            }
            else {
                component.DevicePropertyId = null
            }
            component.Id = self.getUniqueId(self)
            component.Guid = uuidv4()
            let canvasRect = (self.canvas.element as HTMLElement).getBoundingClientRect()
            component.PositionX = mouseX - canvasRect.left
            component.PositionY = mouseY - canvasRect.top
            component.FlowId = self.activeFlow.Id
            component.Type = compType
            self.flowComponents.push(component)
            self.drawComponent(self, component)
        }

        if (data == 'trigger') {
            let dlg = new dialogs.SelectDevicePropertyDialog()
            dlg.show_(dlg, self.deviceProperties, null, function (item) {
                addComponentFunc(item, 'Trigger')
            })
        }
        else if (data == 'condition') {
            let dlg = new dialogs.SelectDevicePropertyDialog()
            dlg.show_(dlg, self.deviceProperties, null, function (item) {
                addComponentFunc(item, 'Condition')
            })
        }
        else if (data == 'action') {
            let dlg = new dialogs.SelectDevicePropertyDialog()
            let subset = _.filter(self.deviceProperties, function (item) { return item.Settable })
            dlg.show_(dlg, self.deviceProperties, null, function (item) {
                addComponentFunc(item, 'Action')
            })
        }
        else if (data == 'passthrough') {
            addComponentFunc(null, 'PassThrough')
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