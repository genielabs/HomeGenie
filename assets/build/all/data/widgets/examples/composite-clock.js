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


/**
 * Import module dependencies to ensure Custom Elements are registered.
 * zuix.js will automatically upgrade the DOM nodes as soon as the
 * components are loaded and ready.
 */
import('/widgets/demo/analog-clock.module.js');
import('/widgets/examples/time-clock.module.js');


class Composite extends ControllerInstance {
    settings = {
      defaultSize: 'big'
    }
    /*
    onInit() {
    }

    onCreate() {
    }

    onDispose() {
    }
    */
}

