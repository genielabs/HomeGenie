// Turn on, if previous minute was not in schedule (start occurrence range)
if (!$$.onPrevious()) 
  $$.boundModules.on();
// Turn off, if next minute won't be in schedule (end occurrence range)
if ($$.onPrevious() && !$$.onNext()) 
  $$.boundModules.off();
