interface IDictionary<T> {
    [key: string]: T;
    [key: number]: T;
}

module Type {
    export function isNumber(value: any): boolean {
        return typeof value === "number"
            || value instanceof Number;
    }

    export function isBoolean(value: any): boolean {
        return typeof value === "boolean"
            || value instanceof Boolean;
    }

    export function isString(value: any): boolean {
        return typeof value === "string"
            || value instanceof String;
    }

    export function isUndefined(value: any): boolean {
        return value === void 0;
    }

    var nullTypeName = getTypeName(null);

    export function isNull(value: any): boolean {
        return getTypeName(value) === nullTypeName;
    }

    export function isObject(value: any): boolean {
        return value instanceof Object;
    }

    export function isFunction(value: any): boolean {
        return value instanceof Function;
    }

    function getTypeName(value: any): string {
        return Object.prototype.toString.call(value);
    }
}