export interface Adapter {
    id(): string;
    getModules(): any;
    getGroups(): any;
    connect(): boolean;
    control(): any;
}
