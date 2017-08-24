export type Req_RunLayout = { type: "RunLayout", graph: string }
export type Req_RouteEdges = { type: "RouteEdges", graph: string, edges?: string[] }
export type Req_SetPolyline = { type: "SetPolyline", graph: string, edge: string, polyline: string }

export type Res_RunLayout = { type: "RunLayout", graph: string }
export type Res_RouteEdges = { type: "RouteEdges", graph: string, edges?: string[] }
export type Res_SetPolyline = { type: "SetPolyline", edge: string, curve: string, sourceArrowHeadStart: string, sourceArrowHeadEnd: string, targetArrowHeadStart: string, targetArrowHeadEnd: string }

export type Res_Error = { type: "Error", error: any }

export type Request = Req_RunLayout | Req_RouteEdges | Req_SetPolyline
export type Response = Res_RunLayout | Res_RouteEdges | Res_SetPolyline | Res_Error