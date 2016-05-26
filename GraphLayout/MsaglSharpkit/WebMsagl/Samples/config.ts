/// <reference path="../typings/requirejs/require.d.ts" />
require.config({
    baseUrl: "/",
    paths: {
        rx: "https://cdnjs.cloudflare.com/ajax/libs/rxjs/3.1.2/rx.lite.min",
        idd: "/lib/IDD/idd",
        "jquery": "https://code.jquery.com/jquery-2.1.4.min",
        "filesaver": "/lib/file-saver.js/FileSaver",
        "knockout": "https://cdnjs.cloudflare.com/ajax/libs/knockout/3.4.0/knockout-min",
        //require plugins
        text: "https://cdnjs.cloudflare.com/ajax/libs/require-text/2.0.12/text.min",
    },
    shim: {
        "idd": ["knockout"],
        "jquery": {
            exports: '$'
        },
    }
});