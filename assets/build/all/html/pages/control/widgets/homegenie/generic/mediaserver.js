[{
  Name: "UPnP Media Server",
  Author: "Generoso Martello",
  Version: "2013-10-04",

  GroupName: '',
  IconImage: 'pages/control/widgets/homegenie/generic/images/media.png',
  StatusText: '',
  Description: '',
  DeviceInfo: '',
  NavStack: ['0'],
  NavPath: ['Root'],
  Widget: null,
  ControlPopup: null,
  Module: null,
  Receiver: null,

  RenderView: function (cuid, module) {
    var container = $(cuid);
    var widget = this.Widget = container.find('[data-ui-field=widget]');
    var controlpopup = this.ControlPopup = widget.data('ControlPopUp');
    //
    // create and store a local reference to control popup object
    //
    if (!controlpopup) {
      this.Module = module;
      //
      container.find('[data-ui-field=controlpopup]').trigger('create');
      controlpopup = this.ControlPopup = container.find('[data-ui-field=controlpopup]').popup();
      widget.data('ControlPopUp', controlpopup);
      //
      // initialization stuff here
      //
      // settings button
      widget.find('[data-ui-field=settings]').on('click', function () {
        HG.WebApp.Control.EditModule(module);
      });
      // when browse button is clicked control popup is shown
      widget.find('[data-ui-field=browse]').on('click', function () {
        if ($(cuid).find('[data-ui-field=widget]').data('ControlPopUp')) {
          $(cuid).find('[data-ui-field=widget]').data('ControlPopUp').popup('open');
        }
      });
      //
      var _this = this;
      controlpopup.on('popupbeforeposition', function (event) {
        var availHeight = $(window).height()-320;
        controlpopup.find('[data-ui-field=browsecontent]').parent().css('height', availHeight);
        controlpopup.find('[data-ui-field=browsecontent]').parent().css('max-height', availHeight);
        controlpopup.find('[data-ui-field=browsefiles]').parent().css('height', availHeight);
        controlpopup.find('[data-ui-field=browsefiles]').parent().css('max-height', availHeight);
      });
      controlpopup.on('popupafteropen', function (event) {
        var browselist = controlpopup.find('[data-ui-field=browsecontent]');
        var item = (_this.NavStack.length > 0 ? _this.NavStack[_this.NavStack.length - 1] : '0');
        _this.RenderBrowseList(browselist, item);
      });
    }
    //
    widget.find('[data-ui-field=name]').html(module.Name);
    //
    //
    // read some context data
    //
    this.GroupName = container.attr('data-context-group');
    this.Description = module.Description;
    var info = HG.WebApp.Utility.GetModulePropertyByName(module, "UPnP.ModelDescription");
    this.DeviceInfo = (info != null) ? info.Value : "";
    //
    // render control popup
    //
    controlpopup.find('[data-ui-field=icon]').attr('src', this.IconImage);
    controlpopup.find('[data-ui-field=group]').html(this.GroupName);
    controlpopup.find('[data-ui-field=name]').html(module.Name);
    //
    var sendtolist = controlpopup.find('[data-ui-field=sendto]');
    sendtolist.empty();
    for (m = 0; m < HG.WebApp.Data.Modules.length; m++) {
      var upnpdev = HG.WebApp.Data.Modules[m];
      var devtype = HG.WebApp.Utility.GetModulePropertyByName(upnpdev, "UPnP.StandardDeviceType");
      if (upnpdev.DeviceType == 'MediaReceiver' || (devtype != null && devtype.Value == 'MediaRenderer')) {
        var dn = (upnpdev.Name != '' ? upnpdev.Name : upnpdev.Description);
        var mr = $('<option value="' + upnpdev.Address + '">' + dn + '</option>')
        sendtolist.append(mr);
      }
    }
    sendtolist.append('<option value="browser">This device</option>');
    sendtolist.selectmenu('refresh');
    // 
    widget.find('[data-ui-field=description]').html(this.DeviceInfo != '' ? this.DeviceInfo : this.Description);
  },

  hasSubfolders: false,
  RenderBrowseList: function (browselist, folderid) {
    var _this = this;
    var browsefiles = _this.ControlPopup.find('[data-ui-field=browsefiles]');
    var path = '';
    var folderCount = 0;
    this.hasSubfolders = false;
    $.each(_this.NavPath, function(idx,p){
      path += p+' / ';
    });
    _this.ControlPopup.find('[data-ui-field=path]').html(path);
    $.mobile.loading('show');
    HG.Control.Modules.ServiceCall('AvMedia.Browse', _this.Module.Domain, _this.Module.Address, encodeURIComponent(folderid), function (data) {
      browsefiles.empty();
      var items = eval(data);
      if (typeof (items) == 'undefined') return false;
      for (i = 0; i < items.length; i++) {
        var iconfile = 'browser_folder'; // object.container
        if (items[i].Class.indexOf('object.item.videoItem') == 0) {
          iconfile = 'browser_video';
        } else if (items[i].Class.indexOf('object.item.audioItem') == 0) {
          iconfile = 'browser_audio';
        } else if (items[i].Class.indexOf('object.item.imageItem') == 0) {
          iconfile = 'browser_image';
        }
        if (items[i].Class.indexOf('object.container') == 0) {
          if (folderCount == 0) {
            browselist.empty();
            if (folderid != '0') {
              var levelupitem = $('<li><a style="white-space:nowrap;" class="ui-btn ui-icon-back ui-btn-icon-left" href="#">Parent folder</a></li>');
              levelupitem.on('click', function (e) {
                _this.NavStack.pop();
                _this.NavPath.pop();
                var clicked = _this.NavStack[_this.NavStack.length - 1];
                if (!_this.hasSubfolders && _this.NavStack.length > 1) {
                  _this.NavStack.pop();
                  _this.NavPath.pop();
                  clicked = _this.NavStack[_this.NavStack.length - 1];
                }
                _this.RenderBrowseList(browselist, clicked);
              });
              browselist.append(levelupitem);
            }
            _this.hasSubfolders = true;
          }
          folderCount++;
          var icon = '<img height="34" src="pages/control/widgets/homegenie/generic/images/' + iconfile + '.png" align="left" style="margin-top:4px;margin-left:6px">';
          var litem = $('<li data-context-id="' + items[i].Id + '" data-mini="true" data-icon="false" class="ui-li-has-thumb"><a style="white-space:nowrap;" href="#">' + icon + ' ' + items[i].Title + '</a></li>');
          litem.on('click', function (e) {
            if (_this.hasSubfolders) {
                _this.NavStack.push($(this).attr('data-context-id'));
                _this.NavPath.push($(this).text());
            } else {
                _this.NavStack[_this.NavStack.length-1] = $(this).attr('data-context-id');
                _this.NavPath[_this.NavPath.length-1] = $(this).text();
            }
            _this.RenderBrowseList(browselist, $(this).attr('data-context-id'));
          });
          browselist.append(litem);
        } else {
          var iconWidth = (items[i].Class.indexOf('object.item.audioItem') == 0 ? 72 : 128); // audio or video thumbnail width
          var icon = '<img width="'+iconWidth+'" height="72" src="pages/control/widgets/homegenie/generic/images/' + iconfile + '.png" align="left" style="border:solid 2px gray;margin-left:8px;margin-right:14px;margin-bottom:8px">';
          var litem = $('<li data-context-id="' + items[i].Id + '" data-icon="false" style="padding:0;margin:0;"><div>' + icon + ' <h3>' + items[i].Title + '</h3><p>loading data...</p><span></span></div></li>');
          //litem.on('click', function (e) {
          //});
          browsefiles.append(litem);
        }
      }
      browselist.listview('refresh');
      browsefiles.listview('refresh');
      _this.ControlPopup.popup("reposition", { positionTo: 'window' });
      if (browsefiles.children().length > 0)
        _this.LoadItemData($(browsefiles.children()[0]));
      $.mobile.loading('hide');
    });
  },
  PlayItemTo: function(uri) {
    var _this = this;
    $.mobile.loading('show');
    var mediareceiver = _this.ControlPopup.find('[data-ui-field=sendto]').val();
    var servicecall = HG.Control.Modules.ServiceCall;
    if (mediareceiver == 'browser') {
      window.open(uri);
      $.mobile.loading('hide');
    } else {
      servicecall('AvMedia.SetUri', _this.Module.Domain, mediareceiver, encodeURIComponent(uri), function () {
        setTimeout(function () {
          servicecall('AvMedia.Play', _this.Module.Domain, mediareceiver, '', function () {
            $.mobile.loading('hide');
          });
        }, 1000);
      });
    }
  },
  LoadItemData: function(item) {
    var _this = this;
    var vid = item.attr('data-context-id');
    HG.Control.Modules.ServiceCall('AvMedia.GetItem', _this.Module.Domain, _this.Module.Address, encodeURIComponent(vid), function (data) {
      var title = $(data.item).attr('dc:title');
      if (typeof title != 'undefined' && title.length > 0)
        item.find('h3').html(title);
      item.find('p').empty();
      var size = 0; // TODO: size..
      var duration = '';
      var audioChannels = '';
      var sampleFrequency = '';
      var thumbnail = '';
      var links = [];
      // if item.res is not an array, we turn it into an array of one element
      if (typeof data.item.res != 'undefined' && data.item.res.constructor !== Array) {
        data.item.res = [ data.item.res ];
      }
      $.each(data.item.res, function(idx, el){
        var protocolInfo = '';
        try {
          protocolInfo = $(el).attr('@protocolInfo');
        } catch (e) { }
        if (typeof protocolInfo == 'undefined' || protocolInfo == '')
          return true;
        var protocolUrl = $(el).attr('#text');
        if (protocolInfo.indexOf(':') > 0) {
          if (protocolInfo.indexOf(':image/') > 0 || protocolInfo.indexOf(':fanart:') > 0 || protocolInfo.indexOf(':poster:') > 0) {
            if (thumbnail == '')
              thumbnail = protocolUrl;
            if (links.length == 0) {
              var btn = $('<a class="media-play-btn ui-corner-all">Open</a>');
              var uri = $(el).attr('#text');
              btn.on('click', function() {
                _this.PlayItemTo(uri);
              });
              links.push(btn);
            }
          } else {
            var format = '';
            var f = protocolInfo.substring(protocolInfo.indexOf('/')+1);
            if (f.indexOf(':') > 0) {
              f = f.substring(0, f.indexOf(':'));
              if (f.indexOf('-') > 0)
                f = f.substring(f.lastIndexOf('-')+1);
              format = f;
            }
            var bitrate = $(el).attr('@bitrate');
            if (typeof bitrate == 'undefined' || bitrate == 0)
              bitrate = '';
            else
              bitrate = Math.round(bitrate/1024)+'Kb';
            if (protocolInfo.indexOf(':audio/') > 0) {
              // audio item specific data (not used)
              if (bitrate == '')
                bitrate = 'original';
              var bitsPerSample = $(el).attr('@bitsPerSample');
              var btn = $('<a class="media-play-btn ui-corner-all">'+bitrate+'</a>');
              var uri = $(el).attr('#text');
              btn.on('click', function() {
                _this.PlayItemTo(uri);
              });
              links.push(btn);
            } else if (protocolInfo.indexOf(':video/') > 0) {
              var btn = $('<a class="media-play-btn ui-corner-all">'+format+' '+bitrate+'</a>');
              var uri = $(el).attr('#text');
              btn.on('click', function() {
                _this.PlayItemTo(uri);
              });
              links.push(btn);
            }
            // common item data for video and audio (read from first resource item)
            if (duration == '')
              duration = $(el).attr('@duration');
            if (audioChannels == '')
              audioChannels = $(el).attr('@nrAudioChannels');
            if (sampleFrequency == '')
              sampleFrequency = $(el).attr('@sampleFrequency');
          }
          if (thumbnail != '')
            item.find('img').attr('src', thumbnail);
        }
      });
      if (duration != '')
        item.find('p').append(' <strong>Duration</strong> '+duration);
      if (sampleFrequency != '')
        item.find('p').append(' <strong>Audio Freq.</strong> '+sampleFrequency);
      if (audioChannels != '')
        item.find('p').append(' <strong>Channels</strong> '+audioChannels);
      if (links.length > 0) {
        item.find('span').append(links);
      }
      // load next item data
      _this.ControlPopup.popup("reposition", { positionTo: 'window' });      
      if (item.next().length > 0)
        _this.LoadItemData($(item.next()));
    });
  }

}]