var ActServerApi = {

    /**
     * Unicast a message.
     * @param to {String} [Required] Name of the receiver.
     * @param message {String} [Required] Message to unicast.
     * @returns {XMLHttpRequest}
     */
    sendMessage: function (server, to, body) {
        if (!server) {
            throw "'server' is undefined.";
        }
        if (!to) {
            throw "'to' is undefined.";
        }
        if (!body) {
            throw "'body' is undefined.";
        }
        if (server.charAt(server.length - 1) != "/") {
            server += "/";
        }
        var query = {
            "action": "sendMessage",
            "from": "sender",
            "to": to,
            "body": body
        };
        return $.getJSON(server + "command/message/send", query, function (json) {
            if (json.isError) {
                console.log(json.message);
            }
        });
    },

    /**
     * Broadcast a message.
     * @param message {String} [Required] Message to broadcast.
     * @returns {XMLHttpRequest}
     */
    broadcastMessage: function (server, body) {
        if (!server) {
            throw "'server' is undefined.";
        }
        if (!body) {
            throw "'body' is undefined.";
        }
        if (server.charAt(server.length - 1) != "/") {
            server += "/";
        }
        var query = {
            "action": "broadcastMessage",
            "from": "sender",
            "body": body
        };
        return $.getJSON(server + "command/message/send", query, function (json) {
            if (json.isError) {
                console.log(json.message);
            }
        });
    },
    /**
     * Starts receiving the message from server.
     * @param param {Object} The object contains parameters.
     *        - server {String} [Required] URL of the ActServer. e.g. "http://localhost:23456/"
     *        - name {String} [Required] Identifier of the receiver.
     *        - receive {Function} Handler for received message.
     *        - error {Function} Error handler.
     *        - timeout {Integer} Timeout of the connection in milliseconds.
     *        - interval {Integer} Interval of requests.
     *        - retryInterval {Integer} Retry interval when error occured.
     * @returns {Object} Controller of the receiver.
     */
    startReceiveMessage: function (param) {
        var controller = this._getControllerObject();
        if (param && !param.timestamp) {
            param.timestamp = new Date().getTime();
        }
        this._pollMessage(param, controller);
        return controller;
    },

    _pollMessage: function(param, controller) {
        if (!param) {
            throw "'param' is undefined.";
        }
        if (!param.server) {
            throw "'server' is not specified.";
        }
        if (!param.name) {
            throw "'name' is not specified.";
        }

        if (param.server.charAt(param.server.length - 1) != "/") {
            param.server += "/";
        }

        if (!controller._stopped) {
            var _this = this;
            $.ajax({
                dataType: "json",
                url: param.server + "command/message/receive",
                data: {
                    "action": "requestMessage",
                    "name": param.name,
                    "timestamp": param.timestamp
                },
                timeout: param.timeout ? param.timeout : 60000,
                success: function (json) {
                    if (!json.isError) {
                        param.timestamp = json.timestamp;
                        _this._pollMessage(param, controller);
                        if (typeof param.receive === "function") {
                            param.receive(json);
                        }
                    } else {
                        setTimeout(function () {
                            _this._pollMessage(param, controller);
                        }, param.retryInterval ? param.retryInterval : 1000);
                        if (typeof param.error === "function") {
                            param.error(json.message);
                        }
                    }
                },
                error: function (_, status, thrown) {
                    setTimeout(function () {
                        _this._pollMessage(param, controller);
                    }, param.retryInterval ? param.retryInterval : 1000);
                    if (typeof param.error === "function") {
                        param.error(status);
                    }
                }
            });
        }
    },

    /**
     * Starts receiving the data from server.
     * @param param {Object} The object contains parameters.
     *        - server {String} [Required] URL of the ActServer. e.g. "http://localhost:23456/"
     *        - module {String} [Required] Module name. e.g. "miniparse"
     *        - data {Object} Additional option for module.
     *        - receive {Function} Handler for received json data.
     *        - error {Function} Error handler.
     *        - timeout {Integer} Timeout of the connection in milliseconds.
     *        - interval {Integer} Interval of requests.
     *        - retryInterval {Integer} Retry interval when error occured.
     * @returns {Object} Controller of the receiver.
     */
    startReceiveData: function (param) {
        var controller = this._getControllerObject();
        this._pollData(param, controller);
        return controller;
    },

    _pollData: function (param, controller) {
        if (!controller._stopped) {

            if (!param) {
                throw "'param' is undefined.";
            }
            if (!param.server) {
                throw "'server' is not specified.";
            }
            if (!param.module) {
                throw "'module' is not specified.";
            }
            
            var data = {
                action: "requestData",
                dataType: param.dataType
            };
            if (param.data) {
                data = $.extend(data, param.data);
            }

            if (param.server.charAt(param.server.length - 1) != "/") {
                param.server += "/";
            }
            
            var _this = this;
            $.ajax({
                dataType: "json",
                url: param.server + "command/" + param.module,
                timeout: param.timeout ? param.timeout : 60000,
                "data": data,
                success: function (json) {
                    if (!json.isError) {
                        setTimeout(function () {
                            _this._pollData(param, controller);
                        }, param.interval ? param.interval : 1000);
                        if (typeof param.receive === "function") {
                            param.receive(json);
                        }
                    } else {
                        setTimeout(function () {
                            _this._pollData(param, controller);
                        }, param.retryInterval ? param.retryInterval : 1000);
                        if (typeof param.error === "function") {
                            param.error(json.message);
                        }
                    }
                },
                error: function (_, status, thrown) {
                    setTimeout(function () {
                        _this._pollData(param, controller);
                    }, param.retryInterval ? param.retryInterval : 1000);
                    if (typeof param.error === "function") {
                        param.error(status);
                    }
                }
            });
        }
    },

    _getControllerObject: function () {
        return {
            _stopped: false,
            stop: function () { this._stopped = true; }
        }
    }
};