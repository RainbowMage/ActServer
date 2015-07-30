module ActServerApi.Config {

    export class Config {

        constructor(
            private actServer: ActServer,
            private timeout: number = 1000) { }

        get(name: string): string {
            var url = this.actServer.server + "command/getconfig";
            return $.ajax(url, {
                        data: {
                            name: name
                        },
                        dataType: 'json',
                        timeout: this.timeout
                    }).responseText;
        }

        set(name: string, value: string): void {
            var url = this.actServer.server + "command/setconfig";
            $.ajax(url, {
                data: {
                    name: name,
                    value: value
                },
                dataType: 'json',
                timeout: this.timeout
            });
        }
    }
}