import { ElementWrapper, element, Element, selectElement } from "./elements"

let __activeContextMenu: ContextMenu = null

selectElement('body').addEventListener("click", function (ev: MouseEvent) {
    if (__activeContextMenu != null) {
        __activeContextMenu.close()
    }
});

selectElement('body').addEventListener("keypress", function (ev: KeyboardEvent) {
    if (__activeContextMenu != null) {
        __activeContextMenu.close()
    }
});

selectElement('body').addEventListener("keydown", function (ev: KeyboardEvent) {
    if (__activeContextMenu != null) {
        __activeContextMenu.close()
    }
});

export class ContextMenuItem {
    public menu: ContextMenu
    public enabled: boolean = true
    public visible: boolean = true
    public onClickFunction;
    public style: string = ""
    constructor(menu: ContextMenu, onClickFunction) {
        this.menu = menu
        this.onClickFunction = onClickFunction;
    }

    getMenuItemElement = () => {
        let self = this
        if (self.visible) {
            let result: ElementWrapper = self.getElement(self)
            if (result != null) {
                if (!self.enabled) {
                    result.attr("style", self.style+"color:grey;")
                }
                else {
                    result.attr("onmouseover", "this.style.backgroundColor='rgb(230, 230, 230)'")
                    result.attr("onmouseout", "this.style.backgroundColor=''")
                    result.attr("style", self.style +"cursor:pointer;")
                    result.element.addEventListener("click", function (ev: MouseEvent) {
                        if (self.onClickFunction != null && self.menu != null) {
                            self.onClickFunction(self.menu.item)
                            self.menu.close()
                        }
                    });
                    result.element.addEventListener("contextmenu", function (ev: MouseEvent) {
                        ev.preventDefault();
                        ev.stopPropagation();
                    });
                }
            }
            return result
        }
        return null
    }

    getElement(self: ContextMenuItem) {
        return null
    }

    setEnabled = (v: boolean) => {
        let self = this
        self.enabled = v
        return self
    }
}

export class TextContextMenuItem extends ContextMenuItem {
    caption: string
    imageUrl: string
    content: ElementWrapper
    image: ElementWrapper
    span: ElementWrapper

    constructor(menu: ContextMenu, caption: string, imageUrl: string, onClickFunction) {
        super(menu, onClickFunction)
        let self = this
        self.caption = caption
        self.imageUrl = imageUrl
        self.image = Element('img')
        if (self.imageUrl == null) {
            self.image.attr("style", "visibility: hidden;")
        }
        else {
            self.image.attr("src", self.imageUrl).attr('style', 'height: 16px; width: auto;')
        }
        self.span = Element('span')
        self.span.attr("style", "margin-left:4px;")
        self.span.innerText(self.caption)
        self.content = Element('div').append(self.image, self.span);
    }

    getElement(self: TextContextMenuItem) {
        return self.content
    }
}


export class UrlContextMenuItem extends ContextMenuItem {
    caption: string
    url: string
    imageUrl: string
    content: ElementWrapper
    image: ElementWrapper
    a1: ElementWrapper
    a2: ElementWrapper

    constructor(menu: ContextMenu, url: string, caption: string, imageUrl: string) {
        super(menu, null)
        let self = this
        self.caption = caption
        self.url = url
        self.imageUrl = imageUrl
        self.image = Element('img')
        if (self.imageUrl == null) {
            self.image.attr("style", "visibility: hidden;")
        }
        else {
            self.image.attr("src", self.imageUrl).attr('style', 'height: 16px; width: auto;')
        }
        self.a1 = Element('a')
        self.a1.attr("style", "margin-left:4px;cursor:pointer;text-decoration:none;color:black!important")
        self.a1.attr("href", url)
        self.a1.innerText(self.caption)

        let i = Element("i")
        i.attr("class", "glyphicon glyphicon-new-window")
        i.attr("style", "vertical-align:middle;cursor:pointer;")

        self.a2 = Element('a')
        self.a2.attr("style", "margin-left:4px;")
        self.a2.attr("href", url)
        self.a2.attr("target", "_blank")
        self.a2.append(i)
        self.content = Element('div').append(self.image, self.a1, self.a2);
    }

    getElement(self: TextContextMenuItem) {
        return self.content
    }
}


export class SeparatorContextMenuItem extends ContextMenuItem {

    constructor(menu: ContextMenu) {
        super(menu, null)
        this.enabled = false
    }

    getElement(self: TextContextMenuItem) {
        let result = Element('hr')
        self.style = "margin-top:2px;margin-bottom:2px;"
        return result
    }
}

export class ContextMenu {
    public item: any
    public menuItems: ContextMenuItem[]
    private menuElement: HTMLElement
    constructor(item, menuItems: ContextMenuItem[]) {
        this.item = item
        this.menuItems = menuItems
        if (this.menuItems != null) {
            for (let mi of this.menuItems) {
                mi.menu = this
            }
        }
    }

    private getElement = () => {
        let self = this
        let result = Element('div')
        let resultItems = Element('div')
        result.append(resultItems)
        self.menuElement = result.element
        if (self.menuItems != null) {
            for (let mi of self.menuItems) {
                let el: ElementWrapper = mi.getMenuItemElement()
                if (el != null) {
                    resultItems.append(el)
                }
            }
        }
        result.element.addEventListener("contextmenu", function (ev: MouseEvent) {
            ev.preventDefault();
            ev.stopPropagation();
        });
        return result
    }

    show = (item, evt: MouseEvent) => {
        let self = this
        if (__activeContextMenu != null) {
            __activeContextMenu.close()
        }
        let el = self.getElement()
        let left = evt.clientX;
        let top = evt.clientY;
        el.attr("style", "position:fixed; display:block;width: 200px;background: white;box-shadow: 3px 3px 5px #888888;border: 1px solid grey; padding: 5px 5px 3px 3px;left:" + left + "px;top:" + top + "px;")
        selectElement('body').appendChild(el.element)
        __activeContextMenu = self;
    }

    close = () => {
        let self = this
        if (__activeContextMenu != null) {
            selectElement('body').removeChild(__activeContextMenu.menuElement)
            __activeContextMenu = null;
        }
    }
}