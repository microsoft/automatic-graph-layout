/// <amd-dependency path="ggraph"/>
/// <amd-dependency path="contextgraph"/>

// Renderer that targets a Canvas.
class CGraph extends ContextGraph {
    graph: GGraph;
    canvas: HTMLCanvasElement;
    grid: boolean = false;

    constructor(canvasID: string, graph?: GGraph) {
        super();
        this.graph = graph === undefined ? null : graph;
        this.canvas = <HTMLCanvasElement>document.getElementById(canvasID);
    }

    drawGraph(): void {
        var context: CanvasRenderingContext2D = this.canvas.getContext("2d");
        context.clearRect(0, 0, this.canvas.width, this.canvas.height);
        if (this.grid)
            this.drawGrid(context);
        context.save();

        var bbox: GRect = this.graph.boundingBox;
        var offsetX = -bbox.x;
        var offsetY = -bbox.y;

        context.translate(offsetX + 0.5, offsetY + 0.5); // the half-pixel translation makes for nicer antialiasing.

        this.drawGraphInternal(context, this.graph);

        context.restore();
    }
}

declare module "cgraph" {
    export class CGraph extends ContextGraph {
        graph: GGraph;
        canvas: HTMLCanvasElement;
        grid: boolean;
        constructor(canvasID: string, graph?: GGraph);
        drawGraph(): void
    }
}