## Scenarios and lighting effects


### Interactive recording

To create scenarios and lighting effects, we will be using the Record Macro option
available from the Actions menu of the Control page, as shown in the following image.

[IMAGE LIVE RECORD]

When **Record Macro** option is activated, every action taken (eg. controlling lights/appliances
from the user interface or with a remote control), will be recorded into a **Wizard Script**. 
By default, in the recorded script, it will be put 1 second _delay between each command_.
We can switch between other kinds of delay while recording. One of these is called _Mimic_.
When Mimic is selected, HomeGenie will replicate exactly the delay we put between commands.

[IMAGE RECORD OPTIONS]

By clicking the "Save" button in the footer bar, the current recording session
will be stored into a new **Wizard Script**.

## Wizard Scripts

So, the new Wizard Script, containing all performed commands, will be shown
in the **Program Editor**. From there, we can add/remove commands, change the name,
the description and the associated automation group.

<div class="content-margin" align="center">
    `Work In Progress...`
</div>

[IMAGE WIZARD SCRIPT]

A Wizard Script requires no programming knowledge and it can either be created from
the "Record" option, as just discussed, or from the *Configure->Automation* section
itself by selecting the "Add new program" option from the Actions menu.

### Manual Script Execution

Every Wizard Script can be added like a module to a group.

[IMAGE ADDING SCRIPT BUTTON]

After adding it to a group, it will show up as a button in the Control page.
Tapping this button it will execute the script. A coloured led, on the left
side of the button, will tell us when the script is running (green light)
or when it's idle (yellow light).

[IMAGE DASHBOARD SCRIPT BUTTONS]

A script can also be executed from the Program Editor's Actions menu by
selecting the "Run" option.

### Automated Script Execution

Beside the manual execution, we can choose to automatically execute
a script whenever some conditions are met. By tapping the "Add Condition"
button, from the Program Editor page, we will be able to set all
the "Program Conditions" that will trigger the execution of the script.

[IMAGE WIZARD SCRIPT MOTION TRIGGER]

In the above example we want to run the script whenever the motion is detected from a sensor. 
A script can also be triggered on time based events as explained in the Scheduler page.
