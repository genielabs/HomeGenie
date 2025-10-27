/**
 * HomeGenie Custom Widget Controller
 * -------------------------------------------------
 * This file contains the logic for your custom widget.
 *
 * KEY CONCEPTS:
 * - Powered by zuix.js: A component-based framework for modern web UIs.
 * - Web Standard: Widgets are 100% standard Web Components (HTML, CSS, JS).
 * - No Build Step: Edit your code and see the changes instantly.
 * - Shadow DOM: Styles are encapsulated, preventing conflicts with other widgets.
 *
 * QUICK START GUIDE:
 * 1.  Define your widget's appearance in the 'View' (HTML) and 'Style' (CSS) tabs.
 * 2.  Use the '#' attribute to link HTML elements to the reactive data model (e.g., `<div #my_text></div>`).
 * 3.  Use this Controller to add interactivity and handle data.
 * 4.  (Optional) Bind your widget to a data source by selecting a 'Bound Module'
 *     in the widget's configuration. Behind this module is an Automation Program
 *     that provides data and control APIs.
 *
 * GET HELP:
 * - Use the integrated "AI Genie" to generate code, fix bugs, and ask questions.
 * - For detailed documentation, visit: https://homegenie.it/server/programming/widgets
 */

class {{ComponentName}} extends ControllerInstance {

    // declare controller fields here, e.g.
    // myUpdateTimeout = null;
    // updateIntervalMs = 2500;

    /**
     * `onInit()` is called once before the View is loaded.
     * It is the ideal place to declare methods for use in the View.
     */
    onInit() {

        // Example of declaring a method for use in HTML event handlers like `(click)`.
        // The `declare` method automatically handles the `this` context binding.
        // this.declare({
        //   myClickHandler: this.myClickHandler
        // });

    }

    /**
     * `onCreate()` is called when the View (HTML) is loaded and ready.
     * Use this to initialize your widget, set up subscriptions, and perform initial data sync.
     */
    onCreate() {

        // Check if a module is bound to this widget
        if (this.boundModule) {
            // A module is bound! You can now access its data and controls.

            // Example of subscribing to a data field from the module.
            // const tempField = this.boundModule.field('Sensor.Temperature');
            // if (tempField) {
            //   // Using the `subscribe` helper method will ensure that the event
            //   // handler is automatically removed when the widget is disposed.
            //   this.subscribe(tempField, (field) => {
            //     // BEST PRACTICE: Update the reactive model. The View will update automatically.
            //     this.model().temperature = field.value;
            //   });
            // }

            // Example of sending a command to the module.
            // this.boundModule.control('Control.On');

        } else {
            // No module is bound. The widget can still have its own internal logic.
        }

        // The `this.field('my_field')` helper is useful for direct DOM manipulation
        // using the ZxQuery wrapper, but it's not the primary way to update content.

        // BEST PRACTICE for updating content is to use the reactive model:
        // this.model().my_field = 'Hello from the Controller!';

    }

    /**
     * `onDispose()` is called when the widget is removed.
     * Use this to clean up resources, like clearing timers.
     */
    onDispose() {

        // Example: Clean up a timer
        // if (this.myUpdateTimeout) {
        //   clearTimeout(this.myUpdateTimeout);
        // }

    }

    // You can add your own custom methods here.
    // myClickHandler() {
    //   alert('Button was clicked!');
    // }
}

