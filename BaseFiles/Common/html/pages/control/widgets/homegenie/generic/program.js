[{
    Name: "Generic Program",
    Author: "Generoso Martello",
    Version: "2013-03-31",

    GroupName: '',
    IconImage: 'pages/control/widgets/homegenie/generic/images/program.png',
    StatusText: '',
    Description: '',
    Initialized: false,

    RenderView: function (cuid, module) {
        var container = $(cuid);
        var widget = container.find('[data-ui-field=widget]');
        //
        // read some context data
        //
        this.GroupName = container.attr('data-context-group');
        //
        if (!this.Initialized) {
            this.Initialized = true;
            //
            //container.css('width', '100%');
            //container.css('float', 'right');
            //container.css('text-align', 'right');
            //container.css('horizontal-alignment', 'right');
            //
            var groupname = this.GroupName;
            widget.on('click', function () {
                HG.Automation.Programs.Toggle(module.Address, groupname, null);
            });
        }
        //
        var prog = HG.WebApp.ProgramsList.GetProgramByAddress(module.Address);
        var statuscolor = 'black';
        var status = HG.WebApp.Utility.GetModulePropertyByName(module, "Program.Status");
        if (status != null) {
            switch (status.Value) {
                case 'Enabled':
                case 'Idle':
                    statuscolor = 'yellow';
                    break;
                case 'Running':
                    statuscolor = 'green';
                    break;
                case 'Disabled':
                    statuscolor = 'black';
                    if (prog.Type.toLowerCase() != 'wizard' && prog.ScriptErrors.trim() != '' && prog.ScriptErrors.trim() != '[]') {
                        if (prog.IsEnabled) {
                            statuscolor = 'red';
                        }
                        else {
                            statuscolor = 'brown';
                        }
                    }
                    break;
            }
        }
        //
        // render widget
        //
        widget.find('[data-ui-field=name]').html(module.Name.substring(module.Name.lastIndexOf('|') + 1));
        widget.css('background-image', 'url(images/common/led_' + statuscolor + '.png)');
    }
}]