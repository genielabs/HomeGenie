## Scheduling

With the *Scheduler*, which is available from the *Configure* menu, 
*HomeGenie* can be programmed to take some actions on time based occurrences.

The main *Scheduler* page is the calendar view that shows programmed events
for the day. By clicking the date text, we can choose a different
date so to display programmed events for any given day.

<div class="media-container">
    <img self="size-medium" src="images/docs/scheduler_calendar_01.png">
</div>

The time-line of an event will have a yellow bar to indicate the time ranges
where the event occurs. If it's grayed out it means that the occurrence is
in the future and has not occurred yet. Passing the pointer over it
will display a popup with a summary of the event and its last/next
occurrence (see above picture). 

It can also have some indicators on the right:

&nbsp;&nbsp; <i class="material-icons bigger">code</i> a script is associated with the event

&nbsp;&nbsp; <i class="material-icons bigger">check_box</i> number of modules bound to the event

[... // TODO: ...]


### Event scheduling

There are various types of events. The most simple event is the one which
has a single occurrence, a given day at a given time, for example:
April 30th 2017 at 9pm.

But we can also define a recurring event, or an event that combines with
other events.

All these kind of events can be defined using the *Cron event Wizard*.

<div class="media-container">
    <img self="size-medium" src="images/docs/scheduler_wizard_01.png">
</div>

( `//TODO: show how to use the scheduler wizard and its options` )


#### Occurrences

... [naming convention, multiple occurrences, ... ]
 
Another way to define event occurrences (schedules) is by using cron expressions. 
...


#### Modules

...


#### Script 

...



### About cron expressions

Cron expressions are strings that are actually made up of five sub-expressions, that describe individual details of the schedule. These sub-expressions are separated with white-space, and represent:

- Minutes
- Hours
- Day-of-Month
- Month
- Day-of-Week

Individual sub-expressions can contain ranges (eg. 8-22) and/or lists (eg. 5,10,30,45).

Wild-cards (the '\*' character) can be used to say “every” possible value of this field. Therefore the '\*' character in the *Month* field of the previous example simply means “every month”. A '\*' in the Day-Of-Week field would therefore obviously mean “every day of the week”.

All of the fields have a set of valid values that can be specified. These values should be fairly obvious – such as the numbers 0 to 59 for minutes, and the values 0 to 23 for hours. Day-of-Month can be any value 1-31. Months can be specified as values between 1 and 12. Days-of-Week can be specified as values between 0 and 6 (or 1 to 7 since both 0 and 7 stand for Sunday).

The '/' character can be used to specify increments to values. For example, if you put '0/15' in the Minutes field, it means 'every 15th minute of the hour, starting at minute zero'. If you used '3/20' in the Minutes field, it would mean 'every 20th minute of the hour, starting at minute three' – or in other words it is the same as specifying '3,23,43' in the Minutes field.


#### Example Cron Expressions


**Example 1** An expression to create a trigger that simply fires every 5 minutes

```crontab
0/5 * * * *
```

**Example 2** Every even minute

```crontab
*/2 * * * *
```

**Example 3** Every odd minute

```crontab
1-59/2 * * * *
```

**Example 4** Every 5 minutes, Weekdays from 8am-5pm.

```crontab
*/5 8-16 * * 1-5
```


Cron expressions can also be grouped using parenthesis and combined using the following operators:

- ` ; ` &nbsp; *AND*
- ` : ` &nbsp; *OR*
- ` > ` &nbsp; *UNTIL* (time range, 'from' > 'to')
- ` % ` &nbsp; *EXCEPT*


**Example 5** From 11:20PM to 3:15AM

```crontab
(20 23 * * *) > (15 3 * * *)
```

**Example 6** From 11:20PM to 3:15AM except in May(5) and September(9)

```crontab
((20 23 * * *) > (15 3 * * *)) % (* * * 5,9 *)
```

**Example 7** At 11:20PM or 3:15AM in January(1) and December(12) every Sunday(0) and Tuesday(2)

```crontab
((20 23 * * *) : (15 3 * * *)) ; (* * * 1,12 0,2)
```


### Cron variables

[ // TODO: explain how to use cron variables - eg. @Holidays.Winter, or built-in eg: SolarTimes....]
