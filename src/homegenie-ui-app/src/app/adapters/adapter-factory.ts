import {Adapter} from './adapter';

class AdapterFactory {
  private static classes = {};

  /**
   * Creates an object instance of type specified by class name.
   * @param className The class name of the adapter to create
   * @param opts Optional argument(s) to pass to the constructor
   */
  static create(className: string, opts?: any): Adapter {
    return new this.classes[className](opts);
  }

  /**
   * Set the list of classes that can be created via the `create` method
   * @param classes The class list
   */
  static setClasses(classes: any): void {
    this.classes = classes;
  }
}

export default AdapterFactory;
