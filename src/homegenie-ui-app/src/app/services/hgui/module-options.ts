import {ModuleField} from "./module";

export class ModuleOptions {
  public id: string;
  public name: string;
  public description: string;
  public items: OptionField[];
}

export class OptionField {
  pid: string;
  field: ModuleField;
  type: OptionFieldType;
  name: string;
  description: string;

}

export class OptionFieldType {
  id: OptionFieldTypeId;
  options: any[];
}

export enum OptionFieldTypeId {
  Text,
  Password,
  CheckBox,
  Slider,
  Location,
  ModuleSelect,
  ScenarioSelect,
  FieldCapture
}
