const __dbg = document.getElementById("debug");
function printDebug(text) {
    if (__dbg) {
        __dbg.innerHTML += text + "<br />";
    }
    console.log(text);
}

const __metadata = document.getElementsByTagName('meta');
function getMeta(name) {
    for (let i = 0; i < __metadata.length; ++i) {
        if (__metadata[i].getAttribute('name') === name) {
            return __metadata[i].getAttribute('content');
        }
    }
    return '';
}

class LukeBotWidget
{
    constructor() {
        this.messages = {};
        this.serverAddress = getMeta('serveraddress');
        printDebug(this.serverAddress);
        this.socket = new WebSocket(this.serverAddress);
        this.socket.onopen = (e) => {
            printDebug(`Connected to server at ${this.serverAddress}`);
        }
        this.socket.onclose = (e) => {
            if (this.close)
                this.close();

            if (e.wasClean) {
                printDebug(`Connection closed cleanly`);
            } else {
                printDebug(`Connection lost: ${e.code} (${e.reason})`);
            }
        }
        this.socket.onerror = (e) => {
            printDebug(`Error: ${e.message}`);
        }
        this.socket.onmessage = (e) => {
            try {
                let obj = JSON.parse(e.data);

                if (obj.EventName != null) {
                    if (this.messages[obj.EventName]) {
                        this.messages[obj.EventName](obj);
                    } else {
                        throw new Error("Invalid event name");
                    }
                }
            } catch (error) {
                printDebug(`Message processing error: ${error}`);
                if (this.messageError)
                    this.messageError(error);
            }
        }
    }

    registerMessage(message, callback) {
        this.messages[message] = callback;
    }

    registerMessageError(callback) {
        this.messageError = callback;
    }

    registerClose(callback) {
        this.close = callback;
    }

    send(object) {
        if (this.socket.readyState === 1)
            this.socket.send(JSON.stringify(object));
    }
}
