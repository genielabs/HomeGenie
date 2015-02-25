HG.VoiceControl = HG.VoiceControl || {};
HG.VoiceControl.CurrentInput = '';
HG.VoiceControl.LingoData = [];

HG.VoiceControl.Localize = function (langurl, callback) {
    // get data via ajax 
    // store it in 		HG.VoiceControl.LingoData
    // and replace locales strings in the current page
    $.ajax({
        url: langurl,
        type: 'GET',
        success: function (data) {
            HG.VoiceControl.LingoData = $.parseJSON(data);
            callback(true);
        },
        failure: function (data) {
            callback(false);
        }
    });
};

HG.VoiceControl.InterpretInput = function (sentence) {
    HG.VoiceControl.CurrentInput = sentence;
    //
    var continueparsing = true;
    while (continueparsing) {
        continueparsing = false;

        var command = HG.VoiceControl.SearchCommandMatch(false);
        var nextcommand = HG.VoiceControl.SearchCommandMatch(true);
        //
        var type = HG.VoiceControl.SearchTypeMatch(false);
        //
        var group = HG.VoiceControl.SearchGroupMatch(nextcommand.StartIndex);
        if (group != -1) {
            group = HG.WebApp.Data.Groups[group].Name;
        }
        else {
            group = '';
        }
        //
        if (command != '' && type != '') {

            var types = type.split(',');
            var groupmodules = {};
            if (group != '') {
                groupmodules = HG.Configure.Groups.GetGroupModules(group).Modules;
            }
            else {
                groupmodules = HG.WebApp.Data.Modules;
            }
            //
            for (m = 0; m < groupmodules.length; m++) {
                var module = groupmodules[m];
                for (t = 0; t < types.length; t++) {

                    if (typeof module.DeviceType != 'undefined' && types[t].toLowerCase() == module.DeviceType.toLowerCase()) {
                        HG.Control.Modules.ServiceCall(command, module.Domain, module.Address, '');
                        continueparsing = true;
                        //
                        var date = new Date();
                        var curDate = null;
                        do { curDate = new Date(); }
                        while (curDate - date < 300);
                    }

                }
            }

        }
        else {
            var module = HG.VoiceControl.SearchSubjectMatch(group, nextcommand.StartIndex);
            //
            if (command != '') {
                //alert(module.Address + ' ' + module.Domain);
                HG.Control.Modules.ServiceCall(command, module.Domain, module.Address, '');
                continueparsing = true;
            }
            //alert(group + ' ' + command + ' ' + module.Name);
        }
        //
        var date = new Date();
        var curDate = null;
        do { curDate = new Date(); }
        while (curDate - date < 300);
    }
};

HG.VoiceControl.SearchTypeMatch = function (keepsentence) {
    var type = '';
    var curmatch = { Words: '', StartIndex: -1 };
    for (s = 0; s < HG.VoiceControl.LingoData.Types.length; s++) {
        for (c = 0; c < HG.VoiceControl.LingoData.Types[s].Aliases.length; c++) {
            var res = HG.VoiceControl.FindMatchingInput(HG.VoiceControl.LingoData.Types[s].Aliases[c]);
            if (res.StartIndex != -1 && (res.StartIndex < curmatch.StartIndex || curmatch.StartIndex == -1)) {
                type = HG.VoiceControl.LingoData.Types[s].Type;
                curmatch = res;
                //	                            break;
            }
        }
    }
    //
    if (keepsentence) return curmatch;
    //
    if (curmatch.StartIndex != -1) {
        HG.VoiceControl.RemoveInputMatch(curmatch);
    }
    return type;
};

HG.VoiceControl.SearchCommandMatch = function (keepsentence) {
    var command = '';
    var curmatch = { Words: '', StartIndex: -1 };
    for (s = 0; s < HG.VoiceControl.LingoData.Commands.length; s++) {
        for (c = 0; c < HG.VoiceControl.LingoData.Commands[s].Aliases.length; c++) {
            var res = HG.VoiceControl.FindMatchingInput(HG.VoiceControl.LingoData.Commands[s].Aliases[c]);
            if (res.StartIndex != -1 && (res.StartIndex < curmatch.StartIndex || curmatch.StartIndex == -1)) {
                command = HG.VoiceControl.LingoData.Commands[s].Command;
                curmatch = res;
                //	                            break;
            }
        }
    }
    //
    if (keepsentence) return curmatch;
    //
    if (curmatch.StartIndex != -1) {
        HG.VoiceControl.RemoveInputMatch(curmatch);
    }
    return command;
};

HG.VoiceControl.SearchGroupMatch = function (limitindex) {
    var group = -1;
    var curmatch = { Words: '', StartIndex: -1 };
    for (i = 0; i < HG.WebApp.Data.Groups.length; i++) {
        var res = HG.VoiceControl.FindMatchingInput(HG.WebApp.Data.Groups[i].Name);
        if (res.StartIndex != -1 && (res.StartIndex < limitindex || limitindex == -1) && (res.StartIndex < curmatch.StartIndex || curmatch.StartIndex == -1)) {
            group = i;
            curmatch = res;
            //break;
        }
    }
    if (curmatch.StartIndex != -1) {
        HG.VoiceControl.RemoveInputMatch(curmatch);
    }
    return group;
};

HG.VoiceControl.SearchSubjectMatch = function (group, limitindex) {
    var value = {};
    var groupmodules = {};
    if (group != '') {
        groupmodules = HG.Configure.Groups.GetGroupModules(group).Modules;
    }
    else {
        groupmodules = HG.WebApp.Data.Modules;
    }
    // try finding a module name / address
    var curmatch = { Words: '', StartIndex: -1 };
    for (m = 0; m < groupmodules.length; m++) {
        var module = groupmodules[m];
        var res = HG.VoiceControl.FindMatchingInput(module.Name);
        if (res.StartIndex == -1) res = HG.VoiceControl.FindMatchingInput(module.Address);
        //
        if (res.StartIndex != -1 && (res.Words.length >= curmatch.Words.length) && (res.StartIndex < limitindex || limitindex == -1) && (res.StartIndex <= curmatch.StartIndex || curmatch.StartIndex == -1)) {
            value = module;
            curmatch = res;
            //break;
        }
    }
    if (curmatch.StartIndex != -1) {
        HG.VoiceControl.RemoveInputMatch(curmatch);
    }
    return value;
};

// TODO: ... stuff to be completed here
HG.VoiceControl.SearchArgumentValueMatch = function (limitindex) {
};

HG.VoiceControl.RemoveInputMatch = function (wordsmatch) {
    if (wordsmatch.StartIndex > -1 && wordsmatch.Words.length > 0) {
        HG.VoiceControl.CurrentInput = HG.VoiceControl.CurrentInput.substring(0, wordsmatch.StartIndex) + ' ' + HG.VoiceControl.CurrentInput.substring(wordsmatch.StartIndex + wordsmatch.Words.length - 1);
    }
};

HG.VoiceControl.FindMatchingInput = function (words) {
    if (words && words != '' && words != 'undefined') {
        words = ' ' + words.toLowerCase() + ' ';
        var idx = (' ' + HG.VoiceControl.CurrentInput.toLowerCase() + ' ').indexOf(words);
        if (idx >= 0) {
            var wordsmatch = { Words: words, StartIndex: idx };
            return wordsmatch;
        }
    }
    return { Words: words, StartIndex: -1 };
};
