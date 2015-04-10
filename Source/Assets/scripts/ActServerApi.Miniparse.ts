module ActServerApi.Miniparse {

    export interface IMiniparse {
        start(): void;
        stop(): void;
        onreceive: (message: IMiniparseData) => void;
        onerror: (exception: IException) => void;
    }

    export interface IMiniparseData {
        encounter: IDictionary<string>;
        combatants: IDictionary<string>[];
        Encounter: IDictionary<string>; // for compatibility
        Combatant: IDictionary<string>[]; // for compatibility
        isActive: boolean;
    }

    export interface IMiniparseOption {
        interval: number;
        timeout: number;
        retryInterval: number;
    }

    /**
     * Provides function to send/receive messages using Comet technique.
     */
    export class CometMiniparse implements IMiniparse {
        private timestamp: number;
        private stopRequested: boolean;

        onreceive: (data: IMiniparseData) => void = () => { };
        onerror: (exception: IException) => void = () => { };

        constructor(
            private actServer: ActServer,
            private option: IMiniparseOption = {
                interval: 1000,
                timeout: 5000,
                retryInterval: 10000
            })
        {
            this.timestamp = new Date().getTime();
        }

        start(): void {
            this.stopRequested = false;
            this.poll();
        }

        private poll(): void {
            if (this.stopRequested) {
                return;
            }

            try {
                var url = this.actServer.server + "command/miniparse";
                $.ajax(url, {
                    //contentType: "application/json",
                    dataType: 'json',
                    timeout: this.option.timeout,
                    success: (data) => {
                        if (ActServerApi.isErrorResponse(data)) {
                            throw data.message;
                        }
                        var miniparseData: IMiniparseData = {
                            encounter: data.encounter,
                            combatants: data.combatants,
                            Encounter: data.encounter,
                            Combatant: data.combatants,
                            isActive: data.isActive
                        };
                        setTimeout(() => this.poll(), this.option.interval);
                        this.onreceive(miniparseData);
                    },
                    error: (xhr, status, e) => {
                        setTimeout(() => this.poll(), this.option.retryInterval);
                    }
                });
            } catch (e) {
                setTimeout(() => this.poll(), this.option.retryInterval);
                this.onerror({ message: e });
            }
        }

        stop(): void {
            this.stopRequested = true;
        }
    }
}