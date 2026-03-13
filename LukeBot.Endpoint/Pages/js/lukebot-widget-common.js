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
    #guid = null;

    constructor(guid) {
        this.#guid = guid;
    }

    matchesGuid(guid) {
        return this.#guid === guid;
    }

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

    interrupt(guid) {
        // first cross check currently processed Executable
        if (this.#current && this.#current.matchesGuid(guid)) {
            this.#current.interrupt(true);
            return;
        }

        // guid doesn't match currently processed Executable - find in queue and interrupt it if found
        var idx = -1;
        for (var i = 0; i < this.#queue.length; i++)
        {
            if (this.#queue[i].matchesGuid(guid)) {
                idx = i;
                break;
            }
        }

        if (idx == -1) {
            throw new Error(`Message ${guid} not found`);
        }

        // this Executable in queue is not processed yet, interrupt it separately
        // the Executable should handle this properly on its own without interrupting currently
        // executed Executable (including notifying the Server it was interrupted).
        // Afterwards, remove the entry from the Queue.
        this.#queue[idx].interrupt(false);
        this.#queue.splice(idx, 1);
    }
}

class LukeBotWidget {
    #callbacks = {};
    #messages = {};
    #onInterrupt = null;
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
            try {
                let obj = JSON.parse(e.data);

                if (obj.EventName == null ||
                    obj.EventID == null) {
                    printDebug("Invalid message received");
                    return;
                }

                // save this message to currently processed messages
                this.#messages[obj.EventID] = obj;

                if (obj.EventName == "InterruptEvent") {
                    if (!obj.EventToInterrupt) {
                        throw new Error("Invalid interrupt message received");
                    }

                    if (!this.#messages[obj.EventToInterrupt]) {
                        throw new Error(`Requesting interrupt of non-existent message ${obj.EventToInterrupt}`);
                    }

                    if (this.#onInterrupt) {
                        // if registered, notify the widget about the interruption
                        this.#onInterrupt(obj.EventToInterrupt);
                    }

                    // finally, send back the positive response
                    this.send(WidgetResponse.fromMessage(obj));
                    return;
                }

                if (this.#callbacks[obj.EventName]) {
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

                this.send(WidgetResponse.fromError(obj.EventID, `ERROR exception while processing message: ${error}`));
            }
        }

        window.addEventListener("beforeunload", () => {
            this.socket.close();
        });
    }

    registerMessage(message, callback) {
        this.#callbacks[message] = callback;
    }

    onInterruptEvent(callback) {
        this.#onInterrupt = callback;
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


// alert chains

class AlertStep {
    constructor() {
        if (this.constructor == AlertStep) {
            throw new Error("Cannot instantiate base class");
        }
    }

    execute(next, status) {
        throw new Error("execute() has not been implemented");
    }

    interrupt(next, isCurrent) {
        throw new Error("interrupt() has not been implemented");
    }
}

// TODO I Think this needs some reworking to be more error proof
// Would be good to use exceptions all over the place and find a way
// to send back failure message if something messes up.
class AlertChain extends Executable {
    constructor(receivedObject) {
        super(receivedObject.EventID);
        this.mChain = [];
        this.mCurrent = 0;
        this.mType = receivedObject.EventName;
        this.mUsername = receivedObject.User;
        this.mDisplayName = receivedObject.DisplayName;
        this.mChainStatus = new WidgetResponse(receivedObject.EventID);
    }

    add(alertEvent) {
        if (!(alertEvent instanceof AlertStep)) {
            throw new Error("Cannot add objects not extending AlertStep()");
        }
        this.mChain.push(alertEvent);
    }

    next() {
        if (this.mInterrupted) return;

        this.mCurrent += 1;
        if (this.mCurrent < this.mChain.length) {
            this.mChain[this.mCurrent].execute(this.next.bind(this), this.mChainStatus);
        }
    }

    execute() {
        if (this.mInterrupted) return;

        this.mCurrent = 0;
        this.mChain[this.mCurrent].execute(this.next.bind(this), this.mChainStatus);
    }

    nextInterrupt(isCurrent) {
        this.mInterruptCurrent += 1;
        if (this.mInterruptCurrent < this.mChain.length) {
            this.mChain[this.mInterruptCurrent].interrupt(this.nextInterrupt.bind(this, isCurrent), isCurrent);
        }
    }

    interrupt(isCurrent) {
        this.mInterrupted = true;
        this.mInterruptCurrent = 0;
        this.mChain[this.mInterruptCurrent].interrupt(this.nextInterrupt.bind(this, isCurrent), isCurrent);
    }
}

class AudioAlert extends AlertStep {
    #mOnLoadedMetadataCallback = null;
    #mEndedListener = null;
    #mAudio = null;
    #mAudioPath = "";
    #mEnsureLongEnough = false;

    constructor(audioPath) {
        super();
        this.#mEnsureLongEnough = false;
        if (audioPath)
            this.setPath(audioPath);
    }

    setPath(path) {
        this.#mAudioPath = path;
    }

    execute(next, status) {
        fetch(this.#mAudioPath)
            .catch((error) => {
                status.fail(`ERROR fetching audio file: ${error}`);
                setTimeout(() => { next() }, 5000);
            })
            .then((response) => {
                if (response.ok) {
                    return response.blob();
                } else {
                    throw new Error(`Bad response from fetching audio file: ${response.status}`);
                }
            })
            .catch((error) => {
                status.fail(`ERROR getting response blob: ${error}`);
                setTimeout(() => { next() }, 5000);
            })
            .then((blob) => {
                var audioURL = window.URL.createObjectURL(blob);
                this.#mAudio = new Audio(audioURL);
                if (this.#mOnLoadedMetadataCallback) {
                    this.#mAudio.addEventListener("loadedmetadata", this.#mOnLoadedMetadataCallback);
                }
                this.#mAudio.addEventListener("canplaythrough", (event) => {
                    this.#mAudio.play().catch(
                        (reason) => {
                            status.fail(`ERROR play failed: ${reason}`);
                            setTimeout(() => { next() }, 5000);
                        }
                    );
                });
                this.#mAudio.addEventListener("loadedmetadata", (event) => {
                    this.#mEndedListener = this.#mAudio.addEventListener("ended", () => {
                        if (this.mEnsureLongEnough && this.#mAudio.duration < 5.0) {
                            // Wait to make the alert last at least 5 seconds
                            setTimeout(() => {
                                next();
                            }, (5.0 - this.#mAudio.duration) * 1000);
                        } else {
                            // Audio was longer than non-message alert, continue
                            next();
                        }
                    });
                });
                this.#mAudio.addEventListener("error", (event) => {
                    status.fail(`ERROR playing audio alert: ${this.#mAudio.error.message}`);
                    setTimeout(() => { next() }, 5000);
                });
            }).catch((error) => {
                status.fail(`ERROR fetching audio file: ${error}`);
                setTimeout(() => { next() }, 5000);
            });
    }

    onLoadedMetadata(callback) {
        // store this callback for later; we will add it during execute if it is real
        this.#mOnLoadedMetadataCallback = callback;
    }

    interrupt(next, isCurrent) {
        if (isCurrent) {
            if (this.#mAudio) {
                if (this.#mEndedListener) {
                    this.mAudio.removeEventListener("ended", this.#mEndedListener);
                    this.#mEndedListener = null;
                }
                this.#mAudio.pause();
            }
        }
        next();
    }
}

class TTSAlert extends AudioAlert {
    constructor(voice, message) {
        super();
        var urlParams = new URLSearchParams();
        urlParams.append("voice", voice);
        urlParams.append("text", message);
        this.setPath("/widget/tts?" + urlParams.toString());
        this.mEnsureLongEnough = true;
    }
}

class TimeoutAlert extends AlertStep {
    constructor(timeMs) {
        super();
        this.mTimeoutMs = timeMs;
        this.mTimer = null;
    }

    execute(next, status) {
        this.mTimer = setTimeout((wait) => {
            next();
            this.mTimer = null;
        }, this.mTimeoutMs);
    }

    interrupt(next, isCurrent) {
        if (isCurrent && this.mTimer) {
            clearTimeout(this.mTimer);
            this.mTimer = null;
        }

        next();
    }
}

class AlertChainComplete extends AlertStep {
    #origMessage = null;
    #widgetReference = null
    #executionQueue = null

    constructor(msgObj, widget, queue) {
        super();
        if (!msgObj) {
            throw new Error("Invalid msgObj");
        }

        if (widget.constructor != LukeBotWidget) {
            throw new Error("Invalid LukeBotWidget reference provided");
        }
        if (queue.constructor != ExecutionQueue) {
            throw new Error("Invalid ExecutionQueue reference provided");
        }

        this.#origMessage = msgObj;
        this.#widgetReference = widget;
        this.#executionQueue = queue;
    }

    execute(next, status) {
        this.#widgetReference.send(WidgetResponse.fromMessage(this.#origMessage));
        this.#executionQueue.processNext();
    }

    interrupt(next, isCurrent) {
        this.#widgetReference.send(WidgetResponse.fromMessage(this.#origMessage).fail("Alert interrupted"));
        if (isCurrent) {
            this.#executionQueue.processNext();
        }
    }
}
