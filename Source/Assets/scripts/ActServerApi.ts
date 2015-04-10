/// <reference path="libs/jquery.d.ts"/>
/// <reference path="Common.ts"/>

module ActServerApi {
    export class ActServer {
        private _about: IAbout;
        get about(): IAbout { return this._about; }

        constructor(public server: string = "/") {
            // Append slash after server URL if it not ends with slash.
            if (server.charAt(server.length - 1) !== "/") {
                this.server += "/";
            }
            // Prepend protocol and host
            if (this.server.charAt(0) === "/") {
                this.server = window.location.protocol + "//" + window.location.host + this.server;
            }

            this.retrieveServerInfo();
        }
        
        /**
         * Retrieve server information from server.
         */
        private retrieveServerInfo(): void {
            var data: any;

            var aboutCommand = this.server + "command/version";

            try
            {
                $.ajax(aboutCommand, {
                    dataType: "json",
                    url: aboutCommand,
                    async: false,
                    timeout: 2000,
                    success: (data) => {
                        if (isErrorResponse(data)) {
                            throw "Server error: " + data;
                        }
                        this._about = {
                            name: data.name,
                            version: data.version,
                            copyright: data.copyright,
                            hostType: data.hostType,
                            modules: data.modules
                        }
                    },
                    error: (xhr, stat, e) => {
                        throw e;
                    }
                });
            } catch (e) {
                throw e;
            }

        }
        
        /**
         * Returns whether or not the WebSocket is available.
         * @returns {boolean} True if both browser and server support WebSocket.
         */
        isWebSocketAvailable(): boolean {
            var clientSupport = WebSocket !== undefined;
            var serverSupport = this.about.hostType === "OwinSelfHost";

            return clientSupport && serverSupport;
        }
    }
    
    export class IAbout {
        name: string;
        version: string;
        copyright: string;
        hostType: string;
        modules: IModule[];
    }
    
    export class IModule {
        typeName: string;
        assemblyName: string;
        version: string;
        copyright: string;
        routes: string[];
    }

    export interface IException {
        message: string;
        original?: any;
    }

    export function isErrorResponse(data: any): boolean {
        if (Type.isUndefined(data)) return false;
        else if (data.isError) return true;
        else return false;
    }
}