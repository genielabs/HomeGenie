HG.WebApp.Data._CurrentGroup = null;
HG.WebApp.Data._CurrentGroupIndex = 0;
//
// namespace : HG.WebApp.Home namespace
// info      : -
//
HG.WebApp.Home = HG.WebApp.Home || new function(){ var $$ = this;

    $$.About = function()
    {
        $('#homegenie_about').popup('open');
    };

    $$.UpdateHeaderStatus = function()
    {
        $$.UpdateInterfacesStatus();
    };

    $$.UpdateInterfacesStatus = function()
    {
        var ifaceurl = '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/Interfaces.List/';
        $.ajax({
            url: ifaceurl,
            type: 'GET',
            success: function (data) {
                var interfaces = HG.WebApp.Data.Interfaces = eval(data);
                var status = '';
                var isupdateavailable = false;
                //
                if (interfaces && interfaces != 'undefined')
                {
                    for (i = 0; i < interfaces.length; i++) {
                        var domain = interfaces[i].Domain.split('.');
                        var name = domain[1].toUpperCase();
                        var connected = interfaces[i].IsConnected;
                        //status += '<span style="color:' + (connected == 'True' ? 'lime' : 'gray') + ';margin-right:20px">' + name + '</span>';
                        if (interfaces[i].Domain != "HomeGenie.UpdateChecker")
                        {
                            status += '<img src="images/interfaces/' + name.toLowerCase() + '.png" height="28" width="30" style="' + (connected == 'True' ? 'opacity:1.0' : 'opacity:0.4') + '" vspace="2" hspace="0" />';
                        }
                        else
                        {
                            isupdateavailable = true;
                        }
                    }
                }
                //
                if (isupdateavailable)
                {
                    status += '<a href="#page_configure_maintenance" alt="Update available."><img title="Update available." src="images/update.png" height="28" width="28" style="margin-left:6px" vspace="2" hspace="0" /></a>';
                }
                //
                $('#interfaces_status')
                    .html(status)
                    .data('update_available', isupdateavailable);
            }
        });
    };

};