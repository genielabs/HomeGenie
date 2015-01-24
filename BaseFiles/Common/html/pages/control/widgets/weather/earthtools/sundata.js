[{
    Name: "HomeSeer HSM100 Widget",
    Author: "Generoso Martello",
    Version: "2013-06-24",

    GroupName: '',
    IconImage: 'pages/control/widgets/weather/earthtools/images/earthtools.png',
    StatusText: '',
    Description: '',
    Initialized: false,

    RenderView: function (cuid, module) {
        var container = $(cuid);
        var widget = container.find('[data-ui-field=widget]');
        //
        if (!this.Initialized) {
            this.Initialized = true;
            // settings button
            widget.find('[data-ui-field=settings]').on('click', function () {
                HG.WebApp.ProgramEdit._CurrentProgram.Domain = module.Domain;
                HG.WebApp.ProgramEdit._CurrentProgram.Address = module.Address;
                HG.WebApp.ProgramsList.UpdateOptionsPopup();
            });
        }
        //
        var latitude = HG.WebApp.Utility.GetModulePropertyByName(module, "ConfigureOptions.Latitude").Value;
        widget.find('[data-ui-field=latitude]').html(latitude);
        var longitude = HG.WebApp.Utility.GetModulePropertyByName(module, "ConfigureOptions.Longitude").Value;
        widget.find('[data-ui-field=longitude]').html(longitude);
        //
        var sunrise = HG.WebApp.Utility.GetModulePropertyByName(module, "Astronomy.Sunrise").Value;
        if (sunrise == '') {
            widget.find('[data-ui-field=last_updated_value]').html('Waiting for data...');
        }
        else {
            widget.find('[data-ui-field=a_sunrise]').html(sunrise);
            //
            var sunset = HG.WebApp.Utility.GetModulePropertyByName(module, "Astronomy.Sunset").Value;
            widget.find('[data-ui-field=a_sunset]').html(sunset);
            //
            var mt_civil = HG.WebApp.Utility.GetModulePropertyByName(module, "EarthTool.Morning.Twilight.Civil").Value;
            widget.find('[data-ui-field=cm_twilight]').html(mt_civil);
            var mt_nautical = HG.WebApp.Utility.GetModulePropertyByName(module, "EarthTool.Morning.Twilight.Nautical").Value;
            widget.find('[data-ui-field=nm_twilight]').html(mt_nautical);
            var mt_astronomy = HG.WebApp.Utility.GetModulePropertyByName(module, "EarthTool.Morning.Twilight.Astronomical").Value;
            widget.find('[data-ui-field=am_twilight]').html(mt_astronomy);
            //
            var et_civil = HG.WebApp.Utility.GetModulePropertyByName(module, "EarthTool.Evening.Twilight.Civil").Value;
            widget.find('[data-ui-field=ce_twilight]').html(et_civil);
            var et_nautical = HG.WebApp.Utility.GetModulePropertyByName(module, "EarthTool.Evening.Twilight.Nautical").Value;
            widget.find('[data-ui-field=ne_twilight]').html(et_nautical);
            var et_astronomy = HG.WebApp.Utility.GetModulePropertyByName(module, "EarthTool.Evening.Twilight.Astronomical").Value;
            widget.find('[data-ui-field=ae_twilight]').html(et_astronomy);
            //
            widget.find('[data-ui-field=last_updated_value]').html('Sun Data');
        }
    }


}]