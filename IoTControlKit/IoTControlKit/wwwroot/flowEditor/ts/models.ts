export class Flow {
    Id: number
    Name: string
    Enabled: boolean
}

export class FlowComponent {
    Id: number
    FlowId: number
    Type: string
    DevicePropertyId: number
    Value: string
    PositionX: number
    PositionY: number
}

export class FlowConnector {
    Id: number
    TargetFlowComponentd: number
    SourceFlowComponentd: number
    SourcePort: string  //true, false, <, <=, = etc.
}

export class DeviceProperty {
    Id: number
    DeviceId: number
    NormalizedName: string
    Name: string
    Retained: boolean
    Settable: boolean
    DataType: string
    Unit: string
    Format: string
}

export class DevicePropertyViewModel extends DeviceProperty {
    DeviceName: string
    ControllerName: string
}


export class FlowViewModel {
    Flows: Flow[]
    FlowComponents: FlowComponent[]
    FlowConnectors: FlowConnector[]
    DeviceProperties: DevicePropertyViewModel[]
}
