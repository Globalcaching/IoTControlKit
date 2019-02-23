import { ElementWrapper, element, Element, selectElement } from "./elements"
import { LocalStorageWorker } from "./storageHelper"

export let flowEditorInstance: flowEditor

export function createFlowEditor(container) {
    flowEditorInstance = new flowEditor(container)
    return flowEditorInstance
}

export class flowEditor extends ElementWrapper{
    containerId: string = ''

    constructor(container) {
        super(container)
        let self = this
        let E = Element
        if (Object.prototype.toString.call(container) == '[object String]') { // container is an id
            this.containerId = container;
            container = document.querySelector(this.containerId);
            if (container == null) {
                console.error("flowEditor cannot find container with id " + this.containerId);
            }
            super(container);
        }
        else {
            this.containerId = (container.id != '') ? container.id : 'Unknown id';
        }

        let toolbox = E('div')
        self.append(toolbox)
    }
}