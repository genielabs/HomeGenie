zuix.controller(function (cp) {

    cp.init = function () {
        this.options().css = false;
    };

    cp.create = function () {
        window.onhashchange = function () {
            var path = getCurrentPath();
            cp.trigger('pathChanged', path);
        };
        cp.expose('data', data);
        cp.expose('path', getCurrentPath);
        cp.expose('list', list);
        cp.expose('getContent', getContent);
    };


    // this is the JSON site data
    var contentTree = null;

    var dummyController = zuix.controller(function(cp){});
    var contentController = zuix.controller(function(cp){
        cp.create = function () {
            cp.view().hide();
        };
    });

    function data(d) {
        if (d != null)
            contentTree = d;
        return contentTree;
    }

    function getCurrentPath() {
        if (window.location.hash.length > 0)
            return window.location.hash;
        return '';
    }

    function list(items, container, callback) {
        zuix.$.each(items, function (k, v) {
            (function(item, isLast) {
                var itemId = v.id+'['+item.template+']';
                var ctx = zuix.context(itemId);
                if (ctx == null) {
                    zuix.load(item.template, {
                        contextId: itemId,
                        css: false,
                        braces: item.data,
                        controller: dummyController,
                        ready: function (c) {
                            c.options().data = item;
                            c.view().setAttribute('data-id', item.id);
                            zuix.$(container).append(c.view());
                            callback(c, isLast);
                            if (item.list != null) {
                                list(item.list, c.view(), function () {
                                   // TODO: ...
                                });
                            }
                        }
                    });
                } else {
                    callback(ctx, isLast);
                }
            })(v, k == items.length - 1);
        });
    }

    function getContent(path, callback) {
        // exit if no content path specified
        if (path == null || path.length == 0) return;
        var content = zuix.context(path);
        if (content == null) {
            zuix.load(path, {
                contextId: path,
                wrapContent: true,
                markdown: true,
                prism: true,
                css: false,
                cext: '',
                controller: contentController,
                ready: function (c) {
                    callback(c, true);
                }
            });
        } else {
            callback(content, false);
        }
    }


});
