---
layout: main
title: HomeGenie Scheduler
published: true
---

## Scheduler

...

### Cron expressions

Another way to define event occurrences (schedules) is by using cron expressions. 

Cron expressions are strings that are actually made up of seven sub-expressions, that describe individual details of the schedule. These sub-expression are separated with white-space, and represent:

- Minutes
- Hours
- Day-of-Month
- Month
- Day-of-Week

Individual sub-expressions can contain ranges and/or lists.

Wild-cards (the '\*' character) can be used to say “every” possible value of this field. Therefore the '\*' character in the *Month* field of the previous example simply means “every month”. A '\*' in the Day-Of-Week field would therefore obviously mean “every day of the week”.

All of the fields have a set of valid values that can be specified. These values should be fairly obvious – such as the numbers 0 to 59 for seconds and minutes, and the values 0 to 23 for hours. Day-of-Month can be any value 1-31. Months can be specified as values between 1 and 12. Days-of-Week can be specified as values between 0 and 6 (or 1 to 7 since both 0 and 7 stand for Sunday).

The '/' character can be used to specify increments to values. For example, if you put '0/15' in the Minutes field, it means 'every 15th minute of the hour, starting at minute zero'. If you used '3/20' in the Minutes field, it would mean 'every 20th minute of the hour, starting at minute three' – or in other words it is the same as specifying '3,23,43' in the Minutes field.


#### Example Cron Expressions


**Example 1** – An expression to create a trigger that simply fires every 5 minutes

	0/5 * * * *

**Example 2** – Every even minute

	*/2 * * * *

**Example 3** – Every odd minute

	1-59/2 * * * *

**Example 4** – Every 5 minutes, Weekdays from 8am-5pm.

	*/5 8-16 * * 2-6


Cron expressions can also be grouped using parenthesis and combined using the following operators:


` ; ` &nbsp; logical *AND* operator

` : ` &nbsp; logical *OR*  operator

` > ` &nbsp; *UNTIL* (time range)

` % ` &nbsp; *EXCEPT*


**Example 5** - From 11:20 PM to 3:15 AM

	(20 23 * * *) > (15 3 * * *)

**Example 6** - From 11:20 PM to 3:15 AM except in May (4) and September (8)

	((20 23 * * *) > (15 3 * * *)) % (* * * 4,8 *)

**Example 7** - At 11:20 PM or 3:15 AM in January (1) and December (12) every Sunday (0) and Tuesday (2)

	((20 23 * * *) : (15 3 * * *)) ; (* * * 1,12 0,2)



... **TODO** ...
