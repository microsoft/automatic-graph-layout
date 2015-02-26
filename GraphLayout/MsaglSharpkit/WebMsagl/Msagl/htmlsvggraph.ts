import G = require('./ggraph');
import SVGG = require('./svggraph');

declare var $;

// Renderer that targets an SVG element inside HTML.
export class HTMLSVGGraph extends SVGG.SVGGraph {
    graph: G.GGraph;
    container: HTMLElement;
    grid: boolean = false;

    constructor(containerID: string, graph?: G.GGraph) {
        super(containerID, graph);
        var self = this;
    }

    drawGraph():void {
        if (this.svg !== undefined)
            this.container.removeChild(this.svg);
        this.svg = document.createElementNS("http://www.w3.org/2000/svg", "svg");

        super.drawGraph();

        this.container.appendChild(this.svg);
    }
}