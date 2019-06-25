/* This is a bootstrap file for the web worker. The reason this is needed is that I want to be able to import other AMD modules with RequireJS
in the web worker TS file. This poses two problems. The first is that the RequireJS library needs to be loaded before the TS file is loaded. This
alone would be sufficient to require a bootstrap file. The second problem is that a web worker can start receiving messages immediately after
its source file has been processed. Because AMD modules are loaded asynchronously, the main source file will execute to the end immediately, but
the web worker will not actually be ready to process messages yet. Now, if I call addEventListener only after finishing loading all the modules
I need, I can lose messages. But if I call it before I load the modules, I can't pass it the right callback, because it won't have been loaded
yet. Therefore, this bootstrap, in addition to loading RequireJS, also calls addEventListener with a temporary callback. The temporary callback
stores the message, so that it can be processed after the AMD modules have been loaded. At that point, the temporary callback will be removed
and replaced with a callback that goes directly to the function that does the job. */

// Load RequireJS.
importScripts("https://cdnjs.cloudflare.com/ajax/libs/require.js/2.2.0/require.min.js");

/** A queue of messages that have been stored by the temporary callback. */
var messages = [];
/** The temporary callback. All it does is store the message. */
var storeMessage = function (e) { messages.push(e); }

// Start loading the AMD modules, including the "real" web worker file.
require(['./ggraph', './msagl.worker'], function (G, Worker) {
    // Now that I'm ready, I no longer need to store incoming messages. I'll replace the handler with the real one.
    self.removeEventListener('message', storeMessage)
    self.addEventListener('message', Worker.handleMessage);
    // If I have stored messages (due to them having arrived while I was still loading the AMD modules), I should process then now.
    // Note that using "for(var i in messages)" does not work due to SharpKit messing with arrays.
    for (var i = 0; i < messages.length; i++)
        Worker.handleMessage(messages[i]);
    messages = null;
});

// Add the temporary callback.
self.addEventListener('message', storeMessage);