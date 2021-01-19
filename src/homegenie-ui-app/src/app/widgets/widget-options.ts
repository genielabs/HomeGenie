export class WidgetOptions {
  widget: string; // widget id
  icon?: string;  // widget icon (TODO: deprecate this)
  data?: any;
  features?: any; // TODO: this is temporary, implement as `data` field and delete it
}
