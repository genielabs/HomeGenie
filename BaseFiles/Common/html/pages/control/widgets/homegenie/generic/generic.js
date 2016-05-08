$$.widget = {
  name: 'Generic Dummy Widget',
  version: '1.2',
  author: 'Generoso Martello',
  release: '2016-05-06',
  icon: 'pages/control/widgets/homegenie/generic/images/flag-yellow.png'
};

$$.start = function() {
  // Settings button click
  $$.field('settings').on('click', function () {
    $$.ui.EditModule($$.module);
  });
}

$$.refresh = function () {
  $$.field('name').html($$.module.Name + " (" + $$.module.DeviceType + ")");
  var description = ($$.module.Domain.substring($$.module.Domain.lastIndexOf('.') + 1)) + ' ' + $$.module.Address;
  $$.field('description').html(description);
  HG.Ui.GetModuleIcon($$.module, function(i,e){
    $$.field(e).attr('src', i);
  }, 'icon');
}

$$.update = function(parameter, value) {
  // TODO: ..
}

$$.stop = function() {
  // TODO: ..
}