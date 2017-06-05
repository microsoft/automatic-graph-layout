/* jsclr expects "window" and "document" to exist (even fi just for namespacing). In a web worker that is not true, so I'm creating dummy
variables with those names. */
if (typeof window === "undefined") {
    var window = this;
    window.document = {};
    var document = window.document;
}