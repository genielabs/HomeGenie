/*!
 * Start Bootstrap - Creative Bootstrap Theme (http://startbootstrap.com)
 * Code licensed under the Apache License v2.0.
 * For details, see http://www.apache.org/licenses/LICENSE-2.0.
 */

(function($) {
    "use strict"; // Start of use strict

    // jQuery for page scrolling feature - requires jQuery Easing plugin
    $('a.page-scroll').bind('click', function(event) {
        var $anchor = $(this);
        $('html, body').stop().animate({
            scrollTop: ($($anchor.attr('href')).offset().top - 50)
        }, 1250, 'easeInOutExpo');
        event.preventDefault();
    });

    // Highlight the top nav as scrolling occurs
    $('body').scrollspy({
        target: '.navbar-fixed-top',
        offset: 51
    })

    // Closes the Responsive Menu on Menu Item Click
    $('.navbar-collapse ul li a').click(function() {
        $('.navbar-toggle:visible').click();
    });

    // Fit Text Plugin for Main Header
    $("h1").fitText(
        1.2, {
            minFontSize: '35px',
            maxFontSize: '65px'
        }
    );

    // Offset for Main Navigation
    $('#mainNav').affix({
        offset: {
            top: 100
        }
    })

    // Initialize WOW.js Scrolling Animations
    new WOW().init();

})(jQuery); // End of use strict

var currentTabletImage = 0;
window.setTimeout(animate_tablet, 2600);
function animate_tablet() {
    $('#tablet_img_0'+currentTabletImage).fadeOut(600);
    currentTabletImage++;
    if (currentTabletImage > 9) {
        currentTabletImage = 0;
    }
    $('#tablet_img_0'+(currentTabletImage)).fadeIn(600);
    window.setTimeout(animate_tablet, 2600);
}
$(document).ready(function() {
    var dw = $('header').width();
    var dh = $('header').height();
    var h = $('#header-background').height();
    $(window).resize(function() {
        dw = $('header').width();
        dh = $('header').height();
        $('#header-background').width(dw);
        $('#header-background').height(dh);
        h = $('#header-background').height();
    });
    //$('#header-background').width(dw+(dw/5));
    //$('#header-background').css('left', (-dw/10));
    //$('#header-background').css('right', (-dw/10));
    $(window).scroll(function() {
        var offset = $(window).scrollTop();
        if (offset > h/3) {
            $('#find-out-more').hide();
            $('#about-arrow-up').show();
            $('#about-arrow-down').show();
        } else {
            $('#find-out-more').show();
            $('#about-arrow-up').hide();
            $('#about-arrow-down').hide();
        }
        //var o = offset / 800; if (o > 1) o = 1; o = 1-o;
        $('#header-background').css('top', offset/2);
        $('#header-background').height(h - offset/4);
    });
});
