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

    export class SortType {
        constructor(public value: string) { }
        toString() { return this.value; }

        static NONE = new SortType("None");
        static STRING_ASCENDING = new SortType("StringAscending");
        static STRING_DESCENDING = new SortType("StringDescending");
        static NUMERIC_ASCENDING = new SortType("NumericAscending");
        static NUMERIC_DESCENDING = new SortType("NumericDescending");
    }

    export interface IMiniparseOption {
        interval: number;
        timeout: number;
        retryInterval: number;
        sortKey: string;
        sortType: SortType;
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
                retryInterval: 10000,
                sortKey: 'EncDPS',
                sortType: SortType.NUMERIC_DESCENDING
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
                    data: {
                        sortKey: this.option.sortKey,
                        sortType: this.option.sortType.toString()
                    },
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