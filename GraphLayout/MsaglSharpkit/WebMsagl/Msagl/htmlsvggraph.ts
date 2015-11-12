/// <amd-dependancy path="ggraph"/>
/// <amd-dependancy path="svggraph"/>

// Renderer that targets an SVG element inside HTML.
class HTMLSVGGraph extends SVGGraph {
    graph: GGraph;
    container: HTMLElement;
    grid: boolean = false;

    constructor(containerID: string, graph?: GGraph) {
        super(containerID, graph);
        var self = this;
    }

    drawGraph(): void {
        if (this.svg !== undefined)
            this.container.removeChild(this.svg);
        this.svg = document.createElementNS("http://www.w3.org/2000/svg", "svg");

        super.drawGraph();

        this.container.appendChild(this.svg);
    }
}

declare module "htmlsvggraph" {
    export class HTMLSVGGraph extends SVGGraph {
        graph: GGraph;
        container: HTMLElement;
        grid: boolean;
        constructor(containerID: string, graph?: GGraph);
        drawGraph(): void
    }
}