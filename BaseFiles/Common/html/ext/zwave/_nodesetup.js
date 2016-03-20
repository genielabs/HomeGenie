HG.Ext.ZWave = HG.Ext.ZWave || {};
HG.Ext.ZWave.NodeSetup = HG.Ext.ZWave.NodeSetup || {};

HG.Ext.ZWave.NodeSetup.Show = function (module) {
    HG.Ext.ZWave.NodeSetup.Refresh(module);
}

HG.Ext.ZWave.NodeSetup.Refresh = function (module) {

    // load data into fields
    $('#configurepage_OptionZWave_id').val(module.Address);
    $('#configurepage_OptionZWave').find('h3[data-module-prop="Name"]').html(module.Name);
    //$('#configurepage_OptionZWave').find('span[data-module-prop="Description"]').html(module.Description);
    //$('#configurepage_OptionZWave').find('span[data-module-prop="Interface"]').html(module.Interface);
    //$('#configurepage_OptionZWave').find('h4[data-module-prop="Domain"]').html(module.Domain);
    $('#configurepage_OptionZWave').find('span[data-module-prop="Address"]').html(module.Address);
    //$('#configurepage_OptionZWave').find('span[data-module-prop="Group"]').html(module.Group);
    //
    /*
    var basicval = HG.WebApp.Utility.GetModulePropertyByName(module, "Status.Level"); // TODO: duplicate prop to ZWaveNode.BasicCommandValue
    if (basicval != null)
    {
        basicval = basicval.Value;
    }
    else
    {
        basicval = "";
    }
    $('#configurepage_OptionZWave').find('input[data-module-prop="BasicCommandValue"]').val(basicval);
    */
    //
    $('#opt-zwave-configvar-label').html('Variable Value = ?');
    $('#opt-zwave-heal-label').html('Routing Info = ?');
    $('#opt-zwave-basic-label').html('Basic Value = ?');
    $('#opt-zwave-wakeup-label').html('Wake Up Interval = ?');
    $('#opt-zwave-association-label').html('Nodes Id in this group = ?');
    $('#opt-zwave-configvar-label').html('Variable Value = ?');
    $('#opt-zwave-switchbinary-label').html('Switch Binary = ?');
    $('#opt-zwave-switchmulti-label').html('Switch MultiLevel = ?');
    $('#opt-zwave-sensorbinary-label').html('Sensor Binary = ?');
    $('#opt-zwave-sensormulti-label').html('Sensor MultiLevel = ?');
    $('#opt-zwave-battery-label').html('Battery Level = ?');
    //
    $('#opt-zwave-association-groupinput').show();
    $('#opt-zwave-association-grouplist').hide();
    $('#opt-zwave-configuration-varinput').show();
    $('#opt-zwave-configuration-varslist').hide();
    //
    $('#opt-zwave-wakeup-box').hide();
    $('#opt-zwave-associations-box').hide();
    $('#opt-zwave-configuration-box').hide();
    //
    $('#opt-zwave-multiinstance-box').hide();
    $('#opt-zwave-switchbinary-opt').hide();
    $('#opt-zwave-switchmulti-opt').hide();
    $('#opt-zwave-sensorbinary-opt').hide();
    $('#opt-zwave-sensormulti-opt').hide();
    //
    $('#opt-zwave-meter-box').hide();
    //
    $('#opt-zwave-battery-box').hide();
    $('#opt-zwave-door-lock').hide();
    //
    $('#opt-zwave-nodeinformation-overview').empty();
    //
    var manufacturerspec = HG.WebApp.Utility.GetModulePropertyByName(module, 'ZWaveNode.ManufacturerSpecific');
    $('#opt-zwave-manufacturerspecs-label').html('Manufacturer Specific: ' + (manufacturerspec != null ? manufacturerspec.Value : '?'));
    //
    var nodeVersion = HG.WebApp.Utility.GetModulePropertyByName(module, 'ZWaveNode.VersionReport');
    $('#opt-zwave-versionreport-label').html('NodeVersion: ' + (nodeVersion == null ? '?' : formatNodeVersion(nodeVersion.Value)));
    //
    var nodeinfo = HG.WebApp.Utility.GetModulePropertyByName(module, 'ZWaveNode.NodeInfo');
    var infotext = '';
    if (nodeinfo != null) {
        //
        var classdesc = new Array();
        classdesc['20'] = 'Basic';
        classdesc['22'] = 'Application Status';
        classdesc['25'] = 'Switch Binary';
        classdesc['26'] = 'Switch Multi Level';
        classdesc['27'] = 'Switch All';
        classdesc['2B'] = 'Scene Activation';
        classdesc['30'] = 'Sensor Binary';
        classdesc['31'] = 'Sensor Multi Level';
        classdesc['32'] = 'Meter';
        classdesc['38'] = 'Thermostat Heating';
        classdesc['40'] = 'Thermostat Mode';
        classdesc['42'] = 'Thermostat Operating State';
        classdesc['43'] = 'Thermostat Set Point';
        classdesc['44'] = 'Thermostat Fan Mode';
        classdesc['45'] = 'Thermostat Fan State';
        classdesc['47'] = 'Thermostat Set Back';
        classdesc['60'] = 'Multi Instance';
        classdesc['62'] = 'Door Lock';
        classdesc['63'] = 'User Code';
        classdesc['70'] = 'Configuration';
        classdesc['71'] = 'Alarm';
        classdesc['72'] = 'Manufacturer Specific';
        classdesc['77'] = 'Node Naming';
        classdesc['7A'] = 'Firmware Update';
        classdesc['80'] = 'Battery';
        classdesc['82'] = 'Hail';
        classdesc['84'] = 'Wake Up';
        classdesc['85'] = 'Association';
        classdesc['86'] = 'Version';
        classdesc['98'] = 'Security';
        classdesc['9C'] = 'Sensor Alarm';
        classdesc['9D'] = 'Silence Alarm';
        //
        var zclasses = nodeinfo.Value.split(' ');
        for (var zc = 3; zc < zclasses.length; zc++) {
            if (zc !== 3) infotext += ', ';
            var desc = zclasses[zc];
            if (typeof (classdesc[zclasses[zc]]) != 'undefined') {
                desc = classdesc[zclasses[zc]];
            }
            infotext += desc;
            //
            switch (desc) {
                case 'Wake Up':
                    $('#opt-zwave-wakeup-box').show();
                    break;
                case 'Association':
                    $('#opt-zwave-associations-box').show();
                    break;
                case 'Configuration':
                    $('#opt-zwave-configuration-box').show();
                    break;
                case 'Multi Instance':
                    $('#opt-zwave-multiinstance-box').show();
                    break;
                case 'Switch Binary':
                    $('#opt-zwave-switchbinary-opt').show();
                    break;
                case 'Switch Multi Level':
                    $('#opt-zwave-switchmulti-opt').show();
                    break;
                case 'Sensor Binary':
                    $('#opt-zwave-sensorbinary-opt').show();
                    break;
                case 'Sensor Multi Level':
                    $('#opt-zwave-sensormulti-opt').show();
                    break;
                case 'Battery':
                    $('#opt-zwave-battery-box').show();
                    break;
                case 'Meter':
                    $('#opt-zwave-meter-box').show();
                    break;
                case 'Door Lock':
                    $('#opt-zwave-door-lock').show();
                    break;
            }
        }
        //
        infotext = '<p style="font-weight:normal;font-size:10pt"><strong>Supported Classes</strong><br />' + infotext + '</p><br />';
        $('#opt-zwave-nodeinformation-overview').html(infotext);
        //
    }
    else {
        var tries = 0;
        var callback = function (success) {
            if (!success && tries < 3) {
                tries++;
                $.mobile.loading('show');
                window.setTimeout(function () { HG.WebApp.GroupModules.ZWave_NodeInfoRequest(callback); }, 1000);
            }
            else {
                $.mobile.loading('hide');
            }
        };
        $.mobile.loading('show');
        HG.WebApp.GroupModules.ZWave_NodeInfoRequest(callback);
    }
    //
    /*
    var wakeupinterval = HG.WebApp.Utility.GetModulePropertyByName(module, "ZWaveNode.WakeUpInterval");
    if (wakeupinterval != null)
    {
        wakeupinterval = wakeupinterval.Value;
    }
    else
    {
        wakeupinterval = "";
    }
    $('#configurepage_OptionZWave').find('input[data-module-prop="WakeUpInterval"]').val(wakeupinterval);
    */
    //
    /*
    var association = HG.WebApp.Utility.GetModulePropertyByName(module, "ZWaveNode.Associations." + $('#configurepage_OptionZWave').find('input[data-module-prop="AssociationId"]').val());
    if (association != null)
    {
        association = association.Value;
    }
    else
    {
        association = "";
    }
    $('#configurepage_OptionZWave').find('input[data-module-prop="AssociationValue"]').val(association);
    */
    //
    /*
    var variable = HG.WebApp.Utility.GetModulePropertyByName(module, "ZWaveNode.Variables." + $('#configurepage_OptionZWave').find('input[data-module-prop="VariableId"]').val());
    if (variable != null)
    {
        variable = variable.Value;
    }
    else
    {
        variable = "";
    }
    $('#configurepage_OptionZWave').find('input[data-module-prop="VariableValue"]').val(variable);
    */
    //

    var devinfo = {};

    if (manufacturerspec != null && nodeVersion != null) {
        jQuery.ajaxSetup({ cache: true });
        var version = '';
        try {
            var versionObj = JSON.parse(nodeVersion.Value);
            version = formatAppVersion(versionObj.ApplicationVersion) + '.' + formatAppVersion(versionObj.ApplicationSubVersion);
        } catch (e) {

        }

        $.mobile.loading('show', { text: 'Querying Pepper1 DB...', textVisible: true });
        $.get('/' + HG.WebApp.Data.ServiceKey + '/HomeAutomation.ZWave/1/Db.GetDevice/' + manufacturerspec.Value.toLowerCase() + '/' + version, function (data) {

            $.mobile.loading('hide');
            var responseData = JSON.parse(data.ResponseValue);
            devinfo = responseData[0];
            if (typeof devinfo === 'undefined') {
                // TODO: notify user that device info wasn't found in pepper1db
                return;
            }

            var znodeDesc = devinfo.ZWaveDevice.deviceDescription;
            var zwaveNode = {
                description: Pepper1Db_getLocaleText(znodeDesc.description),
                wakeupNote: Pepper1Db_getLocaleText(znodeDesc.wakeupNote),
                inclusionNote: Pepper1Db_getLocaleText(znodeDesc.inclusionNote),
                productName: znodeDesc.productName,
                brandName: znodeDesc.brandName,
                productLine: znodeDesc.productLine,
                associationGroups: (typeof devinfo.ZWaveDevice.assocGroups != 'undefined'
                    ? Pepper1Db_getArray(devinfo.ZWaveDevice.assocGroups.assocGroup, 'assocGroup')
                    : []),
                configParams: (typeof devinfo.ZWaveDevice.configParams != 'undefined'
                    ? Pepper1Db_getArray(devinfo.ZWaveDevice.configParams.configParam, 'configParam')
                    : []),
                imageUrl: ''
            }

            var addinfo = '';
            var prodline = '';
            if (typeof (zwaveNode.productLine) != 'undefined' && zwaveNode.productLine !== '') {
                prodline = ' (' + zwaveNode.productLine + ')';
            }
            if (zwaveNode.productName !== '') {
                addinfo += '<p style="font-weight:normal;font-size:12pt"><strong>' + zwaveNode.productName + '</strong>' + prodline + '<br /><em>' + zwaveNode.description + '</em></p>';
            }
            if (typeof (devinfo.ZWaveDevice.resourceLinks) != 'undefined' && typeof (devinfo.ZWaveDevice.resourceLinks.deviceImage) != 'undefined') {
                zwaveNode.imageUrl = devinfo.ZWaveDevice.resourceLinks.deviceImage['@url'];
            }

            $('#opt-zwave-nodeinformation-overview').html(addinfo + infotext);
            if (zwaveNode.imageUrl !== '') {
                $('#opt-zwave-nodeinformation-overview').append('<img src="' + zwaveNode.imageUrl + '" height="100" style="position:absolute; top:25px; right:10px; border:solid 2px; padding:1px">');
            }


            //$('#configassoc-gid').attr('min', 1);
            //$('#configassoc-gid').attr('max', zwaveNode.associationGroups.length);
            //alert(zwaveNode.associationGroups[0].number);
            //alert(zwaveNode.associationGroups[0].description);

            // show groups list
            $('#opt-zwave-association-groupinput').hide();
            $('#opt-zwave-association-grouplist').show();
            // populate groups list
            $('#opt-zwave-association-groupselect').selectmenu();
            $('#opt-zwave-association-groupselect').empty();
            var opt;
            var name;
            var desc;
            for (var g = 0; g < zwaveNode.associationGroups.length; g++) {
                name = (typeof (zwaveNode.associationGroups[g].name) != 'undefined' ? zwaveNode.associationGroups[g].name : '');
                desc = (typeof (zwaveNode.associationGroups[g].description) != 'undefined' ? zwaveNode.associationGroups[g].description : '');
                opt = $('<option/>');
                opt.attr('value', zwaveNode.associationGroups[g].number);
                opt.attr('data-context-name', name);
                opt.attr('data-context-description', desc);
                opt.html(zwaveNode.associationGroups[g].number);
                //
                if (g === 0) {
                    $('#opt-zwave-association-groupdescription').html('<strong>' + name + '</strong>' + (name !== '' ? '<br/>' : '') + '<em>' + desc + '</em>');
                    $('#configassoc-gid').val(zwaveNode.associationGroups[g].number);
                }
                //
                $('#opt-zwave-association-groupselect').append(opt);
            }

            $('#opt-zwave-association-groupselect').selectmenu('refresh', true);
            $('#opt-zwave-association-groupselect').bind('change', function () {
                name = $(this).find(':selected').attr('data-context-name');
                desc = $(this).find(':selected').attr('data-context-description');
                $('#opt-zwave-association-groupdescription').html('<strong>' + name + '</strong>' + (name !== '' ? '<br/>' : '') + '<em>' + desc + '</em>');
                $('#configassoc-gid').val($(this).find(':selected').val());
            });

            // show variables list
            $('#opt-zwave-configuration-varinput').hide();
            $('#opt-zwave-configuration-varslist').show();
            // populate variables list
            $('#opt-zwave-configuration-varselect').selectmenu();
            $('#opt-zwave-configuration-varselect').empty();
            for (var p = 0; p < zwaveNode.configParams.length; p++) {
                name = (typeof (zwaveNode.configParams[p].name) != 'undefined' ? zwaveNode.configParams[p].name : '');
                desc = (typeof (zwaveNode.configParams[p].description) != 'undefined' ? zwaveNode.configParams[p].description : '');
                //
                if (zwaveNode.configParams[p].values.length > 0) {
                    desc += '<br><u>Accepted values</u><br>';
                    for (var v = 0; v < zwaveNode.configParams[p].values.length; v++) {
                        var value = zwaveNode.configParams[p].values[v];
                        var valuedesc = '';
                        if (typeof (value.description) != 'undefined' && value.description !== '') {
                            valuedesc = ' : <em>' + value.description + '</em>';
                        }
                        if (value.to === value.from) {
                            desc += '&nbsp; <b>' + value.from + '</b>' + valuedesc + '<br>';
                        }
                        else {
                            desc += '&nbsp; from <b>' + value.from + '</b> to <b>' + value.to + '</b>' + valuedesc + '<br>';
                        }
                    }
                    //+zwaveNode.configParams[p].values.length;
                }
                //
                opt = $('<option/>');
                opt.attr('value', zwaveNode.configParams[p].number);
                opt.attr('data-context-name', name);
                opt.attr('data-context-description', desc);
                opt.html(zwaveNode.configParams[p].number);
                //
                if (p === 0) {
                    $('#opt-zwave-configuration-vardescription').html('<strong>' + name + '</strong>' + (name !== '' ? '<br/>' : '') + '<em>' + desc + '</em>');
                    $('#configvar-id').val(zwaveNode.configParams[p].number);
                }
                //
                $('#opt-zwave-configuration-varselect').append(opt);
            }

            $('#opt-zwave-configuration-varselect').selectmenu('refresh', true);
            $('#opt-zwave-configuration-varselect').change(function () {
                name = $(this).find(':selected').attr('data-context-name');
                desc = $(this).find(':selected').attr('data-context-description');
                $('#opt-zwave-configuration-vardescription').html('<strong>' + name + '</strong>' + (name !== '' ? '<br/>' : '') + '<em>' + desc + '</em>');
                $('#configvar-id').val($(this).find(':selected').val());
            });

            //if (callback != null) callback(widgetobj);

        });
        jQuery.ajaxSetup({ cache: false });

    } else {
        zwave_ManufacturerSpecificGet();
    }
}

formatAppVersion = function (val) {
    var str = val.toString();
    return str.length === 2 ? str : '0' + str;
}

formatNodeVersion = function (nodeVersion) {
    var str = '';
    try {
        var versionObj = JSON.parse(nodeVersion);
        for (var prop in versionObj) {
            if (versionObj.hasOwnProperty(prop)) {
                str += prop + ': ' + versionObj[prop] + ', ';
            }
        }
        str = str.substring(0, str.length - 2);
    } catch (e) { // for compatibility with old styled node version value
        str = nodeVersion;
    }

    return str;
}


// TODO: Refactor all of the following methods to stay in "HG.Ext.ZWave." domain



HG.WebApp.GroupModules.ZWave_AssociationGet = function () {
    $('#opt-zwave-association-label').html('Nodes Id in this group = ? (querying node...)');
    zwave_AssociationGet($('#configurepage_OptionZWave_id').val(), $('#configassoc-gid').val(), function (res) {
        if (res === 'ERR_TIMEOUT') {
            $('#opt-zwave-association-label').html('Nodes Id in this group = ? (operation timeout!)');
        }
        else {
            $('#opt-zwave-association-label').html('Nodes Id in this group = ' + res);
        }
    });
};

HG.WebApp.GroupModules.ZWave_ConfigVariableGet = function () {
    $('#opt-zwave-configvar-label').html('Variable Value = ? (querying node...)');
    zwave_ConfigurationParameterGet($('#configurepage_OptionZWave_id').val(), $('#configvar-id').val(), function (res) {
        if (res === 'ERR_TIMEOUT') {
            $('#opt-zwave-configvar-label').html('Variable Value = ? (operation timeout!)');
        }
        else {
            $('#opt-zwave-configvar-label').html('Variable Value = ' + res);
        }
    });
};

HG.WebApp.GroupModules.ZWave_NodeNeighborUpdate = function () {
    $('#opt-zwave-heal-label').html('Requesting Neighbor Update ...');
    $.mobile.loading('show');
    zwave_NodeNeighborUpdate($('#configurepage_OptionZWave_id').val(), function (res) {
        if (res === 'ERR_TIMEOUT') {
            $('#opt-zwave-heal-label').html('Healing operation timeout!');
        }
        else {
            $('#opt-zwave-heal-label').html('Routing Info = ' + res);
        }
        $.mobile.loading('hide');
    });
};

HG.WebApp.GroupModules.ZWave_BasicGet = function () {
    $('#opt-zwave-basic-label').html('Basic Value = ? (querying node...)');
    zwave_BasicGet($('#configurepage_OptionZWave_id').val(), function (res) {
        if (res === 'ERR_TIMEOUT') {
            $('#opt-zwave-basic-label').html('Basic Value = ? (operation timeout!)');
        }
        else {
            $('#opt-zwave-basic-label').html('Basic Value = ' + res);
        }
    });
};

HG.WebApp.GroupModules.ZWave_BatteryGet = function () {
    $('#opt-zwave-battery-label').html('Battery Level = ? (querying node...)');
    zwave_BatteryGet($('#configurepage_OptionZWave_id').val(), function (res) {
        if (res === 'ERR_TIMEOUT') {
            $('#opt-zwave-battery-label').html('Battery Level = ? (operation timeout!)');
        }
        else {
            $('#opt-zwave-battery-label').html('Battery Level = ' + res + '%');
        }
    });
};

HG.WebApp.GroupModules.ZWave_DoorLockGet = function () {
    $('#opt-zwave-doorlock-label').html('Door Lock Status = ? (querying node...)');
    zwave_DoorLockGet($('#configurepage_OptionZWave_id').val(), function (res) {
        if (res === 'ERR_TIMEOUT') {
            $('#opt-zwave-doorlock-label').html('Door Lock Status = ? (operation timeout!)');
        }
        else {
            $('#opt-zwave-doorlock-label').html('Door Lock Status = ' + res);
        }
    });
};

HG.WebApp.GroupModules.ZWave_WakeUpGet = function () {
    $('#opt-zwave-wakeup-label').html('Wake Up Interval = ? (querying node...)');
    zwave_WakeUpGet($('#configurepage_OptionZWave_id').val(), function (res) {
        if (res === 'ERR_TIMEOUT') {
            $('#opt-zwave-wakeup-label').html('Wake Up Interval = ? (operation timeout!)');
        }
        else {
            $('#opt-zwave-wakeup-label').html('Wake Up Interval = ' + res + 's');
        }
    });
};

HG.WebApp.GroupModules.ZWave_MeterGet = function (type) {
    zwave_MeterGet($('#configurepage_OptionZWave_id').val(), type, function () { });
};

HG.WebApp.GroupModules.ZWave_MeterReset = function () {
    zwave_MeterReset($('#configurepage_OptionZWave_id').val(), function () { });
};

HG.WebApp.GroupModules.ZWave_NodeInfoRequest = function (callback) {
    var zwaveNodeId = $('#configurepage_OptionZWave_id').val();
    $('#opt-zwave-manufacturerspecs-label').html('Manufacturer Specific = ? (querying node...)');
    zwave_ManufacturerSpecificGet(zwaveNodeId, function (res) {
        if (res === 'ERR_TIMEOUT') {
            $('#opt-zwave-manufacturerspecs-label').html('Manufacturer Specific = ? (operation timeout!)');
            if (callback != null) callback(false);
        }
        else {
            var mspecs = res;
            zwave_VersionReport(zwaveNodeId, function (result) {
                var nodeVersion = result;
                $('#opt-zwave-manufacturerspecs-label').html('Manufacturer Specific = ' + mspecs + ' (querying nodeinfo)');
                $('#opt-zwave-versionreport-label').html('SW Version: ' + nodeVersion);
                zwave_NodeInformationGet(zwaveNodeId, function (res) {
                    if (res === 'ERR_TIMEOUT') {
                        $('#opt-zwave-manufacturerspecs-label').html('Manufacturer Specific = ' + mspecs + ' (operation timeout!)');
                        if (callback != null) callback(false);
                    }
                    else {
                        //TODO: find a better way of refreshing options data
                        HG.Configure.Modules.List(function (data) {
                            HG.WebApp.Data.Modules = eval(data);
                            HG.WebApp.GroupModules.ShowModuleOptions('HomeAutomation.ZWave', zwaveNodeId);
                        });
                        //
                        if (callback != null) callback(true);
                    }
                });
            });
        }
    });
};

HG.WebApp.GroupModules.SwitchBinaryParameterGet = function () {
    $('#opt-zwave-switchbinary-label').html('Switch Binary = ? (querying node...)');
    zwave_SwitchBinaryParameterGet($('#configurepage_OptionZWave_id').val(), $('#instancevar-id').val(), function (res) {
        if (res === 'ERR_TIMEOUT') {
            $('#opt-zwave-switchbinary-label').html('Switch Binary = ? (operation timeout!)');
        }
        else {
            $('#opt-zwave-switchbinary-label').html('Switch Binary = ' + (res === '0' ? 'Off' : 'On'));
        }
    });
}

HG.WebApp.GroupModules.SwitchMultiLevelParameterGet = function () {
    $('#opt-zwave-switchmulti-label').html('Switch MultiLevel = ? (querying node...)');
    zwave_SwitchMultilevelParameterGet($('#configurepage_OptionZWave_id').val(), $('#instancevar-id').val(), function (res) {
        if (res === 'ERR_TIMEOUT') {
            $('#opt-zwave-switchmulti-label').html('Switch MultiLevel = ? (operation timeout!)');
        }
        else {
            $('#opt-zwave-switchmulti-label').html('Switch MultiLevel = ' + Math.round(parseFloat(res.replace(',', '.') * 99)));
        }
    });
};

HG.WebApp.GroupModules.SensorBinaryParameterGet = function () {
    $('#opt-zwave-sensorbinary-label').html('Sensor Binary = ? (querying node...)');
    zwave_SensorBinaryParameterGet($('#configurepage_OptionZWave_id').val(), $('#instancevar-id').val(), function (res) {
        if (res === 'ERR_TIMEOUT') {
            $('#opt-zwave-sensorbinary-label').html('Sensor Binary = ? (operation timeout!)');
        }
        else {
            $('#opt-zwave-sensorbinary-label').html('Sensor Binary = ' + (res === '0' ? 'Off' : 'On'));
        }
    });
};

HG.WebApp.GroupModules.SensorMultiLevelParameterGet = function () {
    $('#opt-zwave-sensormulti-label').html('Sensor MultiLevel = ? (querying node...)');
    zwave_SensorMultilevelParameterGet($('#configurepage_OptionZWave_id').val(), $('#instancevar-id').val(), function (res) {
        if (res === 'ERR_TIMEOUT') {
            $('#opt-zwave-sensormulti-label').html('Sensor MultiLevel = ? (operation timeout!)');
        }
        else {
            $('#opt-zwave-sensormulti-label').html('Sensor MultiLevel = ' + res);
        }
    });
};



// TODO: Refactor all of the following methods to stay in "HG.Ext.ZWave.Api" domain



function zwave_AssociationGet(nodeid, groupid, callback) {
    $.get('/' + HG.WebApp.Data.ServiceKey + '/HomeAutomation.ZWave/' + nodeid + '/Association.Get/' + groupid + '/', function (data) {
        if (typeof callback != 'undefined' && callback != null) {
            callback(data.ResponseValue);
        }
    });
}
function zwave_AssociationSet(nodeid, groupid, targetid) {
    $.get('/' + HG.WebApp.Data.ServiceKey + '/HomeAutomation.ZWave/' + nodeid + '/Association.Set/' + groupid + '/' + targetid + '/', function () { });
}
function zwave_AssociationRemove(nodeid, groupid, targetid) {
    $.get('/' + HG.WebApp.Data.ServiceKey + '/HomeAutomation.ZWave/' + nodeid + '/Association.Remove/' + groupid + '/' + targetid + '/', function () { });
}

function zwave_NodeNeighborUpdate(nodeid, callback) {
    $.get('/' + HG.WebApp.Data.ServiceKey + '/HomeAutomation.ZWave/' + nodeid + '/Controller.NodeNeighborUpdate/', function (data) {
        if (typeof callback != 'undefined' && callback != null) {
            callback(data.ResponseValue);
        }
    });
}

function zwave_BasicGet(nodeid, callback) {
    $.get('/' + HG.WebApp.Data.ServiceKey + '/HomeAutomation.ZWave/' + nodeid + '/Basic.Get/', function (data) {
        if (typeof callback != 'undefined' && callback != null) {
            callback(data.ResponseValue);
        }
    });
}
function zwave_BasicSet(nodeid, value) {
    $.get('/' + HG.WebApp.Data.ServiceKey + '/HomeAutomation.ZWave/' + nodeid + '/Basic.Set/' + value + '/', function () { });
}

function zwave_BatteryGet(nodeid, callback) {
    $.get('/' + HG.WebApp.Data.ServiceKey + '/HomeAutomation.ZWave/' + nodeid + '/Battery.Get/', function (data) {
        if (typeof callback != 'undefined' && callback != null) {
            callback(data.ResponseValue);
        }
    });
}

function zwave_DoorLockGet(nodeid, callback) {
    $.get('/' + HG.WebApp.Data.ServiceKey + '/HomeAutomation.ZWave/' + nodeid + '/DoorLock.Get/', function (data) {
        if (typeof callback != 'undefined' && callback != null) {
            callback(data.ResponseValue);
        }
    });
}

function zwave_WakeUpGet(nodeid, callback) {
    $.get('/' + HG.WebApp.Data.ServiceKey + '/HomeAutomation.ZWave/' + nodeid + '/WakeUp.Get/', function (data) {
        if (typeof callback != 'undefined' && callback != null) {
            callback(data.ResponseValue);
        }
    });
}
function zwave_WakeUpSet(nodeid, opt1, opt2) {
    $.get('/' + HG.WebApp.Data.ServiceKey + '/HomeAutomation.ZWave/' + nodeid + '/WakeUp.Set/' + opt1 + '/' + opt2 + '/', function () { });
}


function zwave_MeterGet(nodeid, type, callback) {
    $.get('/' + HG.WebApp.Data.ServiceKey + '/HomeAutomation.ZWave/' + nodeid + '/Meter.Get/' + type + '/', function () {
        if (typeof callback != 'undefined' && callback != null) {
            callback();
        }
    });
}
function zwave_MeterReset(nodeid, callback) {
    $.get('/' + HG.WebApp.Data.ServiceKey + '/HomeAutomation.ZWave/' + nodeid + '/Meter.Reset/', function () {
        if (typeof callback != 'undefined' && callback != null) {
            callback();
        }
    });
}


function zwave_ConfigurationParameterGet(nodeid, varid, callback) {
    $.get('/' + HG.WebApp.Data.ServiceKey + '/HomeAutomation.ZWave/' + nodeid + '/Config.ParameterGet/' + varid + '/', function (data) {
        if (typeof callback != 'undefined' && callback != null) {
            callback(data.ResponseValue);
        }
    });
}
function zwave_ConfigurationParameterSet(nodeid, varid, value) {
    $.get('/' + HG.WebApp.Data.ServiceKey + '/HomeAutomation.ZWave/' + nodeid + '/Config.ParameterSet/' + varid + '/' + value + '/', function () { });
}


function zwave_SwitchBinaryParameterGet(nodeid, varid, callback) {
    $.get('/' + HG.WebApp.Data.ServiceKey + '/HomeAutomation.ZWave/' + nodeid + '/MultiInstance.Get/Switch.Binary/' + varid + '/', function (data) {
        if (typeof callback != 'undefined' && callback != null) {
            callback(data.ResponseValue);
        }
    });
}
function zwave_SwitchBinaryParameterSet(nodeid, varid, value) {
    $.get('/' + HG.WebApp.Data.ServiceKey + '/HomeAutomation.ZWave/' + nodeid + '/MultiInstance.Set/Switch.Binary/' + varid + '/' + value + '/', function () { });
}


function zwave_SwitchMultilevelParameterGet(nodeid, varid, callback) {
    $.get('/' + HG.WebApp.Data.ServiceKey + '/HomeAutomation.ZWave/' + nodeid + '/MultiInstance.Get/Switch.MultiLevel/' + varid + '/', function (data) {
        if (typeof callback != 'undefined' && callback != null) {
            callback(data.ResponseValue);
        }
    });
}
function zwave_SwitchMultilevelParameterSet(nodeid, varid, value) {
    $.get('/' + HG.WebApp.Data.ServiceKey + '/HomeAutomation.ZWave/' + nodeid + '/MultiInstance.Set/Switch.MultiLevel/' + varid + '/' + value + '/', function () { });
}


function zwave_SensorBinaryParameterGet(nodeid, varid, callback) {
    $.get('/' + HG.WebApp.Data.ServiceKey + '/HomeAutomation.ZWave/' + nodeid + '/MultiInstance.Get/Sensor.Binary/' + varid + '/', function (data) {
        if (typeof callback != 'undefined' && callback != null) {
            callback(data.ResponseValue);
        }
    });
}
function zwave_SensorMultilevelParameterGet(nodeid, varid, callback) {
    $.get('/' + HG.WebApp.Data.ServiceKey + '/HomeAutomation.ZWave/' + nodeid + '/MultiInstance.Get/Sensor.MultiLevel/' + varid + '/', function (data) {
        if (typeof callback != 'undefined' && callback != null) {
            callback(data.ResponseValue);
        }
    });
}

function zwave_NodeAdd(callback) {
    $.get('/' + HG.WebApp.Data.ServiceKey + '/HomeAutomation.ZWave/1/Controller.NodeAdd/', function (data) { callback(data); });
}
function zwave_NodeRemove(callback) {
    $.get('/' + HG.WebApp.Data.ServiceKey + '/HomeAutomation.ZWave/1/Controller.NodeRemove/', function (data) { callback(data); });
}

function zwave_ManufacturerSpecificGet(nodeid, callback) {
    $.get('/' + HG.WebApp.Data.ServiceKey + '/HomeAutomation.ZWave/' + nodeid + '/ManufacturerSpecific.Get/', function (data) {
        if (typeof callback != 'undefined' && callback != null) {
            callback(data.ResponseValue);
        }
    });
}

function zwave_VersionReport(nodeid, callback) {
    $.get('/' + HG.WebApp.Data.ServiceKey + '/HomeAutomation.ZWave/' + nodeid + '/Version.Report/', function (data) {
        if (typeof callback != 'undefined' && callback != null) {
            callback(data.ResponseValue);
        }
    });
}

function zwave_NodeInformationGet(nodeid, callback) {
    $.get('/' + HG.WebApp.Data.ServiceKey + '/HomeAutomation.ZWave/' + nodeid + '/NodeInfo.Get/', function (data) {
        if (typeof callback != 'undefined' && callback != null) {
            callback(data.ResponseValue);
        }
    });
}

function _zwavedelayupdate(nodeid) {
    //HG.WebApp.GroupModules.ShowModuleOptions("HomeAutomation.ZWave", nodeid);
    window.setTimeout(function () {
        //HG.Configure.Modules.List(function(data){
        //HG.WebApp.Data.Modules = eval(data);
        HG.WebApp.GroupModules.ShowModuleOptions('HomeAutomation.ZWave', nodeid);
        //});
    }, 200);
}


/* Pepper1 DB */
function zwave_DbUpdate(nodeid, callback) {
    $.get('/' + HG.WebApp.Data.ServiceKey + '/HomeAutomation.ZWave/' + nodeid + '/Db.Update/', function (data) {
        if (typeof callback != 'undefined' && callback != null) {
            callback(data.ResponseValue);
        }
    });
}

function zwave_DbGetDevice(nodeid, callback) {
    $.get('/' + HG.WebApp.Data.ServiceKey + '/HomeAutomation.ZWave/' + nodeid + '/Db.GetDevice/', function (data) {
        if (typeof callback != 'undefined' && callback != null) {
            callback(data.ResponseValue);
        }
    });
}


function Pepper1Db_getConfigValue(zvalue) {
    var v = { from: '0', to: '0', description: '' };
    if (typeof zvalue !== 'undefined' && zvalue !== null) {
        v.from = parseInt(zvalue['@from'], 16);
        v.to = parseInt(zvalue['@to'], 16);
        v.description = Pepper1Db_getLocaleText(zvalue.description);
    }
    return v;
}

function Pepper1Db_getConfigParam(zparam) {
    var p = { number: 1, valueType: '', valueSize: 1, valueDefault: '', name: '', description: '', values: [] };
    p.number = zparam['@number'];
    p.valueType = zparam['@type'];
    p.valueSize = zparam['@size'];
    p.valueDefault = zparam['@default'];
    p.name = Pepper1Db_getLocaleText(zparam.name);
    p.description = Pepper1Db_getLocaleText(zparam.description);
    p.values = Pepper1Db_getArray(zparam.value, 'configValue');
    return p;
}

function Pepper1Db_getAssociationGroup(zgroup) {
    var g = { number: 1, maxNodes: 1, description: '' };
    g.number = zgroup['@number'];
    g.maxNodes = zgroup['@maxNodes'];
    g.name = (typeof (zgroup.name) != 'undefined' ? Pepper1Db_getLocaleText(zgroup.name) : '');
    g.description = Pepper1Db_getLocaleText(zgroup.description);
    return g;
}

function Pepper1Db_getArray(zarray, type) {
    var retobj = [];
    if (zarray instanceof Array) {
        for (var z = 0; z < zarray.length; z++) {
            switch (type) {
                case 'configParam':
                    retobj.push(Pepper1Db_getConfigParam(zarray[z]));
                    break;
                case 'configValue':
                    retobj.push(Pepper1Db_getConfigValue(zarray[z]));
                    break;
                case 'assocGroup':
                    retobj.push(Pepper1Db_getAssociationGroup(zarray[z]));
                    break;
            }
        }
    }
    else {
        switch (type) {
            case 'configParam':
                retobj.push(Pepper1Db_getConfigParam(zarray));
                break;
            case 'configValue':
                retobj.push(Pepper1Db_getConfigValue(zarray));
                break;
            case 'assocGroup':
                retobj.push(Pepper1Db_getAssociationGroup(zarray));
                break;
        }
    }
    return retobj;
}

function Pepper1Db_getLocaleText(zproperty) {
    var userLang = (navigator.language) ? navigator.language : navigator.userLanguage;
    var lang = userLang.toLowerCase().substring(0, 2);
    // if lang is array
    if ($.isArray(zproperty.lang)) {
        var item = zproperty.lang.filter(function (obj) {
            return obj['@xml:lang'] === lang;
        });
        if (item.length === 0) {
            item = zproperty.lang.filter(function (obj) {
                return obj['@xml:lang'] === 'en';
            });
        }
        return item[0]['#text'];
    }
    // else
    return zproperty.lang['#text'];
}