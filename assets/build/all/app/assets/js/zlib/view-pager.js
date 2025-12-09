/**
 * zUIx - ViewPager Component
 *
 * @version 1.0.6 (2018-08-24)
 * @author Gene
 *
 * @version 1.0.5 (2018-08-21)
 * @author Gene
 *
 * @version 1.0.4 (2018-06-29)
 * @author Gene
 *
 * @version 1.0.3 (2018-06-26)
 * @author Gene
 *
 * @version 1.0.1 (2018-02-12)
 * @author Gene
 *
 */

'use strict';

function ViewPager() {
  const DEFAULT_PAGE_TRANSITION = {duration: 0.3, easing: 'ease'};
  const BOUNDARY_HIT_EASING = 'cubic-bezier(0.0,0.1,0.35,1.1)';
  const DECELERATE_SCROLL_EASING = 'cubic-bezier(0.0,0.1,0.35,1.0)';
  const LAYOUT_HORIZONTAL = 'horizontal';
  const LAYOUT_VERTICAL = 'vertical';
  const SLIDE_DIRECTION_FORWARD = 1;
  const SLIDE_DIRECTION_BACKWARD = -1;
  // state vars
  let currentPage = -1;
  let oldPage = 0;
  let slideTimeout = null;
  let slideIntervalMs = 3000;
  let slideDirection = SLIDE_DIRECTION_FORWARD;
  let updateLayoutTimeout = null;
  let inputCaptured = false;
  // options
  let layoutType = LAYOUT_HORIZONTAL;
  let enableAutoSlide = false;
  let enablePaging = false;
  let holdTouch = false;
  let passiveMode = true;
  let startGap = 0;
  let hideOffViewElements = false;
  // status
  let isDragging = false;
  let wasVisible = false;
  let isLazyContainer = false;
  let isFlying = false;
  let actualViewSize = {
    width: 0,
    height: 0
  };
    // timers
  let componentizeInterval = null;
  let componentizeTimeout = null;
  /** @typedef {ZxQuery} */
  let pageList = null;
  // Create a mutation observer instance to watch for child add/remove
  const domObserver = new MutationObserver(function(a, b) {
    // update child list and re-layout
    pageList = cp.view().children();
    updateLayout();
  });

  const resizeHandler = () => {
    layoutElements(true);
  };
  let gestureHelper = null;

  const cp = this;

  cp.init = function() {
    const options = cp.options();
    options.html = false;
    options.css = false;
    enablePaging = (options.paging === true || enablePaging);
    enableAutoSlide = (options.autoSlide === true || enableAutoSlide);
    passiveMode = (options.passive !== false && passiveMode);
    holdTouch = (options.holdTouch === true || holdTouch);
    startGap = (options.startGap || startGap);
    if (options.verticalLayout === true) {
      layoutType = LAYOUT_VERTICAL;
    }
    if (options.slideInterval != null) {
      slideIntervalMs = (options.slideInterval || slideIntervalMs);
    }
    hideOffViewElements = (options.autoHide === true || hideOffViewElements);
  };

  cp.create = function() {
    // enable absolute positioning for items in this view
    const view = cp.view().css({
      'position': 'relative',
      'overflow': 'hidden'
    });
    // get child items (pages)
    pageList = view.children();
    // loading of images could change elements size, so layout update might be required
    view.find('img').each(function(i, el) {
      this.one('load', updateLayout);
    });
    // re-arrange view on layout changes
    zuix.$(window)
        .on('resize', resizeHandler.bind(this))
        .on('orientationchange', resizeHandler.bind(this));
    // Options for the observer (which mutations to observe)
    // Register DOM mutation observer callback
    domObserver.observe(view.get(), {
      attributes: false,
      childList: true,
      subtree: true,
      characterData: false
    });
    updateLayout();
    let tapTimeout = null;
    // gestures handling - load gesture-helper controller
    zuix.load('assets/js/zlib/gesture-helper', {
      view,
      passive: passiveMode,
      startGap: startGap,
      on: {
        'gesture:touch': function(e, tp) {
          if (!insideViewPager(tp)) return;
          inputCaptured = false;
          stopAutoSlide();
          dragStart();
          if (holdTouch) tp.cancel();
        },
        'gesture:release': function(e, tp) {
          dragStop(tp);
          resetAutoSlide();
        },
        'gesture:tap': function(e, tp) {
          if (!insideViewPager(tp)) return;
          if (tapTimeout != null) {
            clearTimeout(tapTimeout);
          }
          tapTimeout = setTimeout(function() {
            handleTap(e, tp);
          }, 50);
        },
        'gesture:pan': handlePan,
        'gesture:swipe': handleSwipe
      },
      ready: function(ctx) {
        gestureHelper = ctx;
        layoutElements(true);
      }
    });
    // public component methods
    cp.expose('page', function(number, transition) {
      if (number == null) {
        return parseInt(currentPage);
      } else setPage(number, transition !== undefined ? transition : DEFAULT_PAGE_TRANSITION);
    }).expose('get', function(number) {
      return number < pageList.length() && pageList.length() > 0 ? pageList.eq(number) : null;
    }).expose('slide', function(slideMs) {
      if (slideMs !== false) {
        enableAutoSlide = true;
        resetAutoSlide(slideMs !== true ? slideMs : slideIntervalMs);
      } else stopAutoSlide();
    }).expose('layout', function(mode) {
      if (mode == null) {
        return layoutType;
      } else if (mode === LAYOUT_VERTICAL) {
        layoutType = LAYOUT_VERTICAL;
      } else layoutType = LAYOUT_HORIZONTAL;
      updateLayout();
    }).expose('refresh', function() {
      layoutElements(true);
    }).expose('next', next)
        .expose('prev', prev)
        .expose('last', last)
        .expose('first', first);
  };

  cp.dispose = function() {
    if (domObserver != null) {
      domObserver.disconnect();
    }
    if (gestureHelper) {
      zuix.unload(gestureHelper);
    }
    zuix.$(window)
        .off('resize', resizeHandler.bind(this))
        .off('orientationchange', resizeHandler.bind(this));
  };

  function updateLayout() {
    if (updateLayoutTimeout != null) {
      clearTimeout(updateLayoutTimeout);
    }
    updateLayoutTimeout = setTimeout(layoutElements, 250);
  }
  function layoutElements(force) {
    if (!force && (isDragging || componentizeInterval != null)) {
      updateLayout();
      return;
    }
    // init elements
    pageList.each(function(i, el) {
      this.css({
        'position': 'absolute',
        'left': 0,
        'top': 0
      });
    });
    // measure
    const viewSize = getSize(cp.view().get());
    if (viewSize.width === 0 || viewSize.height === 0) {
      if (viewSize.height === 0 && cp.view().position().visible) {
        let maxHeight = 0;
        // guess and set view-pager height
        pageList.each(function(i, el) {
          const size = getSize(el);
          if (size.height > maxHeight) {
            maxHeight = size.height;
          }
        });
        if (viewSize.height < maxHeight) {
          cp.view().css('height', maxHeight + 'px');
        }
      }
      // cannot measure view, try again later
      updateLayout();
      return;
    }
    actualViewSize = viewSize;
    // position elements
    let offset = 0;
    let isLazy = false;
    pageList.each(function(i, el) {
      const size = getSize(el);
      if (layoutType === LAYOUT_HORIZONTAL) {
        let centerY = (viewSize.height-size.height)/2;
        if (centerY < 0) centerY = 0; // TODO: centering with negative offset was not working
        transition(this, DEFAULT_PAGE_TRANSITION);
        position(this, offset, centerY);
        offset += size.width;
      } else {
        let centerX = (viewSize.width-size.width)/2;
        if (centerX < 0) centerX = 0; // TODO: centering with negative offset was not working
        transition(this, DEFAULT_PAGE_TRANSITION);
        position(this, centerX, offset);
        offset += size.height;
      }
      if (this.attr('z-lazy') === 'true' ||
        this.find('[z-lazy="true"]').length() > 0) {
        isLazy = true;
      }
    });
    isLazyContainer = isLazy;

    // focus to current page
    if (currentPage >= 0) {
      setPage(parseInt(currentPage));
    }
    // start automatic slide
    if (pageList.length() > 1) {
      resetAutoSlide();
    }
  }

  function next() {
    let isLast = false;
    let page = parseInt(currentPage)+1;
    if (page >= pageList.length()) {
      page = pageList.length()-1;
      isLast = true;
    }
    setPage(page, DEFAULT_PAGE_TRANSITION);
    return !isLast;
  }
  function prev() {
    let isFirst = false;
    let page = parseInt(currentPage)-1;
    if (page < 0) {
      page = 0;
      isFirst = true;
    }
    setPage(page, DEFAULT_PAGE_TRANSITION);
    return !isFirst;
  }
  function first() {
    setPage(0, DEFAULT_PAGE_TRANSITION);
  }
  function last() {
    setPage(pageList.length()-1, DEFAULT_PAGE_TRANSITION);
  }

  function slideNext() {
    if (cp.view().position().visible) {
      setPage(parseInt(currentPage) + slideDirection, DEFAULT_PAGE_TRANSITION);
    }
    resetAutoSlide();
  }

  function resetAutoSlide(slideInterval) {
    if (slideInterval) {
      slideIntervalMs = slideInterval;
    }
    stopAutoSlide();
    if (enableAutoSlide === true) {
      const visible = cp.view().position().visible;
      if (visible) {
        if (!wasVisible) {
          zuix.componentize(cp.view());
        }
        const delay = pageList.eq(currentPage).attr('slide-interval') || slideIntervalMs;
        slideTimeout = setTimeout(slideNext, delay);
      } else {
        slideTimeout = setTimeout(resetAutoSlide, 500);
      }
      wasVisible = visible;
    }
  }
  function stopAutoSlide() {
    if (slideTimeout != null) {
      clearTimeout(slideTimeout);
    }
  }

  function getItemIndexAt(x, y) {
    let focusedPage = 0;
    pageList.each(function(i, el) {
      const data = getData(this);
      focusedPage = i;
      const size = getSize(el);
      const rect = {
        x: data.position.x,
        y: data.position.y,
        w: size.width,
        h: size.height
      };
      if ((layoutType === LAYOUT_HORIZONTAL && (rect.x > x || rect.x+rect.w > x)) ||
                (layoutType === LAYOUT_VERTICAL && (rect.y > y || rect.y+rect.h > y))) {
        return false;
      }
    });
    return focusedPage;
  }

  function focusPageAt(tp, transition) {
    const vp = cp.view().position();
    const page = getItemIndexAt(tp.x-vp.x, tp.y-vp.y);
    setPage(page, transition != null ? transition : DEFAULT_PAGE_TRANSITION);
  }

  function setPage(n, transition) {
    oldPage = currentPage;
    if (n < 0) {
      slideDirection = SLIDE_DIRECTION_FORWARD;
      n = 0;
    } else if (n >= pageList.length()) {
      slideDirection = SLIDE_DIRECTION_BACKWARD;
      n = pageList.length() - 1;
    } else if (n !== currentPage) {
      slideDirection = (currentPage < n) ? SLIDE_DIRECTION_FORWARD : SLIDE_DIRECTION_BACKWARD;
    }
    currentPage = n;
    if (currentPage != oldPage) {
      pageList.eq(currentPage).css('z-index', 1);
      if (oldPage !== -1) {
        pageList.eq(oldPage).css('z-index', 0);
      }
      cp.trigger('page:change', {in: currentPage, out: oldPage});
    }
    const el = pageList.eq(n);
    const data = getData(el);
    const elSize = getSize(el.get());
    const focusPoint = {
      x: (actualViewSize.width - elSize.width) / 2 - data.position.x,
      y: (actualViewSize.height - elSize.height) / 2 - data.position.y
    };
    flyTo(focusPoint, transition);
    resetAutoSlide();
  }

  function flyTo(targetPoint, transition) {
    const spec = getFrameSpec();
    const firstData = getData(pageList.eq(0));
    const lastPage = pageList.eq(pageList.length() - 1);
    const lastData = getData(lastPage);

    pageList.each(function(i, el) {
      const data = getData(this);
      const frameSpec = getFrameSpec();
      data.dragStart = {
        x: frameSpec.marginLeft + data.position.x,
        y: frameSpec.marginTop + data.position.y
      };
    });

    if (layoutType === LAYOUT_HORIZONTAL) {
      let x = targetPoint.x;
      if (firstData.position.x + targetPoint.x > 0) {
        x = -firstData.position.x;
      } else {
        if (lastData.position.x + lastPage.get().offsetWidth + targetPoint.x < actualViewSize.width) {
          x = -spec.marginLeft*2 + actualViewSize.width - (lastData.position.x + lastPage.get().offsetWidth);
        }
      }
      // check if boundary was adjusted and adjust flying duration accordingly
      if (targetPoint.x-x !== 0 && transition != null) {
        transition = {
          duration: transition.duration * (x / targetPoint.x),
          easing: BOUNDARY_HIT_EASING
        };
        if (!isFinite(transition.duration) || transition.duration < 0) {
          transition.duration = 0.2;
        }
      }
      dragShift(x, 0, transition);
    } else {
      let y = targetPoint.y;
      if (firstData.position.y + targetPoint.y > 0) {
        y = -firstData.position.y;
      } else {
        if (lastData.position.y + lastPage.get().offsetHeight + targetPoint.y < actualViewSize.height) {
          y = -spec.marginTop*2 + actualViewSize.height - (lastData.position.y + lastPage.get().offsetHeight);
        }
      }
      // check if boundary was adjusted and adjust flying duration accordingly
      if (targetPoint.y-y !== 0 && transition != null) {
        transition = {
          duration: transition.duration * (y / targetPoint.y),
          easing: BOUNDARY_HIT_EASING
        };
        if (!isFinite(transition.duration) || transition.duration < 0) {
          transition.duration = 0.2;
        }
      }
      dragShift(0, y, transition);
    }
    isFlying = true;
  }

  function getSize(el) {
    const rect = el.getBoundingClientRect();
    const width = rect.width || el.offsetWidth;
    const height = el.offsetHeight || rect.height;
    return {width: width, height: height};
  }

  function getData(el) {
    const data = el.get().data = el.get().data || {};
    data.position = data.position || {x: 0, y: 0};
    return data;
  }

  function componentizeStart() {
    if (isLazyContainer) {
      componentizeStop();
      if (componentizeTimeout != null) {
        clearTimeout(componentizeTimeout);
      }
      if (componentizeInterval != null) {
        clearInterval(componentizeInterval);
      }
      componentizeInterval = setInterval(function() {
        if (hideOffViewElements) {
          pageList.each(function(i, el) {
            // hide elements if not inside the view-pager
            const computed = window.getComputedStyle(el, null);
            const size = {
              width: parseFloat(computed.width.replace('px', '')),
              height: parseFloat(computed.height.replace('px', ''))
            };
            const x = parseFloat(computed.left.replace('px', ''));
            const y = parseFloat(computed.top.replace('px', ''));
            if (size.width > 0 && size.height > 0) {
              el = zuix.$(el);
              // check if element is inside the view-pager
              const visibleArea = {
                left: -actualViewSize.width / 2,
                right: actualViewSize.width * 1.5,
                top: (-actualViewSize.height / 2),
                bottom: actualViewSize.height * 1.5
              };
              if (x + size.width < visibleArea.left || y + size.height < visibleArea.top || x > visibleArea.right || y > visibleArea.bottom) {
                if (el.visibility() !== 'hidden') {
                  el.visibility('hidden');
                }
              } else if (el.visibility() !== 'visible') {
                el.visibility('visible');
              }
            }
          });
        }
        zuix.componentize(cp.view());
      }, 10);
    }
  }

  function componentizeStop() {
    if (isLazyContainer && componentizeTimeout == null) {
      clearInterval(componentizeInterval);
    }
  }

  function dragStart() {
    isDragging = true;
    isFlying = false;
    pageList.each(function(i, el) {
      const data = getData(this);
      const frameSpec = getFrameSpec();
      const computed = window.getComputedStyle(el, null);
      data.position.x = parseFloat(computed.left.replace('px', ''));
      data.position.y = parseFloat(computed.top.replace('px', ''));
      this.css('left', data.position.x+'px');
      this.css('top', data.position.y+'px');
      data.dragStart = {x: frameSpec.marginLeft+data.position.x, y: frameSpec.marginTop+data.position.y};
    });
  }

  function getFrameSpec() {
    const spec = {
      w: 0,
      h: 0,
      marginLeft: 0,
      marginTop: 0
    };
    pageList.each(function(i, el) {
      const size = getSize(el);
      spec.w += size.width;
      spec.h += size.height;
    });
    if (layoutType === LAYOUT_HORIZONTAL && spec.w < actualViewSize.width) {
      spec.marginLeft = (actualViewSize.width - spec.w) / 2;
    } else if (layoutType === LAYOUT_VERTICAL && spec.h < actualViewSize.height) {
      spec.marginTop = (actualViewSize.height - spec.h) / 2;
    }
    return spec;
  }

  function dragShift(x, y, tr) {
    if (tr != null) {
      componentizeStart();
      componentizeTimeout = setTimeout(function() {
        componentizeTimeout = null;
        componentizeStop();
      }, tr.duration*1000);
      tr = tr.duration+'s '+tr.easing;
    } else if (isLazyContainer) {
      zuix.componentize(cp.view());
    }
    pageList.each(function(i, el) {
      const data = getData(this);
      transition(this, tr);
      position(this, data.dragStart.x+x, data.dragStart.y+y);
    });
  }

  function dragStop(tp) {
    if (tp != null) {
      tp.done = true;
      // decelerate
      if (!isFlying && ((layoutType === LAYOUT_HORIZONTAL && tp.scrollIntent() === 'horizontal') || (layoutType === LAYOUT_VERTICAL && tp.scrollIntent() === 'vertical'))) {
        decelerate(null, tp);
      }
    }
    componentizeStop();
    isDragging = false;
  }

  // Gesture handling

  function handlePan(e, tp) {
    if (!isDragging || !tp.scrollIntent() || tp.done) {
      return;
    }
    if (inputCaptured ||
            ((tp.direction === 'left' || tp.direction === 'right') && layoutType === LAYOUT_HORIZONTAL) ||
            ((tp.direction === 'up' || tp.direction === 'down') && layoutType === LAYOUT_VERTICAL)) {
      // capture click event
      if (!inputCaptured && tp.event.touches == null) {
        cp.view().get().addEventListener('click', function(e) {
          if (inputCaptured) {
            inputCaptured = false;
            e.cancelBubble = true;
            // TODO: 'preventDefault' should not be used with passive listeners
            e.preventDefault();
          }
          // release mouse click capture
          cp.view().get().removeEventListener('click', this, true);
        }, true);
      }
      inputCaptured = true;
      tp.cancel();
    }
    const spec = getFrameSpec();
    if (layoutType === LAYOUT_HORIZONTAL && tp.scrollIntent() === 'horizontal') {
      dragShift(tp.shiftX-spec.marginLeft, 0);
    } else if (layoutType === LAYOUT_VERTICAL && tp.scrollIntent() === 'vertical') {
      dragShift(0, tp.shiftY-spec.marginTop);
    }
  }

  function handleTap(e, tp) {
    const vp = cp.view().position();
    const page = getItemIndexAt(tp.x-vp.x, tp.y-vp.y);
    cp.trigger('page:tap', page, tp);
    if (enablePaging) focusPageAt(tp);
  }

  function decelerate(e, tp) {
    const minSpeed = 0.01;
    const minStepSpeed = 1.25;
    const accelerationFactor = Math.abs(tp.velocity * (1500 / cp.view().get().offsetWidth));
    let friction = 0.990 + (accelerationFactor / 1000);
    if (friction > 0.999) {
      friction = 0.999;
    }
    const duration = Math.log(minSpeed / Math.abs(tp.velocity)) / Math.log(friction);
    const decelerateEasing = {
      duration: duration / 1000, // ms to s
      easing: DECELERATE_SCROLL_EASING
    };
    const fly = function(destination, shift) {
      if (enablePaging) {
        decelerateEasing.duration *= 0.5;
        if (layoutType === LAYOUT_HORIZONTAL) {
          focusPageAt({
            x: destination.x - shift.x,
            y: destination.y
          }, decelerateEasing);
        } else {
          focusPageAt({
            x: destination.x,
            y: destination.y - shift.y
          }, decelerateEasing);
        }
      } else {
        flyTo(shift, decelerateEasing);
      }
    };
    const flyingDistance = tp.stepSpeed < minStepSpeed ? 0 : accelerationFactor * tp.velocity * (1 - Math.pow(friction, duration + 1)) / (1 - friction);
    const ap = {
      x: flyingDistance,
      y: flyingDistance
    };
    if (willFly(tp) || e == null) fly(tp, ap);
  }

  function willFly(tp) {
    return (!enablePaging || Math.abs(tp.velocity) > 1.25);
  }

  function handleSwipe(e, tp) {
    if (!insideViewPager(tp)) return;
    if (willFly(tp)) {
      return;
    }
    switch (tp.direction) {
      case 'right':
        if (layoutType === LAYOUT_HORIZONTAL) prev();
        break;
      case 'left':
        if (layoutType === LAYOUT_HORIZONTAL) next();
        break;
      case 'down':
        if (layoutType === LAYOUT_VERTICAL) prev();
        break;
      case 'up':
        if (layoutType === LAYOUT_VERTICAL) next();
        break;
    }
  }

  function position(el, x, y) {
    const data = getData(el);
    if (!isNaN(x) && !isNaN(y)) {
      data.position = {'x': x, 'y': y};
      el.css({'left': data.position.x+'px', 'top': data.position.y+'px'});
    }
    return data;
  }

  function transition(el, transition) {
    if (transition == null) {
      transition = 'none';
    }
    el.css({
      'transition': transition
    });
  }

  function insideViewPager(tp) {
    let elements = document.elementsFromPoint(tp.x, tp.y);
    if (elements.length > 0 && !cp.view().get().contains(elements[0])) {
      return false;
    }
    elements = elements.filter((el) => el.attributes['z-load'] &&
            el.attributes['z-load'].value === cp.view().attr('z-load'));
    return elements.length > 0 && elements[0] === cp.view().get();
  }
}

module.exports = ViewPager;
