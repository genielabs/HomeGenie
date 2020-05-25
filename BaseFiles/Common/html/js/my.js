// Put the following code after isotope js include
// Override and customize Isotope FitRows layout mode: CENTER each rows
var fitRows = Isotope.LayoutMode.modes.fitRows.prototype;
fitRows._resetLayout = function() {
  // pre-calculate offsets for centering each row
  this.x = 0;
  this.y = 0;
  this.maxY = 0;
  this._getMeasurement( 'gutter', 'outerWidth' );
  this.centerX = [];
  this.centerX.length = 0;
  this.currentRow = 0;
  this.initializing = true;
  var items = this.items.slice();
  items.sort( function(a, b) {
    return +$(a.element).attr('data-index') < +$(b.element).attr('data-index') ? -1 :
      +$(a.element).attr('data-index') > +$(b.element).attr('data-index') ? 1 : 0;
  });
  for (var i = 0; i < items.length; i++) {
      this._getItemLayoutPosition(items[i]);
  }
  if (this.centerX.length > 0)
    this.centerX[this.currentRow].offset = (this.isotope.size.innerWidth + this.gutter - this.x) / 2;
  
  this.initializing = false;
  this.currentRow = 0;

  // centered offsets were calculated, reset layout
  this.x = 0;
  this.y = 0;
  this.maxY = 0;
  this._getMeasurement( 'gutter', 'outerWidth' );
};
fitRows._getItemLayoutPosition = function( item ) {
  item.getSize();
  var itemWidth = item.size.outerWidth + this.gutter;
  // if this element cannot fit in the current row
  var containerWidth = this.isotope.size.innerWidth + this.gutter;
  if (this.x !== 0 && itemWidth + this.x > containerWidth ) {

    if (this.initializing)
        this.centerX[this.currentRow].offset = (containerWidth-this.x) / 2;
    this.currentRow++;

    this.x = 0;
    this.y = this.maxY;
  }

  if (!this.centerX[this.currentRow]) {
    this.centerX[this.currentRow] = { offset: 0 };
  }
  //if (this.centerX[this.currentRow].offset < 0)
  //  this.centerX[this.currentRow].offset = 0;

  var position = {
    x: this.x+(this.initializing?0:this.centerX[this.currentRow].offset),
    y: this.y
  };

  this.maxY = Math.max( this.maxY, this.y + item.size.outerHeight );
  this.x += itemWidth;

  return position;
};

$.fn.swapNode = function(b) {
    var a = this.get(0);
    var aparent = a.parentNode;
    var asibling = a.nextSibling === b ? a : a.nextSibling;
    b.parentNode.insertBefore(a, b);
    aparent.insertBefore(b, asibling);
};

$.fn.hitTestObject = function(selector, hitPercentageMin) {
    if (hitPercentageMin == null) hitPercentageMin = 0.1; //default minimum hit percentage
    var hitOffest = 5;
    var compares = $(selector);
    var l = this.size();
    var m = compares.size();
    for (var i = 0; i < l; i++) {
        var bounds = this.get(i).getBoundingClientRect();
        for (var j = 0; j < m; j++) {
            var compare = compares.get(j).getBoundingClientRect();
            if (!(bounds.right <= compare.left+hitOffest || bounds.left >= compare.right-hitOffest ||
                bounds.bottom <= compare.top+hitOffest || bounds.top >= compare.bottom-hitOffest)) {
                var collisionOffsetX = compare.width * hitPercentageMin;
                var collisionOffsetY = compare.height * hitPercentageMin;
                var hitsMinPercentage = !(bounds.right < compare.left+collisionOffsetX || bounds.left > compare.right-collisionOffsetX ||
                                        bounds.bottom < compare.top+collisionOffsetY || bounds.top > compare.bottom-collisionOffsetY);
                return hitsMinPercentage ? 2 : 1;
            }
        }
    }
    return false;
};

jQuery.cachedScript = function( url, options ) {
 
  // Allow user to set any option except for dataType, cache, and url
  options = $.extend( options || {}, {
    dataType: "script",
    cache: true,
    url: url
  });
 
  // Use $.ajax() since it is more flexible than $.getScript
  // Return the jqXHR object so we can chain callbacks
  return jQuery.ajax( options );
};

// Compatibility Polyfills and Shims

/*! https://mths.be/startswith v0.2.0 by @mathias */
if (!String.prototype.startsWith) {
    (function() {
        'use strict'; // needed to support `apply`/`call` with `undefined`/`null`
        var defineProperty = (function() {
            // IE 8 only supports `Object.defineProperty` on DOM elements
            try {
                var object = {};
                var $defineProperty = Object.defineProperty;
                var result = $defineProperty(object, object, object) && $defineProperty;
            } catch(error) {}
            return result;
        }());
        var toString = {}.toString;
        var startsWith = function(search) {
            if (this == null) {
                throw TypeError();
            }
            var string = String(this);
            if (search && toString.call(search) == '[object RegExp]') {
                throw TypeError();
            }
            var stringLength = string.length;
            var searchString = String(search);
            var searchLength = searchString.length;
            var position = arguments.length > 1 ? arguments[1] : undefined;
            // `ToInteger`
            var pos = position ? Number(position) : 0;
            if (pos != pos) { // better `isNaN`
                pos = 0;
            }
            var start = Math.min(Math.max(pos, 0), stringLength);
            // Avoid the `indexOf` call if no match is possible
            if (searchLength + start > stringLength) {
                return false;
            }
            var index = -1;
            while (++index < searchLength) {
                if (string.charCodeAt(start + index) != searchString.charCodeAt(index)) {
                    return false;
                }
            }
            return true;
        };
        if (defineProperty) {
            defineProperty(String.prototype, 'startsWith', {
                'value': startsWith,
                'configurable': true,
                'writable': true
            });
        } else {
            String.prototype.startsWith = startsWith;
        }
    }());
}
// Javascript utility extensions (this should be already supplied by js ECMA6)
if (typeof String.prototype.endsWith !== 'function') {
    String.prototype.endsWith = function(suffix) {
        return this.indexOf(suffix, this.length - suffix.length) !== -1;
    };
}
// https://developer.mozilla.org/it/docs/Web/JavaScript/Reference/Global_Objects/String/includes#Polyfill
if (!String.prototype.includes) {
  String.prototype.includes = function(search, start) {
    'use strict';
    if (typeof start !== 'number') {
      start = 0;
    }
    
    if (start + search.length > this.length) {
      return false;
    } else {
      return this.indexOf(search, start) !== -1;
    }
  };
}
