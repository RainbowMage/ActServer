/// <reference path="actserverapi.ts"/>

module ActServerApi.Messaging {
    export function createMessenger(
        server: ActServer,
        option: IMessagingOption): IMessenger
    {
        if (server.isWebSocketAvailable()) {
            return new WebSocketMessenger(server, option);
        } else {
            return new CometMessenger(server, option);
        }
    }

    export interface ISendParam {
        to?: string;
        body: string;
    }

    export interface IMessage {
        from: string;
        to?: string;
        body: string;
        timestamp: number;
    }

    class Message implements IMessage {
        from: string;
        to: string;
        body: string;
        timestamp: number;

        constructor(message: IMessage) {
            this.from = message.from;
            this.to = message.to;
            this.body = message.body;
            this.timestamp = message.timestamp;
        }

        isValid(): boolean {
            return Type.isString(this.from) && this.from !== ""
                && (Type.isUndefined(this.to) || Type.isString(this.to))
                && Type.isString(this.body)
                && Type.isNumber(this.timestamp);
        }
    }

    export interface IMessagingOption {
        name: string;
        timeout?: number;
        retryInterval?: number;
    }

    export interface IMessenger {
        send(message: ISendParam): void;
        start(): void;
        stop(): void;
        onreceive: (message: IMessage) => void;
        onerror: (exception: IException) => void;
    }

    /**
     * Provides function to send/receive messages using WebSocket protocol.
     */
    export class WebSocketMessenger implements IMessenger {
        private webSocket: WebSocket;

        // Handlers
        onreceive: (message: IMessage) => void = () => { };
        onerror: (exception: IException) => void = () => { };

        constructor(
            private actServer: ActServer,
            private option: IMessagingOption) {
        }

        send(message: ISendParam): void {
            if (Type.isUndefined(message.to)) {
                message.to = "";
            }
            try {
                var json = JSON.stringify(message);
                this.webSocket.send(json);
            } catch (e) {
                this.onerror({ message: e, original: e });
            }
        }

        start(): void {
            try {
                var url = this.actServer.server + "websocket/message" + "?name=" + this.option.name;
                url = url.replace(/^(http|https):\/\//, "ws://"); // replace protocol name
                this.webSocket = new WebSocket(url);
                this.webSocket.onmessage = (ev) => {
                    try {
                        var data = JSON.parse(ev.data);
                    } catch (e) {
                        this.onerror({ message: "Unable to parse as JSON: " + ev.data, original: e });
                        return;
                    }
                    if (ActServerApi.isErrorResponse(data)) {
                        this.onerror({ message: data.message });
                        return;
                    }
                    var message = new Message({
                        to: data.to,
                        from: data.from,
                        body: data.body,
                        timestamp: data.timestamp
                    });
                    if (message.isValid()) {
                        this.onreceive(message);
                    } else {
                        this.onerror({ message: "Message object is not valid." });
                    }
                };
                this.webSocket.onerror = (e) => {
                    this.onerror({ message: e.message, original: e });
                };
            } catch (e) {
                this.onerror({ message: e, original: e });
            }
        }

        stop(): void {
            if (!Type.isUndefined(this.webSocket)) {
                this.webSocket.close();
                this.webSocket = void 0;
            }
        }
    }

    /**
     * Provides function to send/receive messages using Comet technique.
     */
    export class CometMessenger implements IMessenger {
        private timestamp: number;
        private stopRequested: boolean;

        onreceive: (message: IMessage) => void = () => { };
        onerror: (exception: IException) => void = () => { };

        constructor(
            private actServer: ActServer,
            private option: IMessagingOption) {
            this.timestamp = new Date().getTime();
        }

        send(message: ISendParam): void {
            if (Type.isUndefined(message.to)) {
                message.to = "";
            }
        }

        start(): void {
            this.stopRequested = false;
            this.poll();
        }

        /**
         * Wait message from server using Comet technique.
         * Set stopRequested variable to true to stop the operation.
         */
        private poll(): void {
            if (this.stopRequested) {
                return;
            }

            try {
                var url = this.actServer.server + "command/message/receive";
                $.ajax(url, {
                    contentType: "application/json",
                    timeout: this.option.timeout ? this.option.timeout : 1000,
                    data: {
                        name: this.option.name,
                        timestamp: this.timestamp
                    },
                    success: (data) => {
                        if (ActServerApi.isErrorResponse(data)) {
                            throw data.message;
                        }
                        var message: Message = new Message({
                            from: data.from,
                            to: data.to,
                            body: data.body,
                            timestamp: data.timestamp
                        });
                        if (message.isValid()) {
                            this.timestamp = message.timestamp;
                            this.poll();
                            this.onreceive(message);
                        } else {
                            this.onerror({ message: "Message is not valid." });
                        }
                    },
                    error: (xhr, status, e) => {
                        this.onerror({ message: e });
                        setTimeout(
                            () => this.poll(),
                            this.option.retryInterval
                                ? this.option.retryInterval
                                : 5000);
                    }
                });
            } catch (e) {
                this.onerror({ message: e });
            }
        }

        stop(): void {
            this.stopRequested = true;
        }
    }
} 