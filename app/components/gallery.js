zuix.controller(function (cp) {
    var slides, current, slideTimeout;
    slides = null;
    current = 0;
    slideTimeout = null;

    cp.init = function() {
        cp.options().html = false;
        cp.options().css = false;
    };

    cp.create = function () {
        var container = zuix.$(document.createElement('div'));
        container.css({
                'overflow': 'hidden',
                'position': 'relative',
                'width': '100%'
            });
        slides = cp.view().children();
        slides.each(function (i, el) {
            this.css({
                    'cursor': 'pointer',
                    'left': 0,
                    'top': 0,
                    'width': '100%'
                });
            if (i > 0)
                this.visibility('hidden')
                    .css('position', 'absolute');
        });
        for(var c = slides.length() - 1; c >= 0; c--) {
            container.insert(0, slides.get(c));
        }
        cp.view().append(container.get());
        slides = container.children();
        cp.view().on('click', function () {
            slide();
        });
        setTimeout(slide, 5000);
    };

    function slide() {
        if (slideTimeout !== null) clearTimeout(slideTimeout);
        if (current !== -1) {
            slides.eq(current).animateCss('fadeOut', function () {
                this.visibility('hidden');
            });
        }
        current++;
        if (current >= slides.length()) {
            current = 0;
        }
        // center the slide horizontally
        var width = cp.view().get().clientWidth;
        var cview = slides.eq(current);
        var cx = (width - cview.get().clientWidth) / 2;
        cview.css('left', cx+'px').visibility('visible').animateCss('fadeIn');
        if (slides.length() > 1) {
            slideTimeout = setTimeout(slide, 5000);
        }
    }
});
