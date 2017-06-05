declare module ChartViewer {
    export interface ViewState {
        [name: string]: any;
    }
    export interface Titles {
        [seriesName: string]: string;
    }
    export interface PlotInfo {
        kind: string;
        displayName: string;
        titles?: Titles;
        [propertyName: string]: any;
    } 
    export interface ChartInfo {
        [id: string]: PlotInfo;
    }
    export interface PropertyTitles {
        [prop: string]: string;
    }
    export interface ViewerControl {
        update(plots: ChartInfo): any;
        viewState: ViewState;
        dispose(): void;
    }
    /**ChartViewer.show() gets a map of pairs (plot identifier, plot definition) as a second argument.
    Plot definition is a collection of key- value pairs specifying properties of a plot, such as line stroke or marker shape.
    Each plot definition has at least one property which is type; it determines a rendering method and must be known to the ChartViewer.*/
    function show(domElement: HTMLElement, plots: ChartInfo, viewState?: ViewState): ChartViewer.ViewerControl;
}

declare module Plot {
    module MarkerShape {
        var Box: string;
        var Circle: string;
        var Diamond: string;
        var Cross: string;
        var Triangle: string;
    }
    /**If treatAs is "Function" (default value), the series x[i] and y[i] are sorted by increasing values x. Otherwise, "Trajectory": the arrays are rendered as is.*/
    module HeatmapRenderType {
        var Gradient: string;
        var Discrete: string;
    }
    module LineTreatAs {
        var Function: string;
        var Trajectory: string;
    }
    /**allows to represent an array of uncertain values by providing the quantiles for each uncertain value*/
    type Quantiles = {
        median: number[];
        lower68: number[];
        upper68: number[];
        lower95: number[];
        upper95: number[];
    };
    /**SizeRange is { min: number; max: number }*/
    type SizeRange = {
        min: number;
        max: number;
    };
    /**Color is a string that supports same color definition as in CSS: "blue", "#606060", "rgba(10,150,200,100)"*/
    type Color = string;
    /**ColorPalette is a string that has specific syntax to define palettes, e.g. "reg,green,blue" or "0=red=white=blue=100"*/
    type ColorPalette = string;
    type LineTitles = {
        x?: string;
        y?: string;
    };
    type MarkersTitles = {
        x?: string;
        y?: string;
        color?: string;
        size?: string;
    };
    type HeatmapTitles = {
        x?: string;
        y?: string;
        values?: string;
    };
    type BandTitles = {
        x?: string;
        y1?: string;
        y2?: string;
    };
    type BoxPlotTitles = {
        x?: string;
        y?: string;
    };
    type LineDefinition = {
        x: number[];
        y: number[] | Quantiles;
        stroke?: Color;
        thickness?: number;
        /**use Plot.LineTreatAs*/
        treatAs?: string;
        fill68?: Color;
        fill95?: Color;
        displayName?: string;
        titles?: LineTitles;
    };
    type BandDefinition = {
        x: number[];
        y1: number[];
        y2: number[];
        fill?: Color;
        displayName?: string;
        titles?: BandTitles;
    };
    type BoxPlotDefinition = {
        y: Quantiles;
        x?: number[];
        borderColor?: Color;
        color?: Color;
        displayName?: string;
        titles?: BoxPlotTitles;
    };
    type MarkersDefinition = {
        x: number[];
        y: number[];
        /**use Plot.MarkerShape*/
        shape?: string;
        color?: Color | number[] | Quantiles;
        colorPalette?: ColorPalette;
        size?: number | number[] | Quantiles;
        sizeRange?: SizeRange;
        borderColor?: Color;
        displayName?: string;
        titles?: MarkersTitles;
    };
   
    type HeatmapDefinition = {
        x: number[];
        y: number[];
        values: number[] | Quantiles;
        colorPalette?: ColorPalette;
        /**use Plot.HeatmapRenderType*/
        treatAs?: string;
        displayName?: string;
        titles?: HeatmapTitles;
    };
    /**line draws a grid function y[i] = y(x[i]) where y[i] is either a scalar value or a random variable distribution.
    In former case, a single polyline is drawn; in the latter case, a median polyline along with filled bands for percentiles of the distribution is rendered.*/
    function line(element: LineDefinition): ChartViewer.PlotInfo;
    /**The plot draws a colored band between two scalar grid functions.
    The space between lines y1[i](x[i]) and y2[i](x[i]) is filled with a solid color; the boundaries are not stroked.*/
    function band(element: BandDefinition): ChartViewer.PlotInfo;
    /**The plot draws a colored boxplot.*/
    function boxplot(element: BoxPlotDefinition): ChartViewer.PlotInfo;
    /**Displays data as a collection of points, each having the value of one variable determining 
    the position on the horizontal axis and the value of the other variable determining the position on the vertical axis. 
    Also variables can be bound to marker size and color. 
    Dependent variable, size-bound variable or color-bound variable 
    can be real or uncertain; the latter is represented as a set of quantiles.*/
    function markers(element: MarkersDefinition): ChartViewer.PlotInfo;
    /**Heatmap plot renders values defined on a rectangular grid using color palette*/
    function heatmap(element: HeatmapDefinition): ChartViewer.PlotInfo;
}

export = { ChartViewer, Plot };