!function(e){if("object"==typeof exports)module.exports=e();else if("function"==typeof define&&define.amd)define(e);else{var f;"undefined"!=typeof window?f=window:"undefined"!=typeof global?f=global:"undefined"!=typeof self&&(f=self),f.zuix=e()}}(function(){var define,module,exports;return (function e(t,n,r){function s(o,u){if(!n[o]){if(!t[o]){var a=typeof require=="function"&&require;if(!u&&a)return a(o,!0);if(i)return i(o,!0);throw new Error("Cannot find module '"+o+"'")}var f=n[o]={exports:{}};t[o][0].call(f.exports,function(e){var n=t[o][1][e];return s(n?n:e)},f,f.exports,e,t,n,r)}return n[o].exports}var i=typeof require=="function"&&require;for(var o=0;o<r.length;o++)s(r[o]);return s})({1:[function(_dereq_,module,exports){
/**
 * Copyright 2015-2017 G-Labs. All Rights Reserved.
 *         https://genielabs.github.io/zuix
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

/**
 *
 *  This file is part of
 *  ZUIX, Javascript library for component-based development.
 *        https://genielabs.github.io/zuix
 *
 * @author Generoso Martello <generoso@martello.com>
 */

"use strict";

function AsynChain(callback) {
    listener = callback;
}

AsynChain.prototype.isReady = function () {
    return jobsList.length == 0 || currentIndex == -1;
};
AsynChain.prototype.getJobs = function () {
    return jobsList;
};
AsynChain.prototype.setJobs = function(list) {
    if (jobsList.length > 0) {
        // TODO: this case should never happen
        currentIndex = -1;
        jobsList.length = 0;
        //done();
        return;
    }
    jobsList = list.slice();
    listener.status('start', jobsList);
    next();
};
AsynChain.prototype.append = function(list) {
    // TODO: this is causing stack-overflow
    if (this.isReady())
        this.setJobs(list);
    else
        Array.prototype.push.apply(jobsList, list);
};

// --------------------------------------------

var jobsList = [];
var currentIndex = -1;
var listener = null;
var lazyThread = null;

function next() {
    resetAsynCallback();
    currentIndex++;
    if (currentIndex < jobsList.length && !listener.willBreak()) {
        worker();
        return true;
    } else if (currentIndex >= jobsList.length || listener.willBreak())
        done();
    return false;
}
function done(reason) {
    currentIndex = -1;
    jobsList.length = 0;
    jobsList = [];
    listener.status(reason != null ? reason : 'done');
}

function worker() {
    var job = jobsList[currentIndex];
    if (job == null) return false;
    var doWork = function () {
        resetAsynCallback();
        if (!listener.doWork(job.item, function () {
            lazyThread = requestAnimationFrame(next);
        })) next();
    };
    if (job.cancelable) {
        if (listener.willBreak())
            done('stopped');
        else if (lazyThread == null)
            lazyThread = requestAnimationFrame(doWork);
        //else next();
        else return false;
    } else doWork();
    return true;
}

function resetAsynCallback() {
    if (lazyThread != null) {
        cancelAnimationFrame(lazyThread);
        lazyThread = null;
    }
}

module.exports = function (callback) {
    return new AsynChain(callback);
};
},{}],2:[function(_dereq_,module,exports){
/**
 * Copyright 2015-2017 G-Labs. All Rights Reserved.
 *         https://genielabs.github.io/zuix
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

/**
 *
 *  This file is part of
 *  ZUIX, Javascript library for component-based development.
 *        https://genielabs.github.io/zuix
 *
 * @author Generoso Martello <generoso@martello.com>
 */

"use strict";

var _console = null, _global = null;
var _console_m = [
    "log", "info", "warn", "error", "debug", "trace", "dir", "group",
    "groupCollapsed", "groupEnd", "time", "timeEnd", "profile", "profileEnd",
    "dirxml", "assert", "count", "markTimeline", "timeStamp", "clear"
];
var _bc = 'background-color:rgba(200,200,200,0.2);',
    _bt = 'background-color:transparent;',
    _c1 = 'color:#8a53ff;',
    _c2 = 'color:#777777',
    _c3 = 'color:#888888;',
    _c_start = 'color:#999900;',
    _c_end = 'color:#00aa00;',
    _c_end_very_slow = 'color:#ff0000;',
    _c_end_slow = 'color:#ff7700;';

var _callback = null;

/**
 * Simple Logging Helper
 *
 * @class Logger
 * @constructor
 */
function Logger(ctx) {
    _console = window ? window.console : {};
    _global = window ? window : {};
    this._timers = {};
    this.args = function(context, level, args) {
        var logHeader = '%c '+level+' %c'+(new Date().toISOString())+' %c'+context;
        var colors = [ _bc+_c1, _bc+_c2, _bc+_c3 ];
        for(var i = 0; i < args.length; i++) {
            if (typeof args[i] == 'string' && args[i].indexOf('timer:') == 0) {
                var t = args[i].split(':');
                if (t.length == 3) {
                    switch (t[2]) {
                        case 'start':
                            this._timers[t[1]] = new Date().getTime();
                            logHeader += ' %cSTART '+t[1];
                            colors.push(_bc+_c_start);
                            break;
                        case 'stop':
                            var elapsed = (new Date().getTime() - this._timers[t[1]]);
                            logHeader += ' %cSTOP '+t[1]+' '+
                                elapsed +
                                ' ms';
                            if (elapsed > 200)
                                colors.push(_bc+_c_end_very_slow);
                            else if (elapsed > 100)
                                colors.push(_bc+_c_end_slow);
                            else
                                colors.push(_bc+_c_end);
                            break;
                    }
                }
            }
        }
        logHeader += ' \n%c '; colors.push(_bt+'color:inherit;');
        //if (typeof args[0] == 'string') {
        //    logHeader += ' %c' + args[0];
        //    Array.prototype.shift.call(args);
        //}
        for (var c = colors.length-1; c >= 0; c--)
            Array.prototype.unshift.call(args, colors[c]);
        Array.prototype.unshift.call(args, logHeader);
        Array.prototype.push.call(args, '\n\n');
    };
    this.log = function (level, args) {
        if (typeof _callback === 'function')
            _callback.call(ctx, level, args);
        // route event
        if (!_global.zuixNoConsoleOutput) {
            this.args(ctx, level, args);
            _console.log.apply(_console, args);
        }
    };
}

Logger.prototype.monitor = function (callback) {
    // global callback for debugging purpose
    _callback = callback;
};

Logger.prototype.console = function(enable) {
    if (enable) {
        window.console = _console;
    } else {
        window.console = {};
        for (var i = 0; i < _console_m.length; i++) {
            if (!window.console[_console_m[i]]) {
                window.console[_console_m[i]] = function () { };
            }
        }
    }
};

Logger.prototype.i = Logger.prototype.info =
Logger.prototype.l = Logger.prototype.log = function(){
    this.log("INFO", arguments);
    return this;
};
Logger.prototype.w = Logger.prototype.warn = function () {
    this.log("WARN", arguments);
    return this;
};
Logger.prototype.e = Logger.prototype.error = function () {
    this.log("ERROR", arguments);
    return this;
};
Logger.prototype.d = Logger.prototype.debug = function(){
    this.log("DEBUG", arguments);
    return this;
};
Logger.prototype.t = Logger.prototype.trace = function () {
    this.log("TRACE", arguments);
    return this;
};

module.exports = function(ctx) {
    return new Logger(ctx);
};

},{}],3:[function(_dereq_,module,exports){
/**
 * Copyright 2015-2017 G-Labs. All Rights Reserved.
 *         https://genielabs.github.io/zuix
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

/**
 *
 *  This file is part of
 *  ZUIX, Javascript library for component-based development.
 *        https://genielabs.github.io/zuix
 *
 * @author Generoso Martello <generoso@martello.com>
 */

"use strict";

var _log =
    _dereq_('./Logger')('TaskQueue.js');

/**
 * Task Queue Manager
 *
 * @class TaskQueue
 * @constructor
 */
function TaskQueue(listener) {
    var _t = this;
    _t._worker = null;
    _t._taskList = [];
    _t._requests = [];
    if (listener == null)
        listener = function () { };
    _t.taskQueue = function (tid, fn, pri) {
        _t._taskList.push({
            tid: tid,
            fn: fn,
            status: 0,
            priority:  pri,
            step: function (tid) {
                //var _h = this;
                //_h.tid = tid;
                _log.t(tid, 'load:step');
                listener(_t, 'load:step', {
                    task: tid
                });
            },
            end: function () {
                this.status = 2;
                var _h = this;
                _log.t(_h.tid, 'load:next', 'timer:task:stop');
                listener(_t, 'load:next', {
                    task: _h.tid
                });
                _t._taskList.splice(this.index, 1);
                _t.taskCheck();
                if (this._callback != null)
                    this._callback.call(this);
            },
            callback: function (callback) {
                this._callback = callback;
            }
        });
        _log.t(tid, 'task added', pri, 'priority');
        _t._taskList.sort(function(a,b) {
            return (a.priority > b.priority) ?
                1 :
                ((b.priority > a.priority)
                    ? -1 : 0);
        } );
        _t.taskCheck();
    };
    _t.taskCheck = function () {
        for (var i = 0; i < _t._taskList.length; i++) {
            if (_t._taskList[i].status == 0) {
                _t._taskList[i].status = 1;
                _log.t(_t._taskList[i].tid, 'load:begin', 'timer:task:start');
                listener(_t, 'load:begin', {
                    task: _t._taskList[i].tid
                });
                _t._taskList[i].index = i;
                (_t._taskList[i].fn).call(_t._taskList[i]);
                return;
            }  else if (_t._taskList[i].status == 1) {
                // currently running
                return;
            }
            else if (_t._taskList[i].status == 2) {
                // TODO: _!!!-!
                return;
            }
        }
        _log.t('load:end');
        listener(_t, 'load:end');
    }
}
TaskQueue.prototype.queue = function(tid, fn, pri) {
    return this.taskQueue(tid, fn, pri);
};

module.exports = TaskQueue;
},{"./Logger":2}],4:[function(_dereq_,module,exports){
/**
 * Copyright 2015-2017 G-Labs. All Rights Reserved.
 *         https://genielabs.github.io/zuix
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

/**
 *
 *  This file is part of
 *  ZUIX, Javascript library for component-based development.
 *        https://genielabs.github.io/zuix
 *
 * @author Generoso Martello <generoso@martello.com>
 */

"use strict";

// Generic utility class
module.exports = {

    isIE: function() {
        return (window.navigator.userAgent.indexOf('Trident') > 0);
    },

    isNoU: function (obj) {
        return (typeof obj === 'undefined' || obj === null);
    },

    isFunction: function (f) {
        return typeof f === 'function';
    },

    objectEquals: function (x, y) {
        if (x === null || x === undefined || y === null || y === undefined) {
            return x === y;
        }
        // after this just checking type of one would be enough
        if (x.constructor !== y.constructor) {
            return false;
        }
        // if they are functions, they should exactly refer to same one (because of closures)
        if (x instanceof Function) {
            return x === y;
        }
        // if they are regexps, they should exactly refer to same one (it is hard to better equality check on current ES)
        if (x instanceof RegExp) {
            return x === y;
        }
        if (x === y || x.valueOf() === y.valueOf()) {
            return true;
        }
        if (Array.isArray(x) && x.length !== y.length) {
            return false;
        }

        // if they are dates, they must had equal valueOf
        if (x instanceof Date) {
            return false;
        }

        // if they are strictly equal, they both need to be object at least
        if (!(x instanceof Object)) {
            return false;
        }
        if (!(y instanceof Object)) {
            return false;
        }

        // recursive object equality check
        var p = Object.keys(x);
        return Object.keys(y).every(function (i) {
                return p.indexOf(i) !== -1;
            }) &&
            p.every(function (i) {
                return util.objectEquals(x[i], y[i]);
            });
    },

    propertyFromPath: function (o, s) {
        if (typeof s !== 'string') return;
        s = s.replace(/\[(\w+)]/g, '.$1'); // convert indexes to properties
        s = s.replace(/^\./, '');           // strip a leading dot
        var a = s.split('.');
        var ref = o;
        for (var i = 0, n = a.length; i < n; ++i) {
            var k = a[i];
            if (typeof ref[k] !== 'undefined') {
                ref = ref[k];
            } else {
                return;
            }
        }
        return ref;
    },

    cloneObject: function cloneObject(obj) {
        if (obj === null || typeof obj !== 'object') {
            return obj;
        }
        // give temp the original obj's constructor
        var temp = obj.constructor();
        for (var key in obj)
            temp[key] = cloneObject(obj[key]);
        return temp;
    }

};
},{}],5:[function(_dereq_,module,exports){
/**
 * Copyright 2015-2017 G-Labs. All Rights Reserved.
 *         https://genielabs.github.io/zuix
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

/**
 *
 *  This file is part of
 *  ZUIX, Javascript library for component-based development.
 *        https://genielabs.github.io/zuix
 *
 * @author Generoso Martello <generoso@martello.com>
 */

"use strict";

var _log =
    _dereq_('./Logger')('TaskQueue.js');
var util = _dereq_('./Util.js');

/**
 *
 * @callback ZxQuery~iterationCallback
 * @param {number} i Iteration count
 * @param {object} item Current element
 * @this {object}
 */

/**
 *
 * @callback ZxQuery~instanceIterationCallback
 * @param {number} count Iteration count
 * @param {Element} item Current element
 * @this {ZxQuery}
 */


/** @private */
var _zuix_events_mapping = [];
function routeEvent(e) {
    triggerEventHandlers(this, e.type, e);

}
function addEventHandler(el, path, handler) {
    var found = false;
    z$.each(_zuix_events_mapping, function () {
        if (this.element === el && this.path === path && this.handler == handler) {
            _log.w('Handler already registered', el, path, handler);
            found = true;
            return false;
        }
    });
    if (!found) {
        _zuix_events_mapping.push({ element: el, path: path, handler: handler });
        el.addEventListener(path, routeEvent, false);
    }
}
function removeEventHandler(el, path, handler) {
    var left = 1, index = -1;
    z$.each(_zuix_events_mapping, function (i) {
        if (this.element === el && this.path === path && this.handler == handler) {
            left--;
            index = i;
        }
    });
    if (index !== -1)
        _zuix_events_mapping.splice(index, 1);
    // unregister event handler since it was the last one
    if (left == 0)
        el.removeEventListener(path, routeEvent);
}
function triggerEventHandlers(el, path, evt) {
    var element = z$(el);
    z$.each(_zuix_events_mapping, function () {
        if (this.element === el && this.path === path) {
            this.handler.call(element, evt);
        }
    });
}
function removeAllEventHandlers(el) {
    z$.each(_zuix_events_mapping, function () {
        if (this.element === el) {
            _log.t('Removing event handler', this.element, this.path, this.handler);
            removeEventHandler(this.element, this.path, this.handler);
        }
    });
}

/**
 * ZxQuery, a very lite subset of jQuery-like functions
 * internally used in Zuix.
 *
 * The constructor takes one optional argument that can be
 * a DOM element, a node list or a valid DOM query selector string expression.
 * If no parameter is given, the ZxQuery will wrap the root *document* element.
 *
 * @class ZxQuery
 * @param {Object|ZxQuery|Array<Node>|Node|NodeList|string|undefined} [element]
 * @return {ZxQuery} The *ZxQuery* instance object.
 * @constructor
 */
function ZxQuery(element) {
    /** @protected */
    this._selection = [];

    if (typeof element === 'undefined')
        element = document.documentElement;

    if (element instanceof ZxQuery)
        return element;
    else if (element instanceof HTMLCollection || element instanceof NodeList || Array.isArray(element))
        this._selection = element;
    else if (element instanceof HTMLElement || element instanceof Node)
        this._selection = [element];
    else if (typeof element === 'string')
        this._selection = document.documentElement.querySelectorAll(element);
    else if (element !== null) { //if (typeof element === 'string') {
        _log.e('ZxQuery cannot wrap object of this type.', (typeof element), element);
        throw(new Error(), element);
    }
    return this;
}


/**
 * Number of elements in current DOM selection.
 * @return {Number} Number of DOM elements in the current selection.
 */
ZxQuery.prototype.length = function () {
    return this._selection.length;
};
/**
 * Get the closest parent matching the selector filter.
 * @param {string} [filter] A valid DOM query selector filter (**default:** *first parent*).
 * @return {ZxQuery} A new *ZxQuery* object with the *parent* selection.
 */
ZxQuery.prototype.parent = function (filter) {
    if (!util.isNoU(filter))
        return new ZxQuery(z$.getClosest(this._selection[0], filter));
    return new ZxQuery(this._selection[0].parentNode);
};
/**
 * Get the children matching the given selector filter.
 * @param {string} [filter] A valid DOM query selector filter (**default:** *all children*).
 * @return {ZxQuery}  A new *ZxQuery* object with the *children* selection.
 */
ZxQuery.prototype.children = function (filter) {
    // TODO: implement filtering
    if (!util.isNoU(filter))
        return new ZxQuery(this._selection[0].querySelectorAll(filter));
    return new ZxQuery(this._selection[0].children);
};
/**
 * Reverse the order of elements in current selection.
 * @return {ZxQuery} The *ZxQuery* object itself
 */
ZxQuery.prototype.reverse = function () {
    var elements = (Array.prototype.slice).call(this._selection, 0);
    this._selection = elements.reverse();
    return this;
};
/**
 * Get the DOM element at given position in the current selection.
 * If no index is provided, the default element will be returned.
 *
 * @param {number} [i] Position of element (**default:** 0)
 * @return {Node|Element} The *DOM* element
 */
ZxQuery.prototype.get = function (i) {
    if (util.isNoU(i)) i = 0;
    return this._selection[i];
};
/**
 * Get the ZxQuery object for then element at the given
 * position in the current selection.
 * @param {number} i Position of element
 * @return {ZxQuery} A new *ZxQuery* object
 */
ZxQuery.prototype.eq = function (i) {
    var selection = this._selection;
    var resultSet = selection[i];
    if (arguments.length > 1) {
        resultSet = [];
        z$.each(arguments, function (k, v) {
            if (selection[v] != null)
                resultSet.push(selection[v])
        });
    }
    return new ZxQuery(resultSet);
};
/**
 * Select all descendants matching the given *DOM* query selector filter.
 * @param {string} selector A valid *DOM* query selector
 * @return {ZxQuery} A new *ZxQuery* object
 */
ZxQuery.prototype.find = function (selector) {
    return new ZxQuery(this._selection[0].querySelectorAll(selector));
};
/**
 * Iterate through all *DOM* elements in the selection.
 * The context object *this*, passed to the
 * *iterationCallback*`(index, item)`, will be the
 * *DOM* element corresponding the current iteration.
 * `index` will be the iteration count, and `item`
 * the current Element. The context `this` will be a `{ZxQuery}`
 * instance wrapping the current `item`.
 *
 * If the callback returns *false*, the iteration loop will interrupt.
 * @param {ZxQuery~instanceIterationCallback} iterationCallback The callback *fn* to call at each iteration
 * @return {ZxQuery} The *ZxQuery* object itself
 */
ZxQuery.prototype.each = function (iterationCallback) {
    z$.each(this._selection, iterationCallback);
    return this;
};
/**
 * Gets or sets the given element attribute.
 * @param {string|JSON} attr The attribute name
 * @param {string|undefined} [val] The attribute value
 * @return {string|ZxQuery} The *attr* attribute value when no *val* specified, otherwise the *ZxQuery* object itself
 */
ZxQuery.prototype.attr = function (attr, val) {
    var _t = this;
    if (typeof attr === 'object') {
        z$.each(attr, function (i, v) {
            _t.each(function (k, el) {
                el.setAttribute(i, v);
            });
        });
    } else if (typeof val == 'undefined')
        return this._selection[0].getAttribute(attr);
    else if (val === null)
        this._selection[0].removeAttribute(attr);
    else
        this.each(function (k, v) {
            this.get().setAttribute(attr, val);
        });
    return this;
};
/**
 * Trigger a component event.
 * @param {string} eventPath Path of the event to trigger.
 * @param {object} eventData Value of the event.
 * @return {ZxQuery} The *ZxQuery* object itself
 */
ZxQuery.prototype.trigger = function (eventPath, eventData) {
    var event;
    if (window.CustomEvent) {
        event = new CustomEvent(eventPath, {detail: eventData});
    } else {
        event = document.createEvent('CustomEvent');
        event.initCustomEvent(eventPath, true, true, eventData);
    }
    this.each(function (k, el) {
        el.dispatchEvent(event);
    });
    return this;
};
/**
 * Listen once for the given event.
 * @param {string} eventPath Event path
 * @param {function} eventHandler Event handler
 * @return {ZxQuery} The *ZxQuery* object itself
 */
ZxQuery.prototype.one = function (eventPath, eventHandler) {
    var fired = false;
    this.on(eventPath, function (a, b) {
        if (fired) return;
        fired = true;
        z$(this).off(eventPath, eventHandler);
        (eventHandler).call(this, a, b);
    });
    return this;
};
/**
 * Listen for the given event.
 * @param {string} eventPath Event path
 * @param {function} eventHandler Event handler
 * @return {ZxQuery} The *ZxQuery* object itself
 */
ZxQuery.prototype.on = function (eventPath, eventHandler) {
    var events = eventPath.match(/\S+/g) || [];
    this.each(function (k, el) {
        z$.each(events, function (k, ev) {
            // TODO: verify if this case apply to all events
            if (el.tagName.toLowerCase() === 'body')
                el = document;
            addEventHandler(el, ev, eventHandler);
        });
    });
    return this;
};
/**
 * Stop listening for the given event.
 * @param {string} eventPath Event path
 * @param {function} eventHandler Event handler
 * @return {ZxQuery} The *ZxQuery* object itself
 */
ZxQuery.prototype.off = function (eventPath, eventHandler) {
    var events = eventPath.match(/\S+/g) || [];
    this.each(function (k, el) {
        z$.each(events, function (k, ev) {
            // TODO: verify if this case apply to all events
            if (el.tagName.toLowerCase() === 'body')
                el = document;
            removeEventHandler(el, ev, eventHandler);
        });
    });
    return this;
};
/**
 * Un-register all event handlers registered for selected elements.
 * @return {ZxQuery}
 */
ZxQuery.prototype.reset = function () {
    this.each(function (k, el) {
        removeAllEventHandlers(el);
    });
    return this;
};
/**
 * Returns *true* if the element is empty.
 * @return {boolean} *true* if the element is empty, *false* otherwise
 */
ZxQuery.prototype.isEmpty = function () {
    return (this._selection[0].innerHTML.replace(/\s/g, '').length === 0);
};
/**
 * Gets coordinates and visibility status of the element.
 *
 * @return {{x, y, visible}}
 */
ZxQuery.prototype.position = function () {
    if (this._selection[0] != null)
        return z$.getPosition(this._selection[0])
    else // TODO: check this out; should prevent this from happening
        return { x: -1, y: -1, visible: false };
};

/**
 * Sets or gets the given css property.
 * @param {string|JSON} attr The CSS property name or JSON list of properties/values.
 * @param {string|undefined} [val] The attribute value.
 * @return {string|ZxQuery} The *attr* css value when no *val* specified, otherwise the *ZxQuery* object itself
 */
ZxQuery.prototype.css = function (attr, val) {
    var _t = this;
    if (typeof attr === 'object') {
        z$.each(attr, function (i, v) {
            _t.each(function (k, el) {
                el.style[i] = v;
            });
        });
    } else if (util.isNoU(val))
        return this._selection[0].style[attr];
    else
        _t.each(function (k, el) {
            el.style[attr] = val;
        });
    return this;
};
/**
 * Adds the given css class to the element class list.
 * @param {string} className The css class name.
 * @return {ZxQuery} The *ZxQuery* object itself
 */
ZxQuery.prototype.addClass = function (className) {
    var classes = className.match(/\S+/g) || [];
    z$.each(this._selection, function (k, el) {
        if (el.classList) {
            z$.each(classes, function (k, cl) {
                el.classList.add(cl);
            });
        } else el.className += ' ' + className;
    });
    return this;
};
/**
 * Returns *true* if the element contains the given css class.
 * @param {string} className The css class name.
 * @return {boolean} *true* if the element has the *className* css class, *false* otherwise
 */
ZxQuery.prototype.hasClass = function (className) {
    return z$.hasClass(this._selection[0], className);
};
/**
 * Removes the given css class to the element class list.
 * @param {string} className The css class name.
 * @return {ZxQuery} The *ZxQuery* object itself
 */
ZxQuery.prototype.removeClass = function (className) {
    var classes = className.match(/\S+/g) || [];
    z$.each(this._selection, function (k, el) {
        if (el.classList) {
            z$.each(classes, function (k, cl) {
                el.classList.remove(cl);
            });
        } else el.className = el.className.replace(new RegExp('(^|\\b)' + className.split(' ').join('|') + '(\\b|$)', 'gi'), ' ');
    });
    return this;
};
/**
 * Moves to the previous sibling in the DOM.
 * @return {ZxQuery} A new *ZxQuery* object with the previous sibling element.
 */
ZxQuery.prototype.prev = function () {
    return new ZxQuery(this._selection[0].previousElementSibling);
};
/**
 * Moves to the next sibling in the DOM.
 * @return {ZxQuery} A new *ZxQuery* object with the next sibling element.
 */
ZxQuery.prototype.next = function () {
    return new ZxQuery(this._selection[0].nextElementSibling);
};
/**
 * Gets or sets the HTML markup.
 * @param {string|undefined} [htmlText] HTML markup text.
 * @return {ZxQuery|string}
 */
ZxQuery.prototype.html = function (htmlText) {
    if (util.isNoU(htmlText))
        return this._selection[0].innerHTML;
    this.each(function (k, el) {
        el.innerHTML = htmlText;
    });
    return this;
};
/**
 * Gets or sets the checked attribute.
 * @param {boolean|undefined} [check] Value to assign to the 'checked' attribute.
 * @return {ZxQuery|boolean}
 */
ZxQuery.prototype.checked = function(check) {
    if (util.isNoU(check)) {
        var checked = this._selection[0].checked;
        return (checked != null && checked != 'false' && (checked || checked == 'checked'));
    }
    this.each(function (k, el) {
        el.checked = check;
    });
    return this;

};
/**
 * Gets or sets the 'value' attribute.
 * @param {string|undefined} [value] Value to assign to the 'value' attribute.
 * @return {ZxQuery|string}
 */
ZxQuery.prototype.value = function(value) {
    if (util.isNoU(value))
        return this._selection[0].value;
    this.each(function (k, el) {
        el.value = value;
    });
    return this;

};
/**
 * Appends the given element/markup to the current element.
 * @param {Object|ZxQuery|Array<Node>|Node|NodeList|string} el Element to append.
 * @return {ZxQuery} The *ZxQuery* object itself
 */
ZxQuery.prototype.append = function (el) {
    if (typeof el === 'string')
        this._selection[0].innerHTML += el;
    else
        this._selection[0].appendChild(el);
    return this;
};
/**
 * Insert the given child element before the one at the
 * specified index.
 *
 * @param index Position where to insert `el` Element.
 * @param {Object|ZxQuery|Array<Node>|Node|NodeList} el Element to insert.
 * @return {ZxQuery} The *ZxQuery* object itself
 */
ZxQuery.prototype.insert = function (index, el) {
    var target = this.children().get(index);
    if (target !== null)
        this._selection[0].insertBefore(el, target);
    else
        this._selection[0].appendChild(el);
    return this;
};
/**
 * Prepends the given element/markup to the current element.
 * @param {Object|ZxQuery|Array<Node>|Node|NodeList|string} el Element to append.
 * @return {ZxQuery} The *ZxQuery* object itself
 */
ZxQuery.prototype.prepend = function (el) {
    if (typeof el === 'string')
        this._selection[0].innerHTML = el + this._selection[0].innerHTML;
    else
        this._selection[0].insertBefore(el, this._selection[0].firstElementChild);
    return this;
};
/**
 * Re-attach element to its parent.
 * @return {ZxQuery}
 */
ZxQuery.prototype.attach = function () {
    var el = this._selection[0];
    if (el.parentNode == null && el.__zuix_oldParent != null) {
        z$(el.__zuix_oldParent).insert(el.__zuix_oldIndex, el);
        el.__zuix_oldParent = null;
        delete el.__zuix_oldParent;
        delete el.__zuix_oldIndex;
    }
    return this;
};
/**
 * Detach element from its parent.
 * @return {ZxQuery}
 */
ZxQuery.prototype.detach = function () {
    var el = this._selection[0];
    var parent = el.parentNode;
    if (parent != null) {
        el.__zuix_oldParent = parent;
        el.__zuix_oldIndex = Array.prototype.indexOf.call(parent.children, el);
        parent.removeChild(el);
        _log.t('Detached from parent', parent, el);
    }
    return this;
};
/**
 * Gets or sets the css `display` property.
 * @param {string|undefined} [mode] The display value.
 * @return {string|ZxQuery} The *display* css value when no *mode* specified, otherwise the *ZxQuery* object itself
 */
ZxQuery.prototype.display = function (mode) {
    if (util.isNoU(mode))
        return this._selection[0].style.display;
    z$.each(this._selection, function (k, el) {
        el.style.display = mode;
    });
    return this;
};
/**
 * Gets or sets the css `visibility` property.
 * @param {string|undefined} [mode] The visibility value.
 * @return {string|ZxQuery} The *visibility* css value when no *mode* specified, otherwise the *ZxQuery* object itself
 */
ZxQuery.prototype.visibility = function (mode) {
    if (util.isNoU(mode))
        return this._selection[0].style.visibility;
    z$.each(this._selection, function (k, el) {
        el.style.visibility = mode;
    });
    return this;
};
/**
 * Sets the css `display` property to ''.
 * @return {ZxQuery} The *ZxQuery* object itself
 */
ZxQuery.prototype.show = function () {
    return this.display('');
};
/**
 * Sets the css `display` property to 'none'.
 * @return {ZxQuery} The *ZxQuery* object itself
 */
ZxQuery.prototype.hide = function () {
    return this.display('none');
};

// --- ZxQuery factory members --- //

/**
 * Exported ZxQuery interface.
 *
 * @param [what] {Object|ZxQuery|Array<Node>|Node|NodeList|string|undefined}
 * @returns {ZxQuery}
 */
var z$ = function (what) {
    return new ZxQuery(what);
};
z$.find = function (filter) {
    return z$().find(filter);
};
/**
 * Iterate through all objects in the given `items` collection.
 * The context object *this*, passed to the
 * *iterationCallback*`(index, item)`, will be the
 * object corresponding the current iteration and
 * the `index` passed to the callback will be the iteration count.
 *
 * If the callback returns *false*, the iteration loop will interrupt.
 *
 * @param {Array<Object>|JSON} items Enumerable objects collection.
 * @param {ZxQuery~iterationCallback} iterationCallback The callback *fn* to call at each iteration
 * @return {z$} `this`.
 */
z$.each = function (items, iterationCallback) {
    var len = (items == null ? 0 : Object.keys(items).length);
    if (len > 0) {
        var count = 0;
        for (var i in items) {
            var item = items[i];
            if (item instanceof Element)
                item = z$(item);
            if (iterationCallback.call(item, i, items[i]) === false)
                break;
            count++;
            if (count >= len)
                break;
        }
    }
    return this;
};
z$.hasClass = function(el, className) {
    var classes = className.match(/\S+/g) || [];
    var success = false;
    z$.each(classes, function (k, cl) {
        if (el.classList)
            success = el.classList.contains(cl);
        else
            success = (new RegExp('(^| )' + cl + '( |$)', 'gi').test(el.className));
        if (success) return false;
    });
    return success;
};
z$.ajax = function ajax(opt) {
    var url;
    if (!util.isNoU(opt) && !util.isNoU(opt.url))
        url = opt.url;
    else
        url = opt;
    var xhr = new XMLHttpRequest();
    xhr.open('GET', url);
    xhr.onload = function () {
        if (xhr.status === 200) {
            if (util.isFunction(opt.success)) opt.success(xhr.responseText);
        }
        else {
            if (util.isFunction(opt.error)) opt.error(xhr);
        }
        if (util.isFunction(opt.then)) opt.then(xhr);
    };
    xhr.send();
    return this;
};
z$.wrapElement = function (containerTag, element) {
    //$(element).wrap($('<'+containerTag+'/>'));
    //return element;
    /** @type Element */
    var container = document.createElement(containerTag);
    if (typeof element === 'string')
        container.innerHTML = element;
    else
    // TODO: test this, it may not work
        container.appendChild(element);
    return container;
};
z$.wrapCss = function (wrapperRule, css) {
    var wrapReX = /([.,\w])([^/{};]*)({)/g;
    var r, result = null, wrappedCss = '';
    while (r = wrapReX.exec(css)) {
        if (result != null) {
            var rule = css.substring(result.index, r.index);
            var splitReX = /(.*)\{([^}]+)[}]/g; // [^{]
            var ruleParts = splitReX.exec(rule);
            if (ruleParts != null && ruleParts.length > 1) {
                var classes = ruleParts[1].split(',');
                z$.each(classes, function (k, v) {
                    if (v.replace(' ', '') == '.') v = ''; // <-- `.` it means 'self' (the container itself)
                    wrappedCss += '\n' + wrapperRule + '\n' + v;
                    if (k < classes.length - 1)
                        wrappedCss += ', ';
                });
                wrappedCss += ' {' + ruleParts[2] + '}\n';
            } else {
                _log.w('z$.wrapCss was unable to parse rule.', ruleParts, rule);
            }
        }
        result = r;
    }
    if (result != null)
        wrappedCss += wrapperRule + ' ' + css.substring(result.index);
    if (wrappedCss != '') {
        css = wrappedCss;
    }
    return css;
};
z$.appendCss = function (css, target, cssId) {
    var style = null, head;
    if (typeof css === 'string') {
        // output css
        head = document.head || document.getElementsByTagName('head')[0];
        style = document.createElement('style');
        style.type = 'text/css';
        style.id = cssId;
        if (style.styleSheet)
            style.styleSheet.cssText = css;
        else
            style.appendChild(document.createTextNode(css));
    } else if (css instanceof Element) style = css;
    // remove previous style node
    if (!util.isNoU(target))
        head.removeChild(target);
    if (!util.isNoU(style))
        head.appendChild(style);
    return style;
};
z$.replaceBraces = function (html, callback) {
    var outHtml = '', matched = 0, currentIndex = 0;
    var tags = new RegExp(/[^{}]+(?=})/g),
        result;
    while (result = tags.exec(html)) {
        var value = '{'+result[0]+'}';
        if (typeof callback === 'function') {
            var r = callback(result[0]);
            if (!util.isNoU(r)) {
                value = r;
                matched++;
            }
        }
        outHtml += html.substr(currentIndex, result.index-currentIndex-1)+value;
        currentIndex = result.index+result[0].length+1;
    }
    if (matched > 0) {
        outHtml += html.substr(currentIndex);
        return outHtml;
    }
};
z$.getClosest = function (elem, selector) {
    // Get closest match
    for (; elem && elem !== document; elem = elem.parentNode) {
        if (elem.matches(selector)) return elem;
    }
    return null;
};
z$.getPosition = function (el) {
    var visible = z$.isInView(el);
    var x = 0, y = 0;
    while (el) {
        if (el.tagName == "BODY") {
            // deal with browser quirks with body/window/document and page scroll
            var scrollX = el.scrollLeft || document.documentElement.scrollLeft;
            var scrollY = el.scrollTop || document.documentElement.scrollTop;
            x += (el.offsetLeft - scrollX + el.clientLeft);
            y += (el.offsetTop - scrollY + el.clientTop);
        } else {
            // for all other non-BODY elements
            x += (el.offsetLeft - el.scrollLeft + el.clientLeft);
            y += (el.offsetTop - el.scrollTop + el.clientTop);
        }
        el = el.offsetParent;
    }
    return {
        x: x,
        y: y,
        visible: visible
    };
};
z$.isInView = function (el) {
    if (el.offsetParent === null)
        return false;
    var rect = el.getBoundingClientRect();
    return rect.bottom > 0 && rect.right > 0
        && rect.left < (window.innerWidth || document.documentElement.clientWidth) /* or $(window).width() */
        && rect.top < (window.innerHeight || document.documentElement.clientHeight);
    /* or $(window).height() */
};
z$.scrollTo = function(el, targetY) {
    if (targetY === 0 || targetY == null)
        return;
    var duration = 500;
    var scrollTop = el.scrollTop+targetY - 56;
    var scrollOffset = el.scrollTop-scrollTop;
    el.firstElementChild.style.transition = 'transform '+duration+'ms ease';
    if (typeof el.firstElementChild.style.WebkitTransform !== 'undefined')
        el.firstElementChild.style.WebkitTransform = "translate(0, " + (scrollOffset) + "px)";
    else if (typeof el.firstElementChild.style.MozTransform !== 'undefined')
        el.firstElementChild.style.MozTransform= "translate(0, " + (scrollOffset) + "px)";
    else
        el.firstElementChild.style.transform = "translate(0, " + (scrollOffset) + "px)";
    window.setTimeout(function () {
        // TODO: should backup and restore old value
        if (typeof el.firstElementChild.style.WebkitTransform !== 'undefined')
            el.firstElementChild.style.WebkitTransform = "";
        else if (typeof el.firstElementChild.style.MozTransform !== 'undefined')
            el.firstElementChild.style.MozTransform= "";
        else
            el.firstElementChild.style.transform = "";
        el.firstElementChild.style.transition = '';
        el.scrollTop = scrollTop;
    }, duration);
};

z$.ZxQuery = ZxQuery;

// Element.matches() polyfill
if (!Element.prototype.matches) {
    Element.prototype.matches =
        Element.prototype.matchesSelector ||
        Element.prototype.mozMatchesSelector ||
        Element.prototype.msMatchesSelector ||
        Element.prototype.oMatchesSelector ||
        Element.prototype.webkitMatchesSelector ||
        function (s) {
            var matches = (this.document || this.ownerDocument).querySelectorAll(s),
                i = matches.length;
            while (--i >= 0 && matches.item(i) !== this) {
            }
            return i > -1;
        };
}
// window.CustomEvent polyfill for IE>=9
(function () {
    if ( typeof window.CustomEvent === "function" ) return false;
    function CustomEvent ( event, params ) {
        params = params || { bubbles: false, cancelable: false, detail: undefined };
        var evt = document.createEvent( 'CustomEvent' );
        evt.initCustomEvent( event, params.bubbles, params.cancelable, params.detail );
        return evt;
    }
    CustomEvent.prototype = window.Event.prototype;
    window.CustomEvent = CustomEvent;
})();

module.exports =  z$;

},{"./Logger":2,"./Util.js":4}],6:[function(_dereq_,module,exports){
/**
 * @license
 * Copyright 2015-2017 G-Labs. All Rights Reserved.
 *         https://genielabs.github.io/zuix
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

/**
 *
 *  ZUIX, Javascript library for component-based development.
 *        https://genielabs.github.io/zuix
 *
 * @author Generoso Martello <generoso@martello.com>
 */

"use strict";

// TODO: detect whether running in a browser enviroment or not
(function (root, factory) {
    if (typeof define === 'function' && define.amd) {
        // AMD. Register as an anonymous module.
        define('zuix', function () {
            return (root.zuix = (factory).call(root));
        });
    } else if (typeof module === 'object' && module.exports) {
        // Node
        module.exports = (factory).call(root);
    } else {
        // Browser globals
        root.zuix = (factory).call(root);
    }
}(this, _dereq_('./zuix/Zuix.js')));

},{"./zuix/Zuix.js":10}],7:[function(_dereq_,module,exports){
/**
 * Copyright 2015-2017 G-Labs. All Rights Reserved.
 *         https://genielabs.github.io/zuix
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

/**
 *
 *  This file is part of
 *  ZUIX, Javascript library for component-based development.
 *        https://genielabs.github.io/zuix
 *
 * @author Generoso Martello <generoso@martello.com>
 */

"use strict";

var _log =
    _dereq_('../helpers/Logger')('ComponentContext.js');
var z$ =
    _dereq_('../helpers/ZxQuery');
var util =
    _dereq_('../helpers/Util');

/***
 * TODO: describe this class...
 *
 * @param {ContextOptions} options The context options.
 * @param {function} [eventCallback] Event routing callback.
 * @returns {ComponentContext} The component context instance.
 * @constructor
 */

function ComponentContext(options, eventCallback) {

    this._options = null;
    this.contextId = (options == null || options.contextId == null) ? null : options.contextId;
    this.componentId = null;
    this.trigger = function(context, eventPath, eventValue) {
        if (typeof eventCallback === 'function')
            eventCallback(context, eventPath, eventValue);
    };

    /** @protected */
    this._container = null;

    /** @protected */
    this._model = null;
    /** @protected */
    this._view = null;
    /** @protected */
    this._css = null;
    /** @protected */
    this._style = null;
    /**
     * @protected
     * @type {ContextControllerHandler}
     */
    this._controller = null;

    /**
     * Define the local behavior handler for this context instance only.
     * Any global behavior matching the same `componentId` will be overridden.
     *
     * @function behavior
     * @param handler_fn {function}
     */
    this.behavior = null;

    /** @protected */
    this._eventMap = [];
    /** @protected */
    this._behaviorMap = [];

    /**
     * --@-protected
     * @type {ContextController}
     */
    this._c = null;

    this.options(options);

    return this;
}
/**
 * Gets/Sets the component's container element.
 * Returns the current component element if no
 * argument is passed, the {ComponentContext} itself
 * otherwise.
 *
 * @param {Element} [container] The container element.
 * @returns {ComponentContext|Element}
 */
ComponentContext.prototype.container = function (container) {
    // TODO: should automatically re-attach view to the new parent?
    if (container == null) return this._container;
    else if (container instanceof z$.ZxQuery)
        container = container.get();
    this._container = container;
    return this;
};

/**
 * Gets/Sets the component's view element.
 * If an *HTML* string is passed, the the view element
 * will be a new `div` wrapping the given markup.
 * Returns the current view element if no
 * argument is passed, the {ComponentContext} itself otherwise.
 *
 * @param {Element|string|undefined} [view] The view *HTML* string or element.
 * @returns {ComponentContext|Element}
 */
ComponentContext.prototype.view = function (view) {
    if (typeof view === 'undefined') return this._view;
    else if (view instanceof z$.ZxQuery)
        view = view.get();

    _log.t(this.componentId, 'view:attach', 'timer:view:start');
    if (typeof view === 'string') {
        // load view from HTML source

        // trigger `html:parse` hook before assigning content to the view
        var hookData = {content: view};
        this.trigger(this, 'html:parse', hookData);
        view = hookData.content;

        if (this._container != null) {
            // append view content to the container
            this._view = this._container;
            this._view.innerHTML += view;
        } else {
            var viewDiv = z$.wrapElement('div', view);
            if (this._view != null)
                this._view.innerHTML = viewDiv.innerHTML;
            else this._view = viewDiv;
        }

        z$(this._view).find('script').each(function (i, el) {
            if (this.attr('zuix-loaded') !== 'true') {
                this.attr('zuix-loaded', 'true');
                /*if (el.src != null && el.src.length > 0) {
                    var clonedScript = document.createElement('script');
                    clonedScript.setAttribute('zuix-loaded', 'true');
                    clonedScript.onload = function () {
                        // TODO: ...
                    };
                    if (!util.isNoU(this.type) && this.type.length > 0)
                        clonedScript.type = this.type;
                    if (!util.isNoU(this.text) && this.text.length > 0)
                        clonedScript.text = this.text;
                    if (!util.isNoU(this.src) && this.src.length > 0)
                        clonedScript.src = this.src;
                    this.get().parentNode.insertBefore(clonedScript, this.get());
                } else */
                    (eval).call(window, el.innerHTML);
            }
        });

        // trigger `view:process` hook when the view is ready to be processed
        this.trigger(this, 'view:process', z$(this._view));

    } else {
        if (view instanceof z$.ZxQuery)
            view = view.get();
        // load inline view
        if (this._container != null) {
            this._view = z$.wrapElement('div', view.outerHTML).firstElementChild;
            this._view.removeAttribute('data-ui-view');
            this._container.appendChild(this._view);
            this._view = this._container;
        } else this._view = view;
    }

    var v = z$(this._view);
    if (this._options.css === false)
    // disable local css styling for this instance
        v.addClass('zuix-css-ignore');
    else
    // enable local css styling
        v.removeClass('zuix-css-ignore');

    this.modelToView();

    _log.t(this.componentId, 'view:attach', 'timer:view:stop');
    return this;
};

/**
 * Gets/Sets the component's view style.
 * The `css` argument can be a string containing all
 * styles definitions or a reference to a style
 * element. When a string is passed the css
 * is linked to the `componentId` attribute so that
 * its styles will be only applied to the component
 * container.
 * If no argument is given, then the current style
 * element is returned.
 *
 * @example
 * <small>Example - JavaScript</small>
 * <pre><code class="language-js">
 * ctx.style("p { font-size: 120%; } .hidden { display: 'none'; }");
 * </code></pre>
 *
 * @param {string|Element|undefined} [css] The CSS string or element.
 * @returns {ComponentContext|Element}
 */
ComponentContext.prototype.style = function (css) {
    if (typeof css === 'undefined') return this._style;
    _log.t(this.componentId, 'view:style', 'timer:view:start');
    if (css == null || css instanceof Element) {

        this._css = (css instanceof Element) ? css.innerText : css;
        this._style = z$.appendCss(css, this._style, this.componentId);

    } else if (typeof css === 'string') {

        // store original unparsed css (might be useful for debugging)
        this._css = css;

        // nest the CSS inside [data-ui-component='<componentId>']
        // so that the style is only applied to this component type
        css = z$.wrapCss('[data-ui-component="' + this.componentId + '"]:not(.zuix-css-ignore)', css);

        // trigger `css:parse` hook before assigning content to the view
        var hookData = { content: css };
        this.trigger(this, 'css:parse', hookData);
        css = hookData.content;

        // output css
        this._style = z$.appendCss(css, this._style, this.componentId);

    }
    // TODO: should throw error if ```css``` is not a valid type
    _log.t(this.componentId, 'view:style', 'timer:view:stop');
    return this;
};
/**
 * Gets/Sets the component's data model.
 *
 * @example
 * <small>Example - JavaScript</small>
 * <pre><code class="language-js">
 * ctx.model({
 *      title: 'Thoughts',
 *      message: 'She stared through the window at the stars.'
 *  });
 * </code></pre>
 *
 * @param {object|undefined} [model] The model object.
 * @returns {ComponentContext|object}
 */
ComponentContext.prototype.model = function (model) {
    if (typeof model === 'undefined') return this._model;
    else this._model = model; // model can be set to null
    this.modelToView();
    // call controller's `update` method when model is updated
    if (this._c != null && util.isFunction(this._c.update))
        this._c.update.call(this._c);
    return this;
};
/**
 * Gets/Sets the controller handler function.
 *
 * @example
 * <small>Example - JavaScript</small>
 * <pre><code class="language-js">
 * ctx.controller(function(cp) {
 *      cp.create = function() {
 *           cp.view().html('Hello World!');
 *      };
 *      // ...
 *  });
 * </code></pre>
 *
 * @param {ContextControllerHandler|undefined} [controller] The controller handler function.
 * @returns {ComponentContext|ContextControllerHandler}
 */
ComponentContext.prototype.controller = function (controller) {
    if (typeof controller === 'undefined') return this._controller;
    // TODO: should dispose previous context controller first
    else this._controller = controller; // can be null
    return this;
};

/**
 * Gets/Sets the component options.
 *
 * @param {ContextOptions|undefined} options The JSON options object.
 * @return {ComponentContext|object}
 */
ComponentContext.prototype.options = function (options) {
    if (options == null)
        return this._options;
    var o = this._options = this._options || {};
    z$.each(options, function (k, v) {
        o[k] = v;
    });
    if (o.componentId != null)
        this.componentId = o.componentId;
    this.container(o.container);
    this.view(o.view);
    if (typeof o.css !== 'undefined')
        this.style(o.css);
    this.controller(o.controller);
    this.model(o.model);
    // map options to element's attributes
    if (o.lazyLoad && this.container() != null) {
        this.container().setAttribute('data-ui-context', this.contextId);
        this.container().setAttribute('data-ui-load', this.componentId);
        this.container().setAttribute('data-ui-lazyload', 'true');
    }
    return this;
};

/**
 * Listen for a component event.
 *
 * @example
 * <small>Example - JavaScript</small>
 * <pre><code class="language-js">
 * ctx.on('item:share', function(evt, data) { ... });
 * </code></pre>
 *
 * @param {string} eventPath The event path.
 * @param {EventCallback} eventHandler The event handler function.
 * @return {ComponentContext} The ```{ComponentContext}``` object itself.
 */
ComponentContext.prototype.on = function (eventPath, eventHandler) {
    // TODO: throw error if _c (controller instance) is not yet ready
    this._c.on(eventPath, eventHandler);
    return this;
};
/**
 * Load the `.css` file and replace the component's view style.
 * If no `options.path` is specified, it will try to load
 * the file with the same base-name as the `componentId`.
 *
 * @example
 * <small>Example - JavaScript</small>
 * <pre><code class="language-js">
 * // loads 'path/to/component_name.css' by default
 * ctx.loadCss();
 * // or loads the view's css with options
 * ctx.loadCss({
 *     path: 'url/of/style/file.css',
 *     success: function() { ... },
 *     error: function(err) { ... },
 *     then: function() { ... }
 * });
 * </code></pre>
 *
 * @private
 * @param {object} [options] The options object.
 * @param {boolean} [enableCaching] Enable HTTP
 * @return {ComponentContext} The ```{ComponentContext}``` object itself.
 */
ComponentContext.prototype.loadCss = function (options, enableCaching) {
    var context = this;
    if (util.isNoU(options)) options = {};
    if (!util.isNoU(options.caching))
        enableCaching = options.caching;
    var cssPath = context.componentId + '.css';
    if (!util.isNoU(options.path))
        cssPath = options.path;
    if (!enableCaching)
        cssPath += '?'+new Date().getTime();
    z$.ajax({
        url: cssPath,
        success: function (viewCss) {
            context.style(viewCss);
            if (util.isFunction(options.success))
                (options.success).call(context, viewCss);
        },
        error: function (err) {
            _log.e(err, context);
            if (util.isFunction(options.error))
                (options.error).call(context, err);
        },
        then: function () {
            if (util.isFunction(options.then))
                (options.then).call(context);
        }
    });
    return this;
};
/**
 * Load the `.html` file and replace the component's view markup.
 * If no `options.path` is specified, it will try to load the
 * file with the same base-name as the `componentId`.
 *
 * @example
 * <small>Example - JavaScript</small>
 * <pre><code class="language-js">
 * // loads 'path/to/component_name.html' by default
 * ctx.loadHtml();
 * // or loads the view's html with options
 * ctx.loadHtml({
 *     path: 'url/of/view/file.html',
 *     success: function() { ... },
 *     error: function(err) { ... },
 *     then: function() { ... }
 * });
 * </code></pre>
 *
 * @private
 * @param {object} [options] The options object.
 * @param {boolean} [enableCaching] Enable HTTP caching
 * @return {ComponentContext} The ```{ComponentContext}``` object itself.
 */
ComponentContext.prototype.loadHtml = function(options, enableCaching) {
    var context = this;
    var htmlPath = context.componentId;
    if (util.isNoU(options)) options = {};
    if (!util.isNoU(options.caching))
        enableCaching = options.caching;
    if (!util.isNoU(options.path))
        htmlPath = options.path;
    // TODO: check if view caching is working in this case too
    var inlineView = z$().find('[data-ui-view="' + htmlPath + '"]:not([data-ui-component*=""])');
    if (inlineView.length() > 0) {
        var inlineElement = inlineView.get(0);
        if (context.view() === inlineElement || (context.container() != null && context.container().contains(inlineElement)))
            // TODO: test this case
            context.view(inlineElement);
        else
            context.view(inlineElement.outerHTML);
        var html = context.view().innerHTML;
        if (util.isFunction(options.success))
            (options.success).call(context, html);
        if (util.isFunction(options.then))
            (options.then).call(context);
    } else {
        var cext = util.isNoU(options.cext) ? '.html' : options.cext;
        if (htmlPath == context.componentId)
            htmlPath += cext + (!enableCaching ? '?' + new Date().getTime() : '');
        z$.ajax({
            url: htmlPath,
            success: function (viewHtml) {
                context.view(viewHtml);
                if (util.isFunction(options.success))
                    (options.success).call(context, viewHtml);
            },
            error: function (err) {
                _log.e(err, context);
                if (util.isFunction(options.error))
                    (options.error).call(context, err);
            },
            then: function () {
                if (util.isFunction(options.then))
                    (options.then).call(context);
            }
        });
    }
    return this;
};
/**
 * Create the data model starting from ```data-ui-field```
 * elements declared in the component's view.
 *
 * @return {ComponentContext} The ```{ComponentContext}``` object itself.
 */
ComponentContext.prototype.viewToModel = function() {
    _log.t(this.componentId, 'view:model', 'timer:vm:start');
    var _t = this;
    this._model = {};
    // create data model from inline view fields
    z$(this._view).find('[data-ui-field]').each(function(i, el) {
        if (this.parent('pre,code').length() > 0)
            return true;
        var name = this.attr('data-ui-field');
        var value =
            // TODO: this is a work around for IE where "el.innerHTML" is lost after view replacing
            (!util.isNoU(el.innerHTML) && util.isIE())
                ? el.cloneNode(true) : el;
        // dotted field path
        if (name.indexOf('.')>0) {
            var path = name.split('.');
            var cur = _t._model;
            for (var p = 0; p < path.length - 1; p++) {
                if (typeof cur[path[p]] === 'undefined')
                    cur[path[p]] = {};
                cur = cur[path[p]];
            }
            cur[path[path.length - 1]] = value;
        } else _t._model[name] = value;
    });
    _log.t(this.componentId, 'view:model', 'timer:vm:stop');
    return this;
};
/**
 * Copy values from the data model to the ```data-ui-field``
 * elements declared in the component's view.
 *
 * @return {ComponentContext} The ```{ComponentContext}``` object itself.
 */
ComponentContext.prototype.modelToView = function () {
    _log.t(this.componentId, 'model:view', 'timer:mv:start');
    if (this._view != null && this._model != null) {
        var _t = this;
        z$(this._view).find('[data-ui-field]').each(function(i, el) {
            if (this.parent('pre,code').length() > 0)
                return true;
            var boundField = this.attr('data-bind-to');
            if (boundField == null)
                boundField = this.attr('data-ui-field');
            if (typeof _t._model === 'function')
                (_t._model).call(z$(_t._view), this, boundField);
            else {
                var boundData = util.propertyFromPath(_t._model, boundField);
                if (typeof boundData === 'function') {
                    (boundData).call(z$(_t._view), this, boundField);
                } else if (boundData != null) {
                    // try to guess target property
                    switch (el.tagName.toLowerCase()) {
                        // TODO: complete binding cases
                        case 'img':
                            el.src = (!util.isNoU(boundData.src) ?  boundData.src :
                                (!util.isNoU(boundData.innerHTML) ? boundData.innerHTML : boundData));
                            if (boundData.alt) el.alt = boundData.alt;
                            break;
                        case 'a':
                            el.href = (!util.isNoU(boundData.href) ? boundData.href :
                                (!util.isNoU(boundData.innerHTML) ? boundData.innerHTML : boundData));
                            if (boundData.title) el.title = boundData.title;
                            break;
                        case 'input':
                            el.value = (!util.isNoU(boundData.value) ? boundData.value :
                                (!util.isNoU(boundData.innerHTML) ? boundData.innerHTML : boundData));
                            break;
                        default:
                            el.innerHTML = (!util.isNoU(boundData.innerHTML) ? boundData.innerHTML : boundData);
                    }
                }
            }
        });
    }
    _log.t(this.componentId, 'model:view', 'timer:mv:stop');
    return this;
};

module.exports = ComponentContext;
},{"../helpers/Logger":2,"../helpers/Util":4,"../helpers/ZxQuery":5}],8:[function(_dereq_,module,exports){
/**
 * Copyright 2015-2017 G-Labs. All Rights Reserved.
 *         https://genielabs.github.io/zuix
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

/**
 *
 *  This file is part of
 *  ZUIX, Javascript library for component-based development.
 *        https://genielabs.github.io/zuix
 *
 * @author Generoso Martello <generoso@martello.com>
 */

"use strict";


/**
 *
 * @param {Element|ZxQuery|undefined} [element] Scan and process loadable elements inside `element`.
 * @param {Element|undefined} [child] Process only the specified `child` of `element`.
 * @return {Componentizer}
 */
Componentizer.prototype.componentize = function (element, child) {
    zuix.trigger(this, 'componentize:begin');
    if (child != null) {
        var cache = getElementCache(element);
        if (cache == null)
            setElementCache(element, [child]);
        else cache.push(child);
    } else {
        addRequest(element);
    }
    loadNext(element);
    return this;
};

/**
 *
 * @return {boolean}
 */
Componentizer.prototype.willLoadMore = function () {
    return _componentizeQueue.length > 0 || _componentizeRequests.length > 0;
};

/**
 * Enable/Disable lazy-loading, or get current value.
 *
 * @param {boolean} [enable]
 * @return {boolean} *true* if lazy-loading is enabled, *false* otherwise.
 */
Componentizer.prototype.lazyLoad = function (enable) {
    return lazyLoad(enable);
};

/**
 *
 * @param {Zuix} zuixInstance
 * @return {Componentizer}
 */
Componentizer.prototype.setHost = function (zuixInstance) {
    zuix = zuixInstance;
    return this;
};

module.exports = function () {
    return new Componentizer();
};


// ---------------------------------------------


var _log =
    _dereq_('../helpers/Logger')('ComponentContext.js');
var util =
    _dereq_('../helpers/Util');
var z$ =
    _dereq_('../helpers/ZxQuery');

/** @type {Zuix} **/
var zuix = null;

/** @private */
var _componentizeRequests = [];
/** @private */
var _componentizeQueue = [],
    /** @private */
    _disableLazyLoading = false,
    /** @private */
    _lazyElements = [],
    _lazyContainers = [];

// Browser Agent / Bot detection
/** @private */
var _isCrawlerBotClient = false;
if (navigator && navigator.userAgent)
    _isCrawlerBotClient = new RegExp(/bot|googlebot|crawler|spider|robot|crawling/i)
        .test(navigator.userAgent);
if (_isCrawlerBotClient)
    _log.d(navigator.userAgent, "is a bot, ignoring `lazy-loading` option.");

/** @private */
var TaskItem = function () {
    return {
        /** @typedef {Element} */
        element: null,
        /** @typedef {number} */
        priority: 0,
        /** @typedef {boolean} */
        visible: true,
        /** @typedef {boolean} */
        lazy: false
    }
};


// Components Loading Chain
var loader = _dereq_('./../helpers/AsynChain')({

    doWork: function (item, callback) {
        z$(item.element).one('component:ready', function () {
            callback();
        });
        return loadInline(item.element);
    },
    willBreak: function () {
        return false;
    },
    status: function (status) {
        switch (status) {
            case 'start':
                break;
            case 'done':
                loadNext();
                break;
        }
    }

});

function Componentizer() {
    // ...
}

function lazyLoad(enable) {
    if (enable != null)
        _disableLazyLoading = !enable;
    return !_isCrawlerBotClient && !_disableLazyLoading;
}

function addRequest(element) {
    if (element == null)
        element = document;
    if (!_componentizeRequests.indexOf(element))
        _componentizeRequests.push(element);
}

var _elementCache = [];
function setElementCache(element, waiting) {
    _elementCache.push({
        element: element,
        waiting: waiting
    });
}
function getElementCache(element) {
    for (var i = 0; i < _elementCache.length; i++) {
        var cache = _elementCache[i];
        if (cache.element === element)
            return cache.waiting;
    }
    return null;
}

function queueLoadables(element) {

    if (element == null && _componentizeRequests.length > 0)
        element = _componentizeRequests.unshift();

    if (element instanceof z$.ZxQuery)
        element = element.get();

    // Select all loadable elements
    var waitingLoad = getElementCache(element);
//    if (waitingLoad == null || waitingLoad.length == 0) {
    waitingLoad = z$(element).find('[data-ui-load]:not([data-ui-loaded=true]),[data-ui-include]:not([data-ui-loaded=true])');
    waitingLoad = Array.prototype.slice.call(waitingLoad._selection);
    setElementCache(element, waitingLoad);
//    }
    var waitingTasks = [];
    for (var w = 0; w < waitingLoad.length; w++) {
        var pri = parseInt(waitingLoad[w].getAttribute('data-ui-priority'));
        if (isNaN(pri)) pri = 0;
        var task = new TaskItem();
        task.element = waitingLoad[w];
        task.priority = pri; //w - ( 12 * ( w % 2 ) ) + ( pri * 73 ); // fuzzy pri
        waitingTasks.push(task);
    }
    var added = 0;
    // add selected elements to the requests queue
    for (var i = 0; i < waitingTasks.length; i++) {
        var alreadyAdded = false;
        for (var j = 0; j < _componentizeQueue.length; j++) {
            if (waitingTasks[i].element === _componentizeQueue[j].element) {
                alreadyAdded = true;
                break;
            }
        }
        if (!alreadyAdded) {
            _componentizeQueue.push(waitingTasks[i]);
            added++;
        }
    }

    _log.t('componentize:count', _componentizeQueue.length, added);

    if (added == 0 || (_componentizeRequests.length == 0 && _componentizeQueue.length == 0))
        zuix.trigger(this, 'componentize:end');
}

function getNextLoadable() {

    // sort by priority (elements with lower pri number get processed first)
    _componentizeQueue.sort(function (a, b) {
        return a.priority - b.priority;
    });
    var job = null, reinsert = [];
    var item = _componentizeQueue.length > 0 ? _componentizeQueue.shift() : null;
    while (item != null && item.element != null) {
        // defer element loading if lazy loading is enabled and the element is not in view
        var ls = lazyScrollCheck(item.element);
        if (lazyLoad() && ls.scroller !== false && item.element.getAttribute('data-ui-lazyload') != 'false') {
            item.lazy = true;
            item.visible = z$.getPosition(item.element).visible;
        } else {
            item.lazy = false;
            item.visible = true;
        }
        // ...
        if (item.element != null && item.element.getAttribute('data-ui-loaded') == 'true' || !item.visible) {
            if (!item.visible) reinsert.push(item);
            item = null;
        } else if (item != null && item.element != null && item.visible) {
            job = {
                item: item,
                cancelable: item.lazy
            };
            break;
        }
        if (_componentizeQueue.length > 0)
            item = _componentizeQueue.shift();
        else break;
    }
    Array.prototype.push.apply(_componentizeQueue, reinsert);
    return job;
}

function loadNext(element) {
    queueLoadables(element);
    var job = getNextLoadable();
    if (job != null)
        loader.append([job]);
}

/** @protected */
function loadInline(element) {

    var v = z$(element);
    if (v.attr('data-ui-loaded') === 'true' || v.parent('pre,code').length() > 0) {
        //_log.w("Skipped", element);
        return false;
    } else v.attr('data-ui-loaded', 'true');

    /** @type {ContextOptions} */
    var options = v.attr('data-ui-options');
    if (!util.isNoU(options)) {
        options = util.propertyFromPath(window, options);
        // copy passed options
        options = util.cloneObject(options) || {};
    } else options = {};

    // Automatic view/container selection
    if (util.isNoU(options.view) && !v.isEmpty()) {
        options.view = element;
        options.viewDeferred = true;
    } else if (util.isNoU(options.view) && util.isNoU(options.container) && v.isEmpty())
        options.container = element;

    var componentId = v.attr('data-ui-load');
    if (util.isNoU(componentId)) {
        // Static include should not have any controller
        componentId = v.attr('data-ui-include');
        v.attr('data-ui-component', componentId);
        // disable controller auto-loading
        if (util.isNoU(options.controller))
            options.controller = function () {
            }; // null
    }

    // inline attributes have precedence over ```options```

    var model = v.attr('data-bind-model');
    if (!util.isNoU(model) && model.length > 0)
        options.model = util.propertyFromPath(window, model);

    var contextId = v.attr('data-ui-context');
    if (!util.isNoU(contextId))
        options.contextId = contextId;

    var priority = parseInt(v.attr('data-ui-priority'));
    if (!util.isNoU(priority))
        options.priority = priority;

    var el = z$(element);
    el.one('component:ready', function () {
        addRequest(element);
        loadNext(element);
    });

    zuix.load(componentId, options);

    return true;
}


// ------------ Lazy Loading

function getLazyElement(el) {
    for (var l = 0; l < _lazyElements.length; l++) {
        var le = _lazyElements[l];
        if (le.element === el)
            return le;
    }
    return null;
}

function addLazyElement(el) {
    var le = {
        element: el,
        scroller: false
    };
    _lazyElements.push(le);
    return le;
}

function getLazyContainer(el) {
    for (var l = 0; l < _lazyContainers.length; l++) {
        var ls = _lazyContainers[l];
        if (ls.element === el)
            return ls;
    }
    return null;
}

function addLazyContainer(el) {
    var lc = {
        element: el,
        handler: false
    };
    _lazyContainers.push(lc);
    return lc;
}

function lazyScrollCheck(el) {
    // store a reference to its scroller container for lazy-loaded elements
    var ls = getLazyElement(el);
    if (ls == null) {
        ls = addLazyElement(el);
        var lazyContainer = z$.getClosest(el, '[data-ui-lazyload=scroll]');
        // override lazy loading if 'lazyload' is set to 'false' for the current element
        if (lazyContainer != null) {
            var lc = getLazyContainer(lazyContainer);
            if (lc == null) {
                lc = addLazyContainer(lazyContainer);
                // attach 'scroll' event handler to lazy-scroller
                var scrollWatcher = function (instance, lc) {
                    var lastScroll = new Date().getTime();
                    z$(lc).on('scroll', function () {
                        var now = new Date().getTime();
                        if (now - lastScroll > 100) {
                            lastScroll = now;
                            loadNext(lc);
                        }
                    });
                }(this, lazyContainer);
            }
            ls.scroller = (lc == null ? false : lc);
        }
    }
    return ls;
}

},{"../helpers/Logger":2,"../helpers/Util":4,"../helpers/ZxQuery":5,"./../helpers/AsynChain":1}],9:[function(_dereq_,module,exports){
/**
 * Copyright 2015-2017 G-Labs. All Rights Reserved.
 *         https://genielabs.github.io/zuix
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

/**
 *
 *  This file is part of
 *  ZUIX, Javascript library for component-based development.
 *        https://genielabs.github.io/zuix
 *
 * @author Generoso Martello <generoso@martello.com>
 */

"use strict";

var z$ =
    _dereq_('../helpers/ZxQuery');

/**
 * TODO: complete JSDoc
 *
 * @param {ComponentContext} context
 * @returns {ContextController}
 * @constructor
 */
function ContextController(context) {
    var _t = this;

    this._view = null;

    this.context = context;
    /** @type {function} */
/*    this.behavior = function () {
        return context.behavior;
    };*/

    /**
     * @protected
     * @type {!Array.<ZxQuery>}
     **/
    this._fieldCache = [];

    // Interface methods

    /** @type {function} */
    this.init = null;
    /** @type {function} */
    this.create = null;
    /** @type {function} */
    this.destroy = null;

    /** @protected */
    this._childNodes = [];
    /** @type {function} */
    this.saveView = function () {
        this.restoreView();
        this.view().children().each(function (i, el) {
            _t._childNodes.push(el);
        });
    };
    this.restoreView = function() {
        if (this._childNodes.length > 0) {
            _t.view().html('');
            z$.each(_t._childNodes, function (i, el) {
                _t.view().append(el);
            });
            this._childNodes.length = 0;
        }
    };

    this.on = function (eventPath, handler_fn) {
        this.addEvent(eventPath, handler_fn);
        return this;
    };
    /** @protected */
    this.mapEvent = function (eventMap, target, eventPath, handler_fn) {
        if (target != null) {
            target.off(eventPath, this.eventRouter);
            eventMap[eventPath] = handler_fn;
            target.on(eventPath, this.eventRouter);
        } else {
            // TODO: should report missing target
        }
    };
    /** @protected */
    this.eventRouter = function (e) {
        if (typeof context._behaviorMap[e.type] === 'function')
            context._behaviorMap[e.type].call(_t.view(), e, e.detail);
        if (typeof context._eventMap[e.type] === 'function')
            context._eventMap[e.type].call(_t.view(), e, e.detail);
        // TODO: else-> should report anomaly
    };

    // create event map from context options
    var options = context.options(), handler = null;
    if (options.on != null) {
        for (var ep in options.on) {
            handler = options.on[ep];
            _t.addEvent(ep, handler);
        }
    }
    // create behavior map from context options
    if (options.behavior != null) {
        for (var bp in options.behavior) {
            handler = options.behavior[bp];
            _t.addBehavior(bp, handler);
        }
    }

    context.controller().call(this, this);

    return this;
}

// TODO: add jsDoc
ContextController.prototype.addEvent = function (eventPath, handler_fn) {
    this.mapEvent(this.context._eventMap, this.view(), eventPath, handler_fn);
    return this;
};

// TODO: add jsDoc
ContextController.prototype.addBehavior = function (eventPath, handler_fn) {
    this.mapEvent(this.context._behaviorMap, this.view(), eventPath, handler_fn);
    return this;
};

/**
 * Gets elements in the component's view with `data-ui-field`
 * matching the given `fieldName`.
 *
 * @example
 *
 <small>**Example - View's HTML**</small>
 ```html
 <h1 data-ui-field="title">...</h1>
 <p data-ui-field="description">...</p>
 ```

 <small>**Example - JavaScript**</small>
 ```js
 cp.field('title')
 .html('Hello World!');
 var desc = cp.field('description');
 desc.html('The spectacle before us was indeed sublime.');
 ```
 *
 *
 * @param {!string} fieldName Value to match in the `data-ui-field` attribute.
 * @returns {ZxQuery} A `{ZxQuery}` object wrapping the matching element.
 */
ContextController.prototype.field = function (fieldName) {
    // this method is "attacched" from Zuix.js on controller initialization
    return null;
};
ContextController.prototype.clearCache = function () {
    this._fieldCache.length = 0;
};
/**
 * Gets the component's view or the view elements matching
 * the given `filter` in which case acts as a shorthand for
 * `cp.view().find(filter)`.
 *
 * @example
 * <small>Example - JavaScript</small>
 * <pre><code class="language-js">
 * // get all `checkbox` elements with `.checked` class.
 * var choices = cp.view('input[type="checkbox"].checked');
 * choices.removeClass('.checked');
 * // hide the component's view
 * cp.view().hide();
 * </code></pre>
 *
 * @param {(string|undefined)} [filter]
 * @return {ZxQuery}
 */
ContextController.prototype.view = function (filter) {
    // context view changed, dispose cached fields from previous attacched view
    if (this.context.view() != null || this._view !== this.context.view()) {
        this.clearCache();
        // TODO: !!!!
        // TODO: dispose also events on view change (!!!)
        // TODO: !!!!
        this._view = z$(this.context.view());
    }
    if (filter != null)
        return this._view.find(filter);
    else if (this._view !== null)
        return this._view;
    else
        throw({
            message: 'Not attacched to a view yet.',
            source: this
        });
};
/**
 * Gets/Sets the component's data model.
 *
 * @example
 * <small>Example - JavaScript</small>
 * <pre><code class="language-js">
 * var m = {
 *      title: 'Thoughts',
 *      message: 'She stared through the window at the stars.'
 *  };
 * cp.model(m);
 * cp.model().title = 'Changes';
 * console.log(cp.model().title);
 * </code></pre>
 *
 * @param {object|undefined} [model] The model object.
 * @return {ContextController|object}
 */
ContextController.prototype.model = function (model) {
    if (model == null)
        return this.context.model();
    else this.context.model(model);
    return this;
};
/**
 * Gets the component options.
 *
 * @return {object} The component options.
 */
ContextController.prototype.options = function () {
    return this.context.options();
};
/**
 * Triggers the component event `eventPath` with the given
 * `eventData` object. To listen to a component event use the
 * `{ComponentContext}.on(eventPath, handler)` method or
 * in case `isHook` is set to true, use the
 * `zuix.hook(eventPath, handler)` method.
 *
 * @example
 * <small>Example - JavaScript</small>
 * <pre><code class="language-js">
// somewhere inside the slide-show component controller
cp.trigger('slide:change', slideIndex);

// somewhere in a page hosting the slide-show component
// set component's event listeners
zuix.context('my-slide-show')
  .on('slide:change', function(slideIndex) { ... })
  .on(...);
 * </code></pre>
 *
 * @param {string} eventPath The event path.
 * @param {object} eventData The event data.
 * @param {boolean} [isHook] Trigger as global hook event.
 * @return {ContextController}
 */
ContextController.prototype.trigger = function (eventPath, eventData, isHook) {
    if (this.context._eventMap[eventPath] == null && isHook !== true)
        this.addEvent(eventPath, null);
    // TODO: ...
    if (isHook === true) {
        var target = this.context.container();
        if (target == null) target = this.context.view();
        if (target != null)
            z$(target)
                .trigger(eventPath, eventData);
        this.context.trigger(this.context, eventPath, eventData);
    } else
        this.view().trigger(eventPath, eventData);
    return this;
};
/**
 *  Expose in the component context a property or method
 *  defined in the controller.
 *
 * @example
 * <small>Example - JavaScript</small>
 * <pre data-line="5"><code class="language-js">
 * // somewhere in the `create` method of the {ContextController}
 * zuix.controller(function(cp){
 *   cp.create = function() {
 *     // ....
 *     cp.expose('setSlide', slide);
 *   }
 *   // ...
 *   function slide(slideIndex) { ... }
 *   // ...
 * });
 * // ...
 * // calling the exposed method from the instance of
 * // the component.
 * var ctx = zuix.context('my-slide-show');
 * ctx.setSlide(2);
 * </code></pre>
 *
 *
 * @param {string|JSON} methodName Name of the exposed function, or list of method names/functions.
 * @param {function} [handler] Reference to the controller member to expose.
 * @return {ContextController} The `{ContextController}` itself.
 */
ContextController.prototype.expose = function (methodName, handler) {
    if (typeof methodName === 'object') {
        var _t = this;
        z$.each(methodName, function (k, v) {
            _t.context[k] = v;
        });
    } else this.context[methodName] = handler;
    return this;
};
/**
 * Load the `.css` file and replace the component's view style.
 * If no `options.path` is specified, it will try to load
 * the file with the same base-name as the `componentId`.
 *
 * @example
 * <small>Example - JavaScript</small>
 * <pre><code class="language-js">
 * // loads 'path/to/component_name.css' by default
 * cp.loadCss();
 * // or loads the view's css with options
 * cp.loadCss({
 *     path: 'url/of/style/file.css',
 *     success: function() { ... },
 *     error: function(err) { ... },
 *     then: function() { ... }
 * });
 * </code></pre>
 *
 * @param {object} [options] The options object.
 * @return {ContextController} The ```{ContextController}``` object itself.
 */
ContextController.prototype.loadCss = function(options) {
    this.context.loadCss(options);
    return this;
};
/**
 * Load the `.html` file and replace the component's view markup.
 * If no `options.path` is specified, it will try to load the
 * file with the same base-name as the `componentId`.
 *
 * @example
 * <small>Example - JavaScript</small>
 * <pre><code class="language-js">
 * // loads 'path/to/component_name.html' by default
 * cp.loadHtml();
 * // or loads the view's html with options
 * cp.loadHtml({
 *     path: 'url/of/view/file.html',
 *     success: function() { ... },
 *     error: function(err) { ... },
 *     then: function() { ... }
 * });
 * </code></pre>
 *
 * @param {object} [options] The options object.
 * @return {ContextController} The ```{ContextController}``` object itself.
 */
ContextController.prototype.loadHtml = function(options) {
    this.saveView();
    this.context.loadHtml(options);
    return this;
};
/**
 * this member is "attacched" from Zuix.js on controller initialization
 * @type {Logger} */
ContextController.prototype.log = {};
/**
 * Register as default controller for the given component type.
 *
 * @example
<small>**Example - JavaScript**</small>
<pre data-line="6"><code class="language-js">
// Controller of component 'path/to/component_name'
var ctrl = zuix.controller(function(cp) {
    // `cp` is the {ContextController}
    cp.create = function() { ... };
    cp.destroy = function() { ... }
}).for('path/to/component_name');
</pre></code>
 *
 * @param {!string} componentId Component identifier.
 * @return {ContextController} The `{ContextController}` itself.
 */
ContextController.prototype.for = function (componentId) { return this; };

module.exports = ContextController;
},{"../helpers/ZxQuery":5}],10:[function(_dereq_,module,exports){
/**
 * Copyright 2015-2017 G-Labs. All Rights Reserved.
 *         https://genielabs.github.io/zuix
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

/**
 *
 *  ZUIX, Javascript library for component-based development.
 *        https://genielabs.github.io/zuix
 *
 * @author Generoso Martello <generoso@martello.com>
 */

"use strict";

var _log =
    _dereq_('../helpers/Logger')('Zuix.js');
var util =
    _dereq_('../helpers/Util');
var z$ =
    _dereq_('../helpers/ZxQuery');
var TaskQueue =
    _dereq_('../helpers/TaskQueue');
var ComponentContext =
    _dereq_('./ComponentContext');
var ContextController =
    _dereq_('./ContextController');
var _componentizer =
    _dereq_('./Componentizer')();

/**
 * @const
 */
var ZUIX_FIELD_ATTRIBUTE = 'data-ui-field';

/**
 * @private
 * @type {!Array.<ComponentCache>}
 */
var _componentCache = [];

/** @private */
var _contextSeqNum = 0;
/**
 * @private
 * @type {!Array<ComponentContext>}
 */
var _contextRoot = [];

/** @private */
var resourceLoadTask = [];

/** @private */
var _hooksCallbacks = [];

/** @private */
var _globalHandlers = {};

/** @private */
var _enableHttpCaching = true;

/** @private **/
var _componentTask = [];
/** @private **/
var taskQueue = function(tid) {
    if (util.isNoU(_componentTask[tid])) {
        _componentTask[tid] = new TaskQueue(function (tq, eventPath, eventValue) {
            trigger(tq, eventPath, eventValue);
        });
    }
    return _componentTask[tid];
};
/** @private **/
var _pendingResourceTask = {};

/**
 *  ZUIX, Javascript library for component-based development.
 *
 * @class Zuix
 * @constructor
 * @returns {Zuix}
 */
function Zuix() {
    _componentizer.setHost(this);
    /**
     * @private
     * @type {!Array.<ZxQuery>}
     **/
    this._fieldCache = [];
    return this;
}

/**
 * Initializes a controller ```handler```.
 *
 * @private
 * @param handler {ContextControllerHandler}
 * @return {ContextControllerHandler}
 */
function controller(handler) {
    if (typeof handler['for'] !== 'function')
        handler['for'] = function (componentId) {
            _globalHandlers[componentId] = handler;
            return handler;
        };
    return handler;
}

/**
 * Gets elements with `data-ui-field`
 * attribute matching the given `fieldName`.
 *
 * @private
 * @param {!string} fieldName Value to match in the `data-ui-field` attribute.
 * @param {!Element|!ZxQuery} [container] Starting DOM element for this search (**default:** *document*)
 * @param {object} [context] The context
 * @return {ZxQuery}
 */
function field(fieldName, container, context) {
    if (util.isNoU(context))
        context = this;
    if (context._fieldCache == null)
        context._fieldCache = {};

    var el = null;
    if (typeof context._fieldCache[fieldName] === 'undefined') {
        el = z$(container).find('[' + ZUIX_FIELD_ATTRIBUTE + '="' + fieldName + '"]');
        if (el != null)
            context._fieldCache[fieldName] = el;
    } else el = context._fieldCache[fieldName];

    return el;
}

/**
 * Loads a component with the given options.
 *
 * @private
 * @param {!string} componentId The id/name of the component we want to load.
 * @param {ContextOptions|undefined} [options] context options used to initialize the loaded component
 * @return {ComponentContext}
 */
function load(componentId, options) {
    // TODO: throw error on argument mismatch
    // TODO: prevent load loops when including recursively a component

    /** @type {ComponentContext} */
    var ctx = null;
    if (!util.isNoU(options)) {
        // the `componentId` property is mandatory for `createContext` to work properly
        options.componentId = componentId;
        // check if context has its unique id assigned
        if (!util.isNoU(options.contextId)) {
            // if it does, try to pick it from allocated contexts list
            ctx = context(options.contextId);
            if (ctx !== null) {
                ctx.options(options);
            } else {
                // if no context is already allocated
                // with that id, then add a new one
                ctx = createContext(options);
            }
        } else {
            if (options === false)
                options = {};
            // generate contextId (this is a bit buggy, but it's quick)
            options.contextId = 'zuix-ctx-' + (++_contextSeqNum);
            ctx = createContext(options);
        }
    } else {
        // TODO: check if this case is of any use
        // empty context
        options = {};
        ctx = new ComponentContext(options, trigger);
    }

    // assign the given component (widget) to this context
    if (ctx.componentId != componentId) {
        // mutable component, rebind to a different component
        // preserving current context data
        ctx.componentId = componentId;
        /*
         TODO: to be fixed
         if (!util.isNoU(context.view())) {
         // TODO: implement this code in a context.detach() method
         //context.controller().pause()
         context.view().detach();
         context.view(null);
         }*/
    }

    if (util.isFunction(options.ready))
        ctx.ready = options.ready;
    if (util.isFunction(options.error))
        ctx.error = options.error;

    // if component is lazy-loaded, then defer associated resources loading
    if (options.lazyLoad)
        return ctx;

    if (resourceLoadTask[componentId] == null) {
        resourceLoadTask[componentId] = true;
        return loadResources(ctx, options);
    } else {
        if (_pendingResourceTask[componentId] == null)
            _pendingResourceTask[componentId] = [];
        _pendingResourceTask[componentId].push({ c: ctx, o: options});
    }

    return ctx; //loadResources(ctx, options);
}

/** @private */
function loadResources(ctx, options) {
    // pick it from cache if found
    var cachedComponent = getCachedComponent(ctx.componentId);
    if (cachedComponent !== null && options.controller == null && ctx.controller() == null) {
        ctx.controller(cachedComponent.controller);
        _log.t(ctx.componentId+':js', 'component:cached:js');
    }

    if (util.isNoU(options.view)) {

        if (cachedComponent !== null && cachedComponent.view != null) {
            ctx.view(cachedComponent.view);
            _log.t(ctx.componentId+':html', 'component:cached:html');
            /*
             TODO: CSS caching, to be tested.
             */
             if (cachedComponent.view != null && util.isNoU(options.css)) {
                 options.css = false;
                 if (!cachedComponent.css_applied) {
                     cachedComponent.css_applied = true;
                     ctx.style(cachedComponent.css);
                     _log.t(ctx.componentId+':css', 'component:cached:css');
                 }
             }
        }

        // if not able to inherit the view from the base cachedComponent
        // or from an inline element, then load the view from web
        if (util.isNoU(ctx.view())) {
            // Load View
            taskQueue('resource-loader').queue(ctx.componentId+':html', function () {
                resourceLoadTask[ctx.componentId] = this;

                ctx.loadHtml({
                    cext: options.cext,
                    caching: _enableHttpCaching,
                    success: function (html) {
                        if (cachedComponent == null)
                            cachedComponent = cacheComponent(ctx);
                        cachedComponent.view = html;
                        delete cachedComponent.controller;
                        if (options.css !== false) {
                            resourceLoadTask[ctx.componentId].step(ctx.componentId+':css');
                            ctx.loadCss({
                                caching: _enableHttpCaching,
                                success: function (css) {
                                    cachedComponent.css = css;
                                },
                                error: function (err) {
                                    _log.e(err, ctx);
                                },
                                then: function () {
                                    loadController(ctx, resourceLoadTask[ctx.componentId]);
                                }
                            });
                        } else {
                            loadController(ctx, resourceLoadTask[ctx.componentId]);
                        }
                    },
                    error: function (err) {
                        _log.e(err, ctx);
                        if (util.isFunction(options.error))
                            (ctx.error).call(ctx, err);
                    }
                });

            }, options.priority);
            // defer controller loading
            return ctx;
        }
    } else {
        ctx.view(options.view);
    }
    if (ctx.controller() == null) {
        taskQueue('resource-loader').queue(ctx.componentId + ':js', function () {
            resourceLoadTask[ctx.componentId] = this;
            loadController(ctx, resourceLoadTask[ctx.componentId]);
        }, _contextRoot.length);
    } else loadController(ctx);

    return ctx;
}
/**
 * Unload and dispose the component.
 *
 * @private
 * @param context {ComponentContext}
 */
function unload(context) {
    if (!util.isNoU(context) && !util.isNoU(context._c)) {
        if (!util.isNoU(context._c.view())) {
            context._c.view().attr('data-ui-component', null);
            // un-register event handlers associated to the view
            context._c.view().reset();
            // un-register event handlers for all cached fields accessed through cp.field(...) method
            if (!util.isNoU(context._c._fieldCache)) {
                z$.each(context._c._fieldCache, function (k, v) {
                    v.reset();
                });
            }
            // detach from parent
            context._c.view().detach();
        }
        if (util.isFunction(context._c.destroy))
            context._c.destroy();
    }
}

/** @private */
function createContext(options) {
    var context = new ComponentContext(options, trigger);
    _contextRoot.push(context);
    return context;
}

/**
 * TODO: desc
 *
 * @private
 * @param {Element|ZxQuery|object} contextId
 * @return {ComponentContext}
 */
function context(contextId) {
    var context = null;
    if (contextId instanceof z$.ZxQuery)
        contextId = contextId.get();
    z$.each(_contextRoot, function (k, v) {
        if ((contextId instanceof Element && (v.view() === contextId || v.container() === contextId))
            || util.objectEquals(v.contextId, contextId)) {
            context = v;
            return false;
        }
    });
    return context;
}

/** @private */
function removeContext(contextId) {
    // TODO: removeContext
}

/**
 * TODO: desc
 *
 * @private
 * @param path
 * @param handler
 */
function hook(path, handler) {
    if (util.isNoU(handler))
        return _hooksCallbacks[path];
    _hooksCallbacks[path] = handler;
}

/**
 * TODO: desc
 *
 * @private
 * @param context
 * @param path
 * @param data
 */
function trigger(context, path, data) {

    // TODO: call all registered callback
    if (util.isFunction(_hooksCallbacks[path]))
        _hooksCallbacks[path].call(context, data);

    // ZUIX Componentizer is the last executed hook (built-in)
//    if (path == 'view:process')
//        _componentizer.componentize(data);
}

/**
 * Enable/Disable HTTP caching
 *
 * @private
 * @param {boolean} [enable]
 * @return {boolean} *true* if HTTP caching is enabled, *false* otherwise.
 */
function httpCaching(enable) {
    if (enable != null)
        _enableHttpCaching = enable;
    return _enableHttpCaching;
}

/*********************** private members *************************/


/** @private */
function removeCachedComponent(componentId) {
    // TODO: removeCachedComponent
}

/***
 * @private
 * @param {Object} componentId
 * @return {ComponentCache}
 */
function getCachedComponent(componentId) {
    var cached = null;
    z$.each(_componentCache, function (k, v) {
        if (util.objectEquals(v.componentId, componentId)) {
            cached = v;
            return false;
        }
    });
    return cached;
}

/***
 * @private
 * @param {ComponentContext} context
 * @param {TaskQueue} [task]
 */
function loadController(context, task) {
    if (typeof context.options().controller === 'undefined' && context.controller() === null) {
        _log.d(context.componentId, 'controller:load');
        if (!util.isNoU(task))
            task.step(context.componentId+':js');
        if (util.isFunction(_globalHandlers[context.componentId])) {
            context.controller(_globalHandlers[context.componentId]);
            createComponent(context, task);
        } else {
            var job = function(t) {
                z$.ajax({
                    url: context.componentId + ".js" + (_enableHttpCaching ? '' : '?'+new Date().getTime()),
                    success: function (ctrlJs) {
                        // TODO: improve js parsing!
                        try {
                            var fn = ctrlJs.indexOf('function');
                            var il = ctrlJs.indexOf('.load');
                            if (il > 1 && il < fn)
                                ctrlJs = ctrlJs.substring(0, il - 4);
                            var ih = ctrlJs.indexOf('.controller');
                            if (ih > 1 && ih < fn)
                                ctrlJs = ctrlJs.substring(ih + 11);
                            var ec = ctrlJs.indexOf('//<--controller');
                            if (ec > 0)
                                ctrlJs = ctrlJs.substring(0, ec);
                            ctrlJs += '\n//# sourceURL="'+context.componentId + '.js"\n';
                            context.controller(getController(ctrlJs));
                        } catch (e) {
                            _log.e(new Error(), e, ctrlJs, context);
                            if (util.isFunction(context.error))
                                (context.error).call(context, e);
                        }
                    },
                    error: function (err) {
                        _log.e(err, new Error(), context);
                        if (util.isFunction(context.error))
                            (context.error).call(context, err);
                    },
                    then: function () {
                        createComponent(context, t);
                    }
                });
            };
            if (util.isNoU(task)) {
                taskQueue('resource-loader').queue(context.componentId+':js', function () {
                    job(resourceLoadTask[context.componentId] = this);
                }, context.options().priority);
            } else job(task);
        }
    } else {
        createComponent(context, task);
    }
}

function cacheComponent(context) {
    var html = context.view().innerHTML; //(context.view() === context.container() ? context.view().innerHTML : context.view().outerHTML);
    var c = z$.wrapElement('div', html);
    var cached = {
        componentId: context.componentId,
        view: c.innerHTML,
        css: context._css,
        controller: context.controller()
    };
    _componentCache.push(cached);
    _log.t(context.componentId, 'bundle:added');
    return cached;
}

/***
 * @private
 * @param {ComponentContext} context
 * @param {TaskQueue} [task]
 */
function createComponent(context, task) {
    resourceLoadTask[context.componentId] = null;
    if (!util.isNoU(context.view())) {
        var cached = getCachedComponent(context.componentId);
        if (!context.options().viewDeferred)
            if (cached === null)
                cached = cacheComponent(context);
            else if (cached.controller == null)
                cached.controller = context.controller();
        else
            _log.w(context.componentId, 'component:deferred:load');

        if (task != null)
            task.callback(function () {
                _log.d(context.componentId, 'controller:create:deferred');
                initController(context._c);
            });

        _log.d(context.componentId, 'component:initializing');
        if (util.isFunction(context.controller())) {
            // TODO: should use 'require' instead of 'new Controller' ... ?
            /** @type {ContextController} */
            var c = context._c = new ContextController(context);
            c.log = _dereq_('../helpers/Logger')(context.contextId);
            if (typeof c.init === 'function')
                c.init();
            if (!util.isNoU(c.view())) {
                c.view().attr('data-ui-component', context.componentId);
                // if no model is supplied, try auto-create from view fields
                if (util.isNoU(context.model()) && !util.isNoU(context.view()))
                    context.viewToModel();
                c.trigger('view:apply');
                if (context.options().viewDeferred) {
                    context.options().viewDeferred = false;
                    // save the original inline view
                    // before loading the view template
                    // it can be then restored with c.restoreView()
                    c.saveView();

                    // TODO: check if this case is still required, otherwise remove it.
                    if (cached === null) {
                        cached = {
                            componentId: context.componentId,
                            controller: context.controller()
                        };
                        _componentCache.push(cached);
                        _log.t(context.componentId, 'bundle:added');
                        _log.d(context.componentId, 'component:deferred:load');
                    }

                    var pending = -1;
                    if (context.options().css !== false)
                        if (cached.css == null) {
                            if (pending == -1) pending = 0; pending++;
                            context.loadCss({
                                caching: _enableHttpCaching,
                                success: function(css) {
                                    // TODO: this is a work-around for 'componentize' overlapping issue
                                    if (cached.css == null)
                                        cached.css = css;
                                    _log.d(context.componentId, 'component:deferred:css', pending);
                                },
                                then: function () {
                                    if (--pending === 0 && task != null)
                                        task.end();
                                }
                            });
                        } else context.style(cached.css);
                    if (context.options().html !== false)
                        if (cached.view == null) {
                            if (pending == -1) pending = 0; pending++;
                            context.loadHtml({
                                cext: context.options().cext,
                                caching: _enableHttpCaching,
                                success: function(html) {
                                    // TODO: this is a work-around for 'componentize' overlapping issue
                                    if (cached.view == null)
                                        cached.view = html;
                                    _log.d(context.componentId, 'component:deferred:html', pending);
                                },
                                then: function () {
                                    if (--pending === 0 && task != null)
                                        task.end();
                                }
                            });
                        } else context.view(cached.view);
                    if (pending == -1 && task != null)
                        task.end();
                } else if (task != null) task.end();
            }

            if (task == null) {
                _log.d(context.componentId, 'controller:create');
                initController(c);
            }

        } else {
            _log.w(context.componentId, 'component:controller:undefined');
        }

    } else {
        // TODO: report error
        _log.e(context.componentId, 'component:view:undefined');
    }
}

/***
 * @private
 * @param {ContextController} c
 */
function initController(c) {

    _log.t(c.context.componentId, 'controller:init', 'timer:init:start');

    // bind {ContextController}.field method
    c.field = function(fieldName) {
        var el = field(fieldName, c.view(), c);
        el.on = function (eventPath, eventHandler, eventData, isHook) {
            if (typeof eventHandler === 'string') {
                var eh = eventHandler;
                eventHandler = function () { c.trigger(eh, eventData, isHook); }
            }
            return z$.ZxQuery.prototype.on.call(this, eventPath, eventHandler);
        };
        return el;
    };

    if (util.isFunction(c.create)) c.create();
    c.trigger('view:create');

    if (util.isFunction(c.context.ready))
        (c.context.ready).call(c.context, c.context);

    c.trigger('component:ready', c.view(), true);

    _log.t(c.context.componentId, 'controller:init', 'timer:init:stop');
    _log.i(c.context.componentId, 'component:loaded', c.context.contextId);

    if (_pendingResourceTask[c.context.componentId] != null) {
        var pendingRequests = _pendingResourceTask[c.context.componentId];
        _pendingResourceTask[c.context.componentId] = null;
        var ctx = null;
        while (pendingRequests != null && (ctx = pendingRequests.shift()) != null)
            loadResources(ctx.c, ctx.o);
    }

}

/***
 * @private
 * @param javascriptCode string
 * @returns {ContextControllerHandler}
 */
// TODO: refactor this method name
function getController(javascriptCode) {
    var instance = function (ctx) {
    };
    if (typeof javascriptCode === 'string') {
        try {
            instance = (eval).call(this, javascriptCode);
        } catch (e) {
            // TODO: should trigger a global hook
            // eg. 'controller:error'
            _log.e(this, e, javascriptCode);
        }
    }
    return instance;
}

function replaceCache(c) {
    _componentCache = c;
}

/******************* proto ********************/


/**
 * Initializes a controller ```handler```.
 *
 * @example
 *
<small>**Example - JavaScript**</small>
<pre data-line="2"><code class="language-js">
// Controller of component 'path/to/component_name'
var ctrl = zuix.controller(function(cp) {
    // `cp` is the {ContextController}
    cp.create = function() { ... };
    cp.destroy = function() { ... }
}).for('path/to/component_name');
</code></pre>
 *
 * @param {ContextControllerHandler} handler The controller handler
 * function ```function(cp){ ... } ```,
 * where `cp` is the [`{ContextController}`](#ZUIX_API--ContextController)
 * object that is passed to the handler once the component
 * is created.
 * @return {ContextControllerHandler} The initialized controller handler.
 */
Zuix.prototype.controller = controller;
/**
 * Searches and returns elements with `data-ui-field`
 * attribute matching the given `fieldName`.
 *
 * @example
 *
<small>**Example - HTML**</small>
```html
<div data-ui-field="container-div">
   <!-- container HTML -->
</div>
```

<small>**Example - JavaScript**</small>
```js
var containerDiv = zuix.field('container-div');
containerDiv.html('Hello World!');
```
 *
 * @param {!string} fieldName The class to check for.
 * @param {!Element} [container] Starting DOM element for this search (**default:** *document*)
 * @return {ZxQuery} The `{ZxQuery}`-wrapped elements with matching ```data-ui-field``` attribute.
 */
Zuix.prototype.field = field;
/**
 * Searches inside the given element ```element```
 * for all ```data-ui-include``` and ```data-ui-load```
 * directives and process them.
 * This is to be called if adding dynamically content
 * with elements that declare the above attributes.
 *
 * @example
 *
<small>**Example - JavaScript**</small>
```js
zuix.componentize(document);
```
 *
 * @param {Element} [element] Container to use as starting node for the search (**default:** *document*).
 * @return {Zuix} The ```{Zuix}``` object itself.
 */
Zuix.prototype.componentize = function (element) {
    _componentizer.componentize(element);
    return this;
};
/**
 * Loads a component with the given options.
 * This is the programmatic equivalent of
 * `data-ui-include` or `data-ui-load`.
 * All available options are described in the
 * `ContextOptions` object documentation.
 *
 * @example
 *
<small>**Example - JavaScript**</small>
```js
var exampleController = zuix.controller(function(cp){
    cp.create = function() {
        cp.expose('test', testMethod);
        cp.view().html('Helllo World!');
    }
    function testMethod() {
        console.log('Test method exposing');
        cp.view().html('A simple test.');
    }
});
var componentOptions = {
    container: zuix.field('container-div');
    controller: exampleController,
    ready: function () {
        console.log('Loading complete.');
        console.log('Component context instance', this);
    },
    error: function(error) {
        console.log('Loading error!', error);
    }
};
var ctx = zuix.load('path/to/component_name', componentOptions);
ctx.test();
```
 *
 * @param {!string} componentId The identifier name of the component to be loaded.
 * @param {ContextOptions} [options] Options used to initialize the loaded component.
 * @return {ComponentContext} The component instance context.
 */
Zuix.prototype.load = load;
/**
 * Unload and dispose the component.
 *
 * @example
 *
<small>**Example - JavaScript**</small>
```js
zuix.unload(ctx);
```
 *
 * @param {ComponentContext} context The `ComponentContext` instance of the component to be unloaded.
 * @return {Zuix} The ```{Zuix}``` object itself.
 */
Zuix.prototype.unload = function (context) {
    unload(context);
    return this;
};
/**
 * Get the `ComponentContext`, given its `contextId`
 * or component's container/view element.
 * HTML attribute equivalent: `data-ui-context`.
 *
 * @example
<small>**Example - HTML**</small>
```html
<div data-ui-load="site/components/slideshow"
     data-ui-context="my-slide-show">...</div>
```
<small>**Example - JavaScript**</small>
```js
var slideShowDiv = zuix.$.find('[data-ui-context="my-slide-show"]');
var ctx = zuix.context(slideShowDiv);
// or
var ctx = zuix.context('my-slide-show');
// call component's exposed methods
ctx.setSlide(1);
```
 *
 * @param {Element|ZxQuery|object} contextId The `contextId` object
 * (usually a string) or the component's container/view element.
 * @return {ComponentContext} The matching component context.
 */
Zuix.prototype.context = context;
/**
 * Create an instance of the component `componentId`
 * loading it asynchronously and immediately returning its
 * context object with associated container element (detached).
 *
 * @param {string} componentId Identifier name of the component to create.
 * @param {ContextOptions|undefined} [options] Options.
 * @return {ComponentContext}
 */
Zuix.prototype.createComponent = function(componentId, options) {
    if (options == null) options = {};
    options.container = document.createElement('div');
    return load(componentId, options);
};
/**
 * Triggers the event specified by `eventPath`.
 *
 * @param {Object} context Context (`this`) for the event handler
 * @param {string} eventPath The path of the event to fire.
 * @param {object} [eventData] The data object of the event.
 * @return {Zuix} The ```{Zuix}``` object itself.
 */
Zuix.prototype.trigger = function (context, eventPath, eventData) {
    trigger(context, eventPath, eventData);
    return this;
};
/**
 * Set handlers for global events hooks.
 *
<small>**Example - JavaScript**</small>
```js
// The context `this` in the event handlers will be
// the {ComponentContext} object that sourced the event.
// The `data` parameter passed to the handlers, is of
// variant type, depending on the type of the occurring event.
zuix
  .hook('load:begin', function(data){
    loaderMessage.html('Loading "'+data.task+'" ...');
    loaderMessage.show();

}).hook('load:next', function(data){
    loaderMessage.html('"'+data.task+'" done, loading next..');

}).hook('load:end', function(){
    loaderMessage.hide();

}).hook('html:parse', function (data) {
    // ShowDown - MarkDown syntax compiler
    if (this.options().markdown === true && typeof showdown !== 'undefined')
        data.content = new showdown.Converter()
            .makeHtml(data.content);

}).hook('css:parse', function (data) {
    // process css, eg. run a CSS pre-processor
    // eg. Sass, Less, ...

}).hook('view:process', function (view) {
    // The view DOM is now fully loaded and ready

    // Prism code syntax highlighter
    view.find('code').each(function (i, block) {
        this.addClass('language-javascript');
        Prism.highlightElement(block);
    });

    // Force opening of all non-local links in a new window
    zuix.$('a[href*="://"]').attr('target','_blank');

    // Material Design Light auto-detection
    // Call DOM upgrade on newly added view elements
    if (componentHandler)
        componentHandler.upgradeElements(view.get());

});
```
 *
 * @param {string} eventPath The event path.
 * @param {function} eventHandler The handler function.
 * @return {Zuix} The ```{Zuix}``` object itself.
 */
Zuix.prototype.hook = function (eventPath, eventHandler) {
    hook(eventPath, eventHandler);
    return this;
};
/**
 * Enable/Disable lazy-loading, or get current setting.
 *
 * @param {boolean} [enable] Set lazy-load option.
 * @return {Zuix|boolean} *true* if lazy-loading is enabled, *false* otherwise.
 */
Zuix.prototype.lazyLoad = function (enable) {
    if (enable != null)
        _componentizer.lazyLoad(enable);
    else
        return _componentizer.lazyLoad();
    return this;
};
/**
 * Enable/Disable HTTP caching
 *
 * @param {boolean} [enable]
 * @return {Zuix|boolean} *true* if HTTP caching is enabled, *false* otherwise.
 */
Zuix.prototype.httpCaching = function(enable) {
    if (enable != null)
        httpCaching(enable);
    else
        return httpCaching();
    return this;
};

/**
 * Gets/Sets the components data bundle.
 *
 * @param {Array.<{ view, css, controller }>} bundleData A bundle object holding in memory all components data (cache).
 * @param {function} [callback]
 * @return {Zuix|Array.<{ view, css, controller }>}
 */
Zuix.prototype.bundle = function(bundleData, callback) {
    if (util.isNoU(bundleData))
        return _componentCache;
    else if (bundleData && typeof bundleData === 'boolean') {
        _log.t('bundle:start');
        var ll = _componentizer.lazyLoad();
        _componentizer.lazyLoad(false);
        _componentizer.componentize();
        if (typeof callback === 'function') {
            var waitLoop = function(w) {
                setTimeout(function () {
                    if (_componentizer.willLoadMore()) {
                        _log.t('bundle:wait');
                        w(w);
                    } else {
                        _log.t('bundle:end');
                        _componentizer.lazyLoad(ll);
                        callback();
                    }
                }, 1000);
            };
            waitLoop(waitLoop);
        }
    } else {
        // reset css flag before importing bundle
        for (var c = 0; c < bundleData.length; c++) {
            if (bundleData[c].css_applied)
                delete bundleData[c].css_applied;
        }
        _componentCache = bundleData;
    }
    return this;
};

Zuix.prototype.$ = z$;
Zuix.prototype.TaskQueue = TaskQueue;
Zuix.prototype.ZxQuery = z$.ZxQuery;

Zuix.prototype.dumpCache = function () {
    return _componentCache;
};
Zuix.prototype.dumpContexts = function () {
    return _contextRoot;
};

// TODO: add zuix.options to configure stuff like
// TODO: the css/html/js lookup base path (each individually own prop)

/**
 * @param root
 * @return {Zuix}
 */
module.exports = function (root) {
    var zuix = new Zuix();
    if (document.readyState != 'loading'){
        zuix.componentize();
    } else {
        document.addEventListener('DOMContentLoaded', function () {
            zuix.componentize();
        });
    }
    // log messages monitor (one global listener)
    _log.monitor(function (level, args) {
        if (util.isFunction(zuix.monitor))
            zuix.monitor(level, Array.prototype.slice.call(args));
    });
    return zuix;
};



},{"../helpers/Logger":2,"../helpers/TaskQueue":3,"../helpers/Util":4,"../helpers/ZxQuery":5,"./ComponentContext":7,"./Componentizer":8,"./ContextController":9}]},{},[6])
(6)
});