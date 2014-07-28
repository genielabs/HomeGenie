

$('#scheme_color_navbar').minicolors({
    animationSpeed: 100,
    animationEasing: 'swing',
    change: function () { $('#change_color_navbar').removeClass('disabled'); },
    changeDelay: 0,
    control: 'saturation',
    hide: null,
    hideSpeed: 100,
    inline: false,
    letterCase: 'lowercase',
    opacity: false,
    position: 'bottom left',
    show: null,
    showSpeed: 100,
    swatchPosition: 'left',
    textfield: true
});

$('#demo_form_navbar').change(function(e){
    var color = $('#scheme_color_navbar').val();
    $('#change_color_navbar').removeClass('disabled');
    $('#scheme_color_navbar').val(color);
});

$('#demo_form_navbar').submit(function(e){

    $this = $(this);

    var color = $('#scheme_color_navbar').val();


    //var color = $('#scheme_color').val();

    /*if ($('#tetrad').is(':checked')) {
        var wheel_pos1 = '30';
        var wheel_pos2 = '180';
        var wheel_pos3 = '210';
    } else if ($('#triad').is(':checked')) {
        var wheel_pos1 = '120';
        var wheel_pos2 = '240';
        var wheel_pos3 = '0';
    } else if ($('#complement').is(':checked')) {
        var wheel_pos1 = '180';
        var wheel_pos2 = '0';
        var wheel_pos3 = '180';
    } else if ($('#monochrome').is(':checked')) {
        var wheel_pos1 = '8';
        var wheel_pos2 = '352';
        var wheel_pos3 = '0';
    } else */{
        var wheel_pos1 = '45';
        var wheel_pos2 = '315';
        var wheel_pos3 = '180';
    }

    try
    {
        less.modifyVars({
            '@seed-color': color,
            '@wheel_pos1': wheel_pos1,
            '@wheel_pos2': wheel_pos2,
            '@wheel_pos3': wheel_pos3
        });
    } catch (e) { }

    $('#scheme_color_navbar').val(color);

    return false;
});



function rgb2hex(rgb) {
    rgb = rgb.match(/^rgb\((\d+),\s*(\d+),\s*(\d+)\)$/);
    function hex(x) {
        return ("0" + parseInt(x).toString(16)).slice(-2);
    }
    return "#" + hex(rgb[1]) + hex(rgb[2]) + hex(rgb[3]);
}

less = {
    env: "development",
    async: true
};