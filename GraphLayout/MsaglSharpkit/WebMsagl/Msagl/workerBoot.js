// A web worker that uses Require.JS can receive messages before the dependencies have been loaded. Also, a web worker implemented in TS cannot use the require keyword directly,
// because Require.JS will not have been loaded yet. This file imports Require.JS and sets aside an incoming message to be handled after all dependencies have been loaded.

importScripts("../Scripts/require.js");

// This callback stores a message. A queue would be more generic, but in this case one message is sufficient.
var message;
var storeMessage = function (e) { message = e; }

require(['ggraph', 'msaglWorker'], function (G, Worker) {
    // Now that I'm ready, I no longer need to store incoming messages. I'll replace the handler with the real one.
    self.removeEventListener('message', storeMessage)
    self.addEventListener('message', Worker.handleMessage);
    // Process the stored message.
    if (message !== undefined)
        Worker.handleMessage(message);
});

self.addEventListener('message', storeMessage);