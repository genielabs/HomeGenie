HG.WebApp.SystemSettings = HG.WebApp.SystemSettings || new function () { var $$ = this;

    $$.PageId = 'page_configure_interfaces';
    $$.Interfaces = $$.Interfaces || {};

    $$.InitializePage = function () {
        var page = $('#' + $$.PageId);
        var importPopup = page.find('[data-ui-field=import-popup]');
        var importButton = page.find('[data-ui-field=interface_install]');
        var importForm = page.find('[data-ui-field=import-form]');
        var downloadButton = page.find('[data-ui-field=download-btn]');
        var downloadUrl = page.find('[data-ui-field=download-url]');
        var uploadButton = page.find('[data-ui-field=upload-btn]');
        var uploadFile = page.find('[data-ui-field=upload-file]');
        var uploadFrame = page.find('[data-ui-field=upload-frame]');
        page.on('pageinit', function (e) {
            // initialize controls used in this page
            importPopup.popup();
        });
        page.on('pagebeforeshow', function (e) {
            HG.Automation.Programs.List(function () {
                HG.Configure.Groups.List('Automation', function () {
                    $$.ListInterfaces();
                });
            });
        });
        page.on('pageshow', function (e) {
            page.find('[data-locale-id=configure_program_bbaractions]').qtip({
                content: {
                    text: HG.WebApp.Locales.GetLocaleString('configure_system_installtip_description', 'Go to the Package Manager to install additional features.'),
                },
                show: {event: false, ready: true, delay: 1000},
                events: {
                    hide: function () {
                        $(this).qtip('destroy');
                    }
                },
                hide: {event: false, inactive: 3000},
                style: {classes: 'qtip-red qtip-shadow qtip-rounded qtip-bootstrap'},
                position: {my: 'bottom center', at: 'top center'}
            });
        });
        importPopup.on('popupafteropen', function (event) {
            importButton.removeClass('ui-btn-active');
        });
        importButton.on('click', function () {
            setTimeout(function () {
                importPopup.popup('open');
            }, 500);
        });
        downloadButton.on('click', function () {
            if (downloadUrl.val() == '') {
                alert('Insert add-on package URL');
                downloadUrl.parent().stop().animate({borderColor: "#FF5050"}, 250)
                    .animate({borderColor: "#FFFFFF"}, 250)
                    .animate({borderColor: "#FF5050"}, 250)
                    .animate({borderColor: "#FFFFFF"}, 250);
            } else {
                importPopup.popup('close');
                setTimeout(function () {
                    $.mobile.loading('show', {text: 'Downloading, please wait...', textVisible: true, html: ''});
                    $.get('/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/Interface.Import/' + encodeURIComponent(downloadUrl.val()), function (data) {
                        $.mobile.loading('hide');
                        downloadUrl.val('');
                        var response = $.parseJSON(arguments[2].responseText);
                        $$.AddonInstall(response.ResponseValue);
                    });
                }, 1000);
            }
        });
        uploadButton.on('click', function () {
            if (uploadFile.val() == '') {
                alert('Select a file to import first');
                uploadFile.parent().stop().animate({borderColor: "#FF5050"}, 250)
                    .animate({borderColor: "#FFFFFF"}, 250)
                    .animate({borderColor: "#FF5050"}, 250)
                    .animate({borderColor: "#FFFFFF"}, 250);
            } else {
                importPopup.popup('close');
                $.mobile.loading('show', {text: 'Uploading, please wait...', textVisible: true, html: ''});
                importForm.submit();
            }
        });
        uploadFrame.bind('load', function () {
            $.mobile.loading('hide');
            // import completed...
            uploadFile.val('');
            var response = uploadFrame[0].contentWindow.document.body;
            if (typeof response != 'undefined' && response != '' && (response.textContent || response.innerText)) {
                try {
                    response = $.parseJSON(response.textContent || response.innerText);
                    $$.AddonInstall(response.ResponseValue);
                } catch (e) {
                }
            }
        });
    };

    $$.AddonInstall = function (text) {
        var urlRegex = /(https?:\/\/[^\s]+)/g;
        text = text.replace(urlRegex, '<a href="$1" target="_blank">$1</a>');
        HG.WebApp.Utility.ConfirmPopup(HG.WebApp.Locales.GetLocaleString('systemsettings_addonsinstall_title', 'Install add-on?'), '<pre>' + text + '</pre>', function (confirm) {
            if (confirm) {
                $.mobile.loading('show', {text: 'Installing, please wait...', textVisible: true, html: ''});
                $.get('/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/Interface.Install', function (data) {
                    $$.ListInterfaces();
                    $.mobile.loading('hide');
                });
            }
        });
    };

    $$.ListInterfaces = function () {
        var page = $('#' + $$.PageId);
        var interfaceList = page.find('[data-ui-field=interface_list]');
        HG.Configure.Interfaces.ListConfig(function (ifaceList) {
            interfaceList.empty();
            interfaceList.append('<h3>Interfaces (MIG)</h3>');
            $.each(ifaceList, function (k, v) {
                var domain = v.Domain;
                var name = domain.substring(domain.lastIndexOf('.') + 1);
                var item = $('<div data-role="collapsible" data-inset="true" class="ui-mini" />');
                var itemHeader = $('<h3><span data-ui-field="title">' + name + '</span><img src="images/interfaces/' + name.toLowerCase() + '.png" style="position:absolute;right:8px;top:6px"></h3>');
                item.append(itemHeader);
                var configlet = $('<p />');
                item.append(configlet);
                configlet.load('pages/configure/interfaces/configlet/' + name.toLowerCase() + '.html', function () {
                    var displayName = name;
                    if ($$.Interfaces[domain].Localize) {
                        $$.Interfaces[domain].Localize();
                        displayName = HG.WebApp.Locales.GetLocaleString('title', name, $$.Interfaces[domain].Locale);
                    }
                    itemHeader.find('[data-ui-field=title]').html(displayName);
                    itemHeader.attr('description', v.Description);
                    if (v.Description != null && v.Description.trim() != '') {
                        itemHeader.qtip({
                            content: {
                                text: v.Description
                            },
                            show: {event: 'mouseover', ready: false, delay: 500},
                            hide: {event: 'mouseout'},
                            style: {classes: 'qtip-red qtip-shadow qtip-rounded qtip-bootstrap'},
                            position: {my: 'bottom center', at: 'top center'}
                        });
                    }
                    configlet.trigger('create');
                    $$.Interfaces[domain].Initialize();
                });
                interfaceList.append(item);
            });
            interfaceList.append('<h3>Automation Program Plugins</h3>');
            $.each(HG.WebApp.Data.AutomationGroups, function (gidx, grp) {
                var showGroup = true;
                $.each(HG.WebApp.Data.Programs, function (pidx, prg) {
                    if (prg.IsEnabled && prg.Group == grp.Name) {
                        if (HG.Automation.Programs.HasConfigurationOptions(prg.Address))
                        {
                            if (showGroup) {
                                interfaceList.append('<h4>' + grp.Name + '</h4>');
                                showGroup = false;
                            }
                            var title = HG.WebApp.Locales.GetProgramLocaleString(prg.Address, 'Title', prg.Name);
                            var desc = (prg.Description != 'undefined' && prg.Description != null ? prg.Description : '');
                            desc = HG.WebApp.Locales.GetProgramLocaleString(prg.Address, 'Description', desc).replace(/\n/g, '<br />');
                            var item = $('<div data-role="collapsible" data-inset="true" class="ui-mini" />');
                            var itemHeader = $('<h3><span data-ui-field="title">' + title + '</span></h3>');
                            item.append(itemHeader);
                            var itemHtml = '<div><div class="ui-grid-a" style="padding-top:10px;padding-bottom:10px">';
                            itemHtml += '<div class="ui-block-a" style="width:75%">' + desc + '</div>';
                            itemHtml += '<div class="ui-block-b" style="width:25%" align="right"><a data-ui-field="options_button" href="#" class="ui-btn ui-corner-all ui-btn-inline ui-btn-icon-left ui-icon-gear">' + HG.WebApp.Locales.GetLocaleString('configure_program_options', 'Options') + '</a></div>';
                            itemHtml += '</div></div>';
                            item.append(itemHtml);
                            item.find('[data-ui-field="options_button"]').on('click', function () {
                                HG.WebApp.ProgramEdit._CurrentProgram.Domain = 'HomeAutomation.HomeGenie.Automation';
                                HG.WebApp.ProgramEdit._CurrentProgram.Address = prg.Address;
                                HG.WebApp.ProgramsList.UpdateOptionsPopup();
                            });
                            interfaceList.append(item);
                        }
                    }
                });

            });
            interfaceList.trigger('create');
        });
    };

    $$.GetInterface = function (domain) {
        var iface = null;
        var interfaces = HG.WebApp.Data.Interfaces;
        if (interfaces && interfaces != 'undefined') {
            for (i = 0; i < interfaces.length; i++) {
                if (interfaces[i].Domain == domain) {
                    iface = interfaces[i];
                    break;
                }
            }
        }
        return iface;
    };

    $$.ShowPortTip = function (el) {
        $(el).qtip({
            content: {
                title: HG.WebApp.Locales.GetLocaleString('systemsettings_selectport_title'),
                text: HG.WebApp.Locales.GetLocaleString('systemsettings_selectport_text'),
                button: HG.WebApp.Locales.GetLocaleString('systemsettings_selectport_button')
            },
            show: {event: false, ready: true, delay: 1000},
            events: {
                hide: function () {
                    $(this).qtip('destroy');
                }
            },
            hide: {event: false, inactive: 3000},
            style: {classes: 'qtip-red qtip-shadow qtip-rounded qtip-bootstrap'},
            position: {my: 'left center', at: 'right center'}
        });
    };

};
