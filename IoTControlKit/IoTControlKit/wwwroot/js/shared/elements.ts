export function selectElement(selector) {
    let result = document.querySelector(selector) as HTMLElement
    return result;
}
export function element(name: string, id?: string, className?: string): HTMLElement {
    let newElement = document.createElement(name)
    if (id != undefined || id != null) {
        newElement.setAttribute('id', id)
    }
    if (className != undefined || className != null) {
        newElement.setAttribute('class', className)
    }
    return newElement
}

let namespaceSvg = "http://www.w3.org/2000/svg"

export function elementSvg(name, id?: string, className?: string) {
    let newElement = document.createElementNS(namespaceSvg, name)
    if (id != null || id != undefined) {
        newElement.setAttribute('id', id)
    }
    if (className != null || className != undefined) {
        newElement.setAttribute('class', className)
    }
    return newElement
}
export function Element(name: string, id?: string, className?: string): ElementWrapper {
    return new ElementWrapper(element(name, id, className));
}
export function ElementSvg(name: string, id?: string, className?: string): ElementWrapper {
    return new ElementWrapper(elementSvg(name, id, className));
}
export class ElementWrapper {
    element: any; // must be any to avoid type checker to complain (cannot be HTMLElement since SVG-elements are not)
    constructor(element: Element) {
        this.element = element;
    }
    getElement = () => {
        let self = this;
        return self.element;
    }
    append = (...others: ElementWrapper[]) => {
        let self = this;
        for (var o of others) {
            self.element.appendChild(o.element);
        }
        return self;
    }
    // insert elements at index, assuming that elements may already be present
    insert = (index: number, ...others: ElementWrapper[]) => {
        let self = this;
        if (index >= self.element.childNodes.length) {
            console.warn(`ElementWrapper.insert: index >= self.element.childNodes.length`)
        } else {
            for (let i = others.length - 1; i > -1; i--) {
                self.element.insertBefore(others[i].element, self.element.childNodes[index])
            }
        }
        return self;
    }
    attr = (name: string, value: any) => {
        let self = this;
        self.element.setAttribute(name, value.toString())
        return self;
    }
    removeChilds = () => {
        let self = this;
        while (self.element.hasChildNodes()) {
            self.element.removeChild(self.element.firstChild);
        }
    }
    removeFromParent = () => {
        let self = this;
        if (self.element.parentNode != null) {
            self.element.parentNode.removeChild(self.element);
        }
    }
    children = () => {
        let self = this;
        return Array.prototype.slice.call(self.element.children);
    }
    indexInParent = () => {
        let self = this;
        return self.children().indexOf(self.element);
    }
    insertBefore = (newNode: ElementWrapper, existingNode: ElementWrapper) => {
        let self = this;
        return self.element.insertBefore(newNode.element, existingNode.element);
    }
    insertAfter = (newNode: ElementWrapper, existingNode: ElementWrapper) => {
        let self = this;
        return self.element.parentNode.insertBefore(newNode.element, existingNode.element.nextSibling)
    }
    height = (value?: number): any => {
        let self = this;
        if (value == null) {
            return self.element.clientHeight;
        } else {
            (self.element as HTMLElement).style.height = value + 'px';
            return self;
        }
    }
    width = (value?: number): any => {
        let self = this;
        if (value == null) {
            return self.element.clientWidth;
        } else {
            (self.element as HTMLElement).style.width = value + 'px';
            return self;
        }
    }
    innerHTML = (value: string) => {
        let self = this;
        self.element.innerHTML = value;
        return self;
    }
    innerText = (value: string) => {
        let self = this;
        (self.element as HTMLElement).innerText = value;
        return self;
    }
    selectedOption = () => {
        let self = this;
        return self.element.options[self.element.selectedIndex]
    }
}