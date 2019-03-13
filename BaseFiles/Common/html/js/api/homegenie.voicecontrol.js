var recognition = null;
var final_transcript = '';
//
// namespace : HG.VoiceControl namespace
// info      : -
//
HG.VoiceControl = HG.VoiceControl || new function(){ var $$ = this;

    $$.CurrentInput = '';
    $$.LingoData = [];

    $$.Initialize = function() {
        var userLang = HG.WebApp.Locales.GetUserLanguage();
        // enable/disable speech input
            // lookup for a localized version, if any
        $$.Localize('./locales/' + userLang.toLowerCase().substring(0, 2) + '.lingo.json', function(success) {
            if (!success)
            {
                // fallback to english lingo file
                $$.Localize('./locales/en.lingo.json', function(res){ 
                    $$.Setup();
                });
            } else {
                $$.Setup();
            }
        });
    };

    $$.Setup = function () {
        if (!('webkitSpeechRecognition' in window)) {
            //no speech support
            //$('#speechinput').hide();
            //$('#control_bottombar_voice_button').addClass('ui-disabled');
            $('#voicerecognition_button').addClass('ui-disabled');
        } else {
            recognition = new webkitSpeechRecognition();
            recognition.continuous = false;
            recognition.interimResults = false;
            recognition.onstart = function() { 
                $('#voicerecognition_button').addClass('ui-disabled');
            }
            recognition.onresult = function(event) { 
                var interim_transcript = '';
                if (typeof(event.results) == 'undefined') {
                    $('#speechinput').hide();
                    recognition.onend = null;
                    recognition.stop();
                    return;
                }
                for (var i = event.resultIndex; i < event.results.length; ++i) {
                    if (event.results[i].isFinal) {
                        final_transcript += event.results[i][0].transcript;
                    } else {
                        interim_transcript += event.results[i][0].transcript;
                    }
                }
            }
            recognition.onerror = function(event) { 
                $('#voicerecognition_button').removeClass('ui-disabled');
                alert('Voice Recognition Error: ' + event); 
            }
            recognition.onend = function() { 
                $('#voicerecognition_text').val(final_transcript);
                $('#voicerecognition_button').removeClass('ui-disabled');
                $$.InterpretInput(final_transcript);
                final_transcript = '';
            }
        }
    };

    $$.Localize = function (langurl, callback) {
        // get data via ajax 
        // store it in 		$$.LingoData
        // and replace locales strings in the current page
        $.ajax({
            url: langurl,
            type: 'GET',
            success: function (data) {
                $$.LingoData = $.parseJSON(data);
                callback(true);
            },
            failure: function (data) {
                callback(false);
            }
        });
    };

    $$.InterpretInput = function (sentence) {
        $$.CurrentInput = sentence;
        $$.ParseNext();
    };

    $$.ParseNext = function() {

        var command = $$.SearchCommandMatch(false);
        var nextcommand = $$.SearchCommandMatch(true);
        //
        var type = $$.SearchTypeMatch(false);
        //
        var group = $$.SearchGroupMatch(nextcommand.StartIndex);
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
                        setTimeout($$.ParseNext, 50);
                    }

                }
            }

        }
        else {
            var module = $$.SearchSubjectMatch(group, nextcommand.StartIndex);
            //
            if (command != '') {
                //console.log(module.Address + ' ' + module.Domain);
                HG.Control.Modules.ServiceCall(command, module.Domain, module.Address, '');
                setTimeout($$.ParseNext, 50);
            }
            //console.log(group + ' ' + command + ' ' + module.Name);
        }
    };

    $$.SearchTypeMatch = function (keepsentence) {
        var type = '';
        var curmatch = { Words: '', StartIndex: -1 };
        for (s = 0; s < $$.LingoData.Types.length; s++) {
            for (c = 0; c < $$.LingoData.Types[s].Aliases.length; c++) {
                var res = $$.FindMatchingInput($$.LingoData.Types[s].Aliases[c]);
                if (res.StartIndex != -1 && (res.StartIndex < curmatch.StartIndex || curmatch.StartIndex == -1)) {
                    type = $$.LingoData.Types[s].Type;
                    curmatch = res;
                    //	                            break;
                }
            }
        }
        //
        if (keepsentence) return curmatch;
        //
        if (curmatch.StartIndex != -1) {
            $$.RemoveInputMatch(curmatch);
        }
        return type;
    };

    $$.SearchCommandMatch = function (keepsentence) {
        var command = '';
        var curmatch = { Words: '', StartIndex: -1 };
        for (s = 0; s < $$.LingoData.Commands.length; s++) {
            for (c = 0; c < $$.LingoData.Commands[s].Aliases.length; c++) {
                var res = $$.FindMatchingInput($$.LingoData.Commands[s].Aliases[c]);
                if (res.StartIndex != -1 && (res.StartIndex < curmatch.StartIndex || curmatch.StartIndex == -1)) {
                    command = $$.LingoData.Commands[s].Command;
                    curmatch = res;
                    //	                            break;
                }
            }
        }
        //
        if (keepsentence) return curmatch;
        //
        if (curmatch.StartIndex != -1) {
            $$.RemoveInputMatch(curmatch);
        }
        return command;
    };

    $$.SearchGroupMatch = function (limitindex) {
        var group = -1;
        var curmatch = { Words: '', StartIndex: -1 };
        for (i = 0; i < HG.WebApp.Data.Groups.length; i++) {
            var res = $$.FindMatchingInput(HG.WebApp.Data.Groups[i].Name);
            if (res.StartIndex != -1 && (res.StartIndex < limitindex || limitindex == -1) && (res.StartIndex < curmatch.StartIndex || curmatch.StartIndex == -1)) {
                group = i;
                curmatch = res;
                //break;
            }
        }
        if (curmatch.StartIndex != -1) {
            $$.RemoveInputMatch(curmatch);
        }
        return group;
    };

    $$.SearchSubjectMatch = function (group, limitindex) {
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
            var res = $$.FindMatchingInput(module.Name);
            if (res.StartIndex == -1) res = $$.FindMatchingInput(module.Address);
            //
            if (res.StartIndex != -1 && (res.Words.length >= curmatch.Words.length) && (res.StartIndex < limitindex || limitindex == -1) && (res.StartIndex <= curmatch.StartIndex || curmatch.StartIndex == -1)) {
                value = module;
                curmatch = res;
                //break;
            }
        }
        if (curmatch.StartIndex != -1) {
            $$.RemoveInputMatch(curmatch);
        }
        return value;
    };

    // TODO: ... stuff to be completed here
    $$.SearchArgumentValueMatch = function (limitindex) {
    };

    $$.RemoveInputMatch = function (wordsmatch) {
        if (wordsmatch.StartIndex > -1 && wordsmatch.Words.length > 0) {
            $$.CurrentInput = $$.CurrentInput.substring(0, wordsmatch.StartIndex) + ' ' + $$.CurrentInput.substring(wordsmatch.StartIndex + wordsmatch.Words.length - 1);
        }
    };

    $$.FindMatchingInput = function (words) {
        if (words && words != '' && words != 'undefined') {
            words = ' ' + words.toLowerCase() + ' ';
            var idx = (' ' + $$.CurrentInput.toLowerCase() + ' ').indexOf(words);
            if (idx >= 0) {
                var wordsmatch = { Words: words, StartIndex: idx };
                return wordsmatch;
            }
        }
        return { Words: words, StartIndex: -1 };
    };

};