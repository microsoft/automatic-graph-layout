/// <reference path="../Scripts/typings/requirejs/require.d.ts" />
require.config({
    baseUrl: "/",
    paths: {
        rx: "https://cdnjs.cloudflare.com/ajax/libs/rxjs/3.1.2/rx.lite.min",
        idd: "https://cdn.rawgit.com/predictionmachines/InteractiveDataDisplay/v1.3.0/dist/idd",
        "jquery": "https://code.jquery.com/jquery-2.1.4.min",
        "jqueryui": "https://cdnjs.cloudflare.com/ajax/libs/jqueryui/1.11.4/jquery-ui.min",
        "svg": "https://cdnjs.cloudflare.com/ajax/libs/svg.js/2.4.0/svg.min",
        "jquery-mousewheel": "https://cdnjs.cloudflare.com/ajax/libs/jquery-mousewheel/3.1.13/jquery.mousewheel.min",
        "filesaver": "https://cdnjs.cloudflare.com/ajax/libs/FileSaver.js/1.3.3/FileSaver.min",
        "knockout": "https://cdnjs.cloudflare.com/ajax/libs/knockout/3.4.0/knockout-min",
        //require plugins
        text: "https://cdnjs.cloudflare.com/ajax/libs/require-text/2.0.12/text.min",
    },
    shim: {
        "idd": { deps: ["knockout"] },
        "jquery": {
            exports: '$'
        },
    }
});