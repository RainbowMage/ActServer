/// <reference path="actserverapi.ts" />
/// <reference path="actserverapi.messaging.ts" />
 
module ActServerApi.GameLog {

    export interface ILog {
        time: number;
        type: number;
        zone: string;
        combat: boolean;
        text: string;
    }

    export interface IGameLogReceiver {
        start(): void;
        stop(): void;
        onreceive: (log: ILog) => void;
        onerror: (exception: IException) => void;
    }

    export class GameLogReceiver implements IGameLogReceiver {
        private webSocket: WebSocket;

        constructor(private actServer: ActServer) {

        }

        onreceive = (log: ILog) => { };
        onerror = (exception: ActServerApi.IException) => { };

        start(): void {
            try {
                if (!Type.isUndefined(this.webSocket)) {
                    this.onerror({ message: "Please stop receiver before starting." });
                    return;
                }
                var url = this.actServer.server + "websocket/gamelog";
                url = url.replace(/^(http|https):\/\//, "ws://"); // replace protocol name
                this.webSocket = new WebSocket(url);
                this.webSocket.onmessage = (ev) => {
                    var data = JSON.parse(ev.data);
                    if (ActServerApi.isErrorResponse(data)) {
                        this.onerror({ message: data.message });
                        return;
                    }
                    var log: ILog = {
                        time: data.time,
                        type: data.type,
                        zone: data.zone,
                        combat: data.combat,
                        text: data.text,
                    };
                    this.onreceive(log);
                };
                this.webSocket.onerror = (e) => {
                    this.onerror({ message: e.message });
                };
            } catch (e) {
                this.onerror({ message: e });
            }
        }

        stop(): void {
            if (!Type.isUndefined(this.webSocket)) {
                this.webSocket.close();
                this.webSocket = void 0;
            }
        }
    }

}