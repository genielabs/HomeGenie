var CONST_BASE_URL = '/zuix_mdl_blog';
var CONST_SITE_TITLE = 'Welcome to HomeGenie site';
var CONST_STARTPAGE = '#/about';

var contentLoader = null;
var currentPage = null;

var contentOptions = {
    markdown: {
        css: false,
        markdown: true,
        braces: true
    },
    braces: {
        css: false,
        braces: true
    },
    loader: {
        ready: function (ctx) {
            var mdlLayout = zuix.$('.mdl-layout');
            var mdlDrawer = mdlLayout.find('.mdl-layout__drawer');
            contentLoader = ctx;
            contentLoader.on('pathChanged', function(e, path) {
                if (mdlDrawer.attr('aria-hidden') == 'false')
                    mdlLayout.get().MaterialLayout.toggleDrawer();
                showPage(path);
            });
            contentLoader.data(contentTree);
            var showDelay = 0;
            contentLoader.list(
                // the list items to load
                contentTree.main,
                // the container where to append items
                zuix.field('menu'),
                // the callback function to call once each item is loaded
                function(c, eol) {
                    // animate menu item once its loaded
                    zuix.$(c.view())
                        .animateCss('bounceInLeft', { delay: showDelay+'s' });
                    showDelay += 0.1;
                    // load page content once menu has completed loading
                    if (eol) showPage(contentLoader.path());
                });
        }
    }
};


function showPage(path) {
    // parse path or use default
    if (path == null || path.length == 0)
        path = CONST_STARTPAGE;
    path = path.replace('#/', '').split('/');
    // get item data
    var item = getItemFromPath(path);
    if (item == null)
        alert('error');
    // get and show the page
    contentLoader.getContent(item.data.file, function(pageContext, isNew){
        if (isNew) {
            makeScrollable(pageContext.view());
            zuix.field('content_pages')
                .append(pageContext.view());
            zuix.componentize(pageContext.view());
        }
        revealPage(pageContext);
    });
    // update the title bar and highlight current menu item
    zuix.$('.main-navigation div > a').removeClass('current');
    zuix.$.each(path, function (k, v) {
        zuix.$('.main-navigation div[data-id="' + v + '"] > a').addClass('current');
    });
    zuix.field('header-title').html(item.data.title);
}

function revealPage(pageContext) {
    var crossFadeDuration = '0.05s';
    if (currentPage != null)
        zuix.$(currentPage.view()).animateCss('fadeOut', function () {
            this.hide();
        }, { duration: crossFadeDuration });
    zuix.$(pageContext.view()).show().animateCss('fadeIn', null, { duration: crossFadeDuration });
    currentPage = pageContext;
}

function makeScrollable(div) {
    // turn content into an absolute positioned and scrollable element
    // so each page has its own independent scroll
    div.style.position = 'absolute';
    div.style.left = 0;
    div.style.right = 0;
    div.style.bottom = 0;
    div.style.top = 0;
    div.style['overflow'] = 'hidden';
    div.style['overflow-y'] = 'auto';
    div.setAttribute('layout', 'column top-center');
}

function getItemFromPath(path) {
    var item = null, list = contentTree.main;
    for(var p = 0; p < path.length; p++) {
        zuix.$.each(list, function (k, v) {
            if (v.id === path[p]) {
                item = v;
                list = item.list;
                return true;
            }
        });
    }
    return item;
}

zuix.hook('html:parse', function (data) {
    // ShowDown - Markdown compiler
    if (this.options().markdown === true && typeof showdown !== 'undefined') {
        data.content = new showdown.Converter()
            .makeHtml(data.content);
    }
    if (this.options().wrapContent === true) {
        data.content = '<div class="content-padding mdl-shadow--2dp" self="size-xlarge">'+data.content+'</div>';
    }
    if (this.options().braces != null) {
        var _vars = this.options().braces;
        var parsedHtml = zuix.$.replaceBraces(data.content, function (varName) {
            switch (varName) {
                case 'app.title':
                    return CONST_SITE_TITLE;
                case 'site.baseurl':
                    return CONST_BASE_URL;
                default:
                    if (_vars[varName])
                        return _vars[varName];
            }
        });
        if (parsedHtml != null)
            data.content = parsedHtml;
    }
}).hook('view:process', function (view) {
    // Prism code syntax highlighter
    if (this.options().prism && typeof Prism !== 'undefined') {
        view.find('code').each(function (i, block) {
            this.addClass('language-javascript');
            Prism.highlightElement(block);
        });
    }
    // Force opening of all non-local links in a new window
    zuix.$('a[href*="://"]').attr('target','_blank');
    // Material Design Light integration - DOM upgrade
    if (/*this.options().mdl &&*/ typeof componentHandler !== 'undefined')
        componentHandler.upgradeElements(view.get());

    //zuix.componentize(view);
});

// animateCss extension method for ZxQuery
zuix.$.ZxQuery.prototype.animateCss  = function (animationName, param1, param2) {
    var callback, options;

    if (typeof param2 === 'function') {
        options = param1;
        callback = param2;
    } else {
        if (typeof param1 === 'function')
            callback = param1;
        else options = param1;
    }

    var prefixes = ['-webkit', '-moz', '-o', '-ms'];
    for (var key in options)
        for (var p in prefixes)
            this.css(prefixes[p] + '-animation-' + key, options[key]);
    var animationEnd = 'webkitAnimationEnd mozAnimationEnd MSAnimationEnd oanimationend animationend';
    var _t = this;

    if (typeof animationName !== 'function') {
        // stops any previously running animation
        if (this.hasClass('animated')) {
            this.css('transition', ''); // TODO: <-- is this really needed?
            this.trigger('animationend');
        }
        // TODO: should run all the following code for each element in the ZxQuery selection
        this.addClass('animated ' + animationName);
    } else callback = animationName;

    this.one(animationEnd, function () {
        this.removeClass('animated ' + animationName);
        for(var key in options)
            for (var p in prefixes)
                _t.css(prefixes[p] + '-animation-' + key, '');
        if (typeof callback === 'function')
            callback.call(_t, animationName);
    });
    return this;
};
