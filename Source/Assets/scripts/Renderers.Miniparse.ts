/// <reference path="ActServerApi.ts"/>

$(document).ready(() => {

    var miniparseOptions: Renderers.Miniparse.IMiniparseOption = {
        targetElement: $("#miniparse")[0],
        encounterOption: {
            format: "{title} / Time: {duration} / DPS: {ENCDPS}",
            useHTML: true
        },
        tableOption: {
            // Define column headers
            columnHeaders: [
                { text: "#", width: "5%", align: "left" },
                { text: "Name", width: "25%", align: "left" },
                { text: "Job", width: "8%", align: "center" },
                { text: "DPS (%)", width: "18%", align: "center", span: 2 },
                { text: "HPS (%)", width: "18%", align: "center", span: 2 },
                { text: "Acc.(%)", width: "16%", align: "right" },
                { text: "Crit.(%)", width: "16%", align: "right" }
            ],
            // Define cells
            columnCells: [
                { text: (combatant, index) => (index + 1).toString() },
                { text: "{name}", width: "25%" },
                { text: "{Job}", width: "8%", align: "center" },
                { text: "{encdps}", width: "16%", align: "right" },
                { text: "{damage%}", width: "5%", align: "right" },
                { text: "{enchps}", width: "16%", align: "right" },
                { text: "{healed%}", width: "5%", align: "right" },
                { text: "{tohit}%", width: "16%", align: "right" },
                { text: "{crithit%}", width: "16%", align: "right" }
            ]
        }
    }

    var miniparse = new Renderers.Miniparse.MiniparseRenderer(miniparseOptions);

});

module Renderers.Miniparse {

    /**
     * Options for the miniparse view.
     */
    export interface IMiniparseOption {
        encounterOption: IEncouterOption;
        tableOption: ITableOption;
        targetElement: HTMLElement;
    }

    type EncounterFormatFunc = { (encounter: IDictionary<string>): string }

    /**
     * Options for the encounter format.
     */
    interface IEncouterOption {
        useHTML: boolean;
        format: string|EncounterFormatFunc;
    }

    /**
     * Options for the combatant table.
     */
    interface ITableOption {
        columnHeaders: IColumnHeader[];
        columnCells: IColumnCell[];
    }

    interface IColumnHeader {
        text?: string;
        html?: string;
        width?: string;
        align?: string;
        span?: number;
        class?: string;
    }

    type CellBodyFunc = { (combatant: IDictionary<string>, index: number): string }
    type CellEffectFunc = { (element: HTMLElement, combatant: IDictionary<string>, index: number): void };

    interface IColumnCell {
        text?: string|CellBodyFunc;
        html?: string|CellBodyFunc;
        width?: string;
        align?: string;
        class?: string;
        effect?: CellEffectFunc;
    }

    export class MiniparseRenderer {
        private server: ActServerApi.ActServer;
        private messenger: ActServerApi.Messaging.IMessenger;
        private miniparse: ActServerApi.Miniparse.IMiniparse;
        private tableElement: HTMLTableElement;
        private encounterElement: HTMLDivElement;

        constructor(private option: IMiniparseOption) {
            // Create elements
            this.tableElement = document.createElement("table");
            this.tableElement.id = "combatantTable";
            this.encounterElement = document.createElement("div");
            this.encounterElement.id = "encounter";
            this.option.targetElement.appendChild(this.encounterElement);
            this.option.targetElement.appendChild(this.tableElement);

            // Create colgroups for table element
            this.updateTableColumns();

            // Create and initialize API instances
            this.server = new ActServerApi.ActServer("http://localhost:23456/");

            // Messaging API
            this.messenger = ActServerApi.Messaging.createMessenger(this.server, { name: "miniparse" });
            this.messenger.onreceive = (message) => {
                if (message.body === "show") {
                    $("#encounter").show();
                    $("#combatantTable").show();
                } else if (message.body === "hide") {
                    $("#encounter").hide();
                    $("#combatantTable").hide();
                }
            };
            this.messenger.onerror = (error) => {
                console.log(error.message);
            }
            this.messenger.start();

            // Miniparse API
            this.miniparse = new ActServerApi.Miniparse.CometMiniparse(this.server);
            this.miniparse.onreceive = (data) => {
                this.update(data);
            };
            this.miniparse.onerror = (error) => {
                console.log(error.message);
            }
            this.miniparse.start();
        }

        /**
         * Finalizes view object.
         */
        dispose(): void {
            this.miniparse.stop();
            this.messenger.stop();
            $(this.option.targetElement).html("");
        }

        /**
         * Update elements.
         */
        private update(data: ActServerApi.Miniparse.IMiniparseData): void {
            this.updateEncounter(data.encounter);
            if (document.getElementById("combatantTableHeader") == null) {
                this.updateTableHeader();
            }
            this.updateTableCells(data.combatants);
        }

        /**
         * Update encounter information.
         */
        private updateEncounter(encounter: IDictionary<string>) {
            // テキスト取得
            var format = this.option.encounterOption.format;
            var elementText = MiniparseRenderer.getEncounterString(format, encounter);

            // テキスト設定
            if (this.option.encounterOption.useHTML) {
                this.encounterElement.innerHTML = MiniparseRenderer.parseActFormat(elementText, encounter);
            } else {
                this.encounterElement.innerText = MiniparseRenderer.parseActFormat(elementText, encounter);
            }
        }

        private static getEncounterString(
            value: string|EncounterFormatFunc,
            encounter: IDictionary<string>): string
        {
            if (Type.isFunction(value)) {
                var str = (<EncounterFormatFunc>value)(encounter);
                if (Type.isString(str)) {
                    return str;
                }
            } else if (Type.isString(value)) {
                return MiniparseRenderer.parseActFormat(<string>value, encounter);
            }

            return "";
        }

        /**
         * Update table column definitions.
         */
        private updateTableColumns(): void {
            var colgroups = this.tableElement.getElementsByTagName("colgroup");
            if (colgroups.length > 0) {
                return;
            }

            for (var column of this.option.tableOption.columnCells) {
                var colgroup = document.createElement("colgroup");
                if (Type.isString(column.class)) {
                    colgroup.classList.add(column.class);
                }
                if (Type.isNumber(column.width) || Type.isString(column.width)) {
                    colgroup.style.width = column.width;
                    colgroup.style.maxWidth = column.width;
                }
                this.tableElement.appendChild(colgroup);
            }
        }

        /**
         * Update table header
         */
        private updateTableHeader(): void {
            var tableHeader = document.createElement("thead");
            tableHeader.id = "combatantTableHeader";
            var headerRow = tableHeader.insertRow();

            for (var i = 0; i < this.option.tableOption.columnHeaders.length; i++) {
                var cell = document.createElement("th");
                var define = this.option.tableOption.columnHeaders[i];
                // テキスト設定
                if (Type.isString(define.text)) {
                    cell.innerText = define.text;
                } else if (Type.isString(define.html)) {
                    cell.innerHTML = define.html;
                }
                // クラス設定
                if (Type.isString(define.class)) {
                    for (var c in define.class.split(" ")) {
                        cell.classList.add(c);
                    }
                }
                // 幅設定
                if (Type.isNumber(define.width) || Type.isString(define.width)) {
                    cell.style.width = define.width;
                    cell.style.maxWidth = define.width;
                }
                // 横結合数設定
                if (Type.isNumber(define.span)) {
                    cell.colSpan = define.span;
                }
                // 行揃え設定
                if (Type.isString(define.align)) {
                    cell.style["textAlign"] = define.align;
                }
                headerRow.appendChild(cell);
            }

            this.tableElement.tHead = tableHeader;
        }

        // プレイヤーリストを更新する
        private updateTableCells(combatants: IDictionary<string>[]) {
            // 要素取得＆作成
            var oldTableBody = this.tableElement.tBodies.namedItem('combatantTableBody');
            var newTableBody = document.createElement("tbody");
            newTableBody.id = "combatantTableBody";

            // tbody の内容を作成
            var combatantIndex = 0;
            for (var combatantName in combatants) {
                var combatant = combatants[combatantName];
                var tableRow = <HTMLTableRowElement>newTableBody.insertRow(newTableBody.rows.length);
                for (var i = 0; i < this.option.tableOption.columnCells.length; i++) {
                    var cell = tableRow.insertCell(i);
                    var cellOption = this.option.tableOption.columnCells[i];
                    // テキスト設定
                    if (!Type.isUndefined(cellOption.text)) {
                        cell.innerText = MiniparseRenderer.getCellString(cellOption.text, combatant, combatantIndex);
                    } else if (!Type.isUndefined(cellOption.html)) {
                        cell.innerHTML = MiniparseRenderer.getCellString(cellOption.html, combatant, combatantIndex);
                    }
                    // クラス設定
                    if (Type.isString(cellOption.class)) {
                        for (var c in cellOption.class.split(" ")) {
                            cell.classList.add(c);
                        }
                    }
                    // 幅設定
                    if (Type.isNumber(cellOption.width) || Type.isString(cellOption.width)) {
                        cell.style.width = cellOption.width;
                        cell.style.maxWidth = cellOption.width;
                    }
                    // 行構え設定
                    if (Type.isString(cellOption.align)) {
                        cell.style.textAlign = cellOption.align;
                    }
                    // エフェクト実行
                    if (Type.isFunction(cellOption.effect)) {
                        cellOption.effect(cell, combatant, combatantIndex);
                    }
                }
                combatantIndex++;
            }

            // tbody が既に存在していたら置換、そうでないならテーブルに追加
            if (Type.isObject(oldTableBody)) {
                this.tableElement.replaceChild(newTableBody, oldTableBody);
            }
            else {
                this.tableElement.appendChild(newTableBody);
            }
        }

        private static getCellString(
            value: string|CellBodyFunc,
            combatant: IDictionary<string>,
            index: number)
        {
            if (Type.isFunction(value)) {
                var str = (<CellBodyFunc>value)(combatant, index);
                if (Type.isString(str)) {
                    return str;
                }
            } else if (Type.isString(value)) {
                return MiniparseRenderer.parseActFormat(<string>value, combatant);
            }

            return "";
        }

        /**
         * Parse ACT format tags.
         */
        private static parseActFormat(
            str: string,
            dictionary: IDictionary<string>) {
            var result = "";

            var currentIndex = 0;
            do {
                var openBraceIndex = str.indexOf('{', currentIndex);
                if (openBraceIndex < 0) {
                    result += str.slice(currentIndex);
                    break;
                }
                else {
                    result += str.slice(currentIndex, openBraceIndex);
                    var closeBraceIndex = str.indexOf('}', openBraceIndex);
                    if (closeBraceIndex < 0) {
                        // parse error!
                        console.error("parseActFormat: Parse error: missing close-brace for %d.", openBraceIndex);
                        return "ERROR";
                    }
                    else {
                        var tag = str.slice(openBraceIndex + 1, closeBraceIndex);
                        if (!Type.isUndefined(dictionary[tag])) {
                            result += dictionary[tag];
                        } else {
                            console.error("parseActFormat: Unknown tag: '%s'", tag);
                            result += "ERROR";
                        }
                        currentIndex = closeBraceIndex + 1;
                    }
                }
            } while (currentIndex < str.length);
            return result;
        }
    }
}