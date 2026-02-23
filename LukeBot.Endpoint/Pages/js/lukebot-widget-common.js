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

class WidgetResponse {
    constructor(evID) {
        this.EventID = evID;
        this.ErrorCount = 0;
        this.Reason = [];
    }

    static fromMessage(msg) {
        return new WidgetResponse(msg.EventID);
    }

    static fromError(evID, error) {
        return new WidgetResponse(evID).fail(error.Message);
    }

    fail(reason) {
        this.ErrorCount = this.ErrorCount + 1;
        this.Reason.push(reason);
        return this;
    }
}

// base class for an object that can execute things at the ExecutionQueue
class Executable {
    execute() {
        throw new TypeError("Executable object must inherit this class and override this method");
    }

    interrupt(isCurrent) {
        throw new TypeError("Executable object must inherit this class and override this method");
    }
}

// Allows to execute items in a queue. Useful for ex. queueing up alerts
// that have to be played one-by-one. See LukeBot.Widget/Alerts.html
class ExecutionQueue extends EventTarget {
    #WAIT_FOR_PUSH_EVENT = "execQueueWaitForPush";

    #queue = [];
    #current = null;
    #done = false;

    #pop() {
        return this.#queue.shift();
    }

    #available() {
        return (this.#queue.length > 0);
    }

    #getWaitForPushPromise() {
        return new Promise((resolve) => {
            const resolver = () => {
                this.removeEventListener(this.#WAIT_FOR_PUSH_EVENT, resolver);
                resolve();
            };

            this.addEventListener(this.#WAIT_FOR_PUSH_EVENT, resolver);
        });
    }

    #notifyPushPromise() {
        this.dispatchEvent(new Event(this.#WAIT_FOR_PUSH_EVENT));
    }

    constructor() {
        super();
    }

    markDone() {
        this.#done = true;
        this.#notifyPushPromise();
    }

    push(obj) {
        if (!(obj instanceof Executable)) {
            throw new Error("Cannot add objects not inheriting Executable class");
        }

        this.#queue.push(obj);
        this.#notifyPushPromise();
    }

    // waits until either #done is true or until there is an available message
    // this is done to let Widgets determine the fate of
    async processNext() {
        if (this.#done) {
            return;
        }

        if (!this.#available()) {
            this.#current = null;
            await this.#getWaitForPushPromise();

            if (this.#done) {
                return;
            }
        }

        this.#current = this.#pop();
        this.#current.execute();
    }

    interrupt(idx) {
        if (idx == 0) {
            if (this.#current) {
                this.#current.interrupt(true);
                return;
            }
        } else if (idx < this.#queue.length ) {
            // this event in queue is not playing yet, interrupt it separately
            // the Executable should handle this properly and send back the Alert is interrupted.
            // Afterwards, remove the entry from the Queue.
            this.#queue[idx].interrupt(false);
            this.#queue.splice(idx, 1);
        }
    }
}

class LukeBotWidget {
    #callbacks = {};
    #messages = {};
    #close = null;
    #messageError = null;
    #connectionError = null;

    constructor() {
        this.serverAddress = getMeta('serveraddress');
        printDebug(this.serverAddress);
        this.socket = new WebSocket(this.serverAddress);
        this.socket.onopen = (e) => {
            printDebug(`Connected to server at ${this.serverAddress}`);
        }
        this.socket.onclose = (e) => {
            if (e.wasClean) {
                printDebug(`Connection closed cleanly`);
            } else {
                printDebug(`Connection lost: ${e.code} (${e.reason})`);
            }
            if (this.#close)
                this.#close(e);
        }
        this.socket.onerror = (e) => {
            printDebug(`Error: ${e.message}`);
            if (this.#connectionError) {
                this.#connectionError(e);
            }
        }
        this.socket.onmessage = (e) => {
            let objEventID = null;
            try {
                let obj = JSON.parse(e.data);

                if (obj.EventName == null ||
                    obj.EventID == null) {
                    printDebug("Invalid message received");
                    return;
                }

                objEventID = obj.EventID;

                if (this.#callbacks[obj.EventName]) {
                    // save this message to currently processed messages
                    this.#messages[obj.EventID] = obj;

                    // execute the Widget-specific callback
                    this.#callbacks[obj.EventName](obj);
                } else {
                    throw new Error("Invalid event name");
                }
            } catch (error) {
                printDebug(`Message processing error: ${error}`);
                if (this.#messageError) {
                    this.#messageError(error);
                }

                alertsWidget.send(new WidgetResponse(objEventID).fail(`ERROR exception while processing message: ${error}`));
            }
        }

        window.addEventListener("beforeunload", () => {
            this.socket.close();
        });
    }

    registerMessage(message, callback) {
        this.#callbacks[message] = callback;
    }

    registerMessageError(callback) {
        this.#messageError = callback;
    }

    registerConnectionError(callback) {
        this.#connectionError = callback;
    }

    registerClose(callback) {
        this.#close = callback;
    }

    send(object) {
        if (!(object instanceof WidgetResponse)) {
            throw new Error("Cannot send back anything other than a WidgetResponse object");
        }

        if (!object.EventID) {
            throw new Error("Invalid/malformed response - missing Event ID to respond to");
        }

        if (!this.#messages[object.EventID]) {
            throw new Error("Invalid response - Event ID does not exist");
        }

        if (this.socket.readyState === WebSocket.OPEN) {
            this.socket.send(JSON.stringify(object));
        }

        delete this.#messages[object.EventID];
    }
}
