/**
 * zUIx - Gesture Controller
 *
 * @version 1.0.1 (2018-08-21)
 * @author Gene
 *
 * @version 1.0.0 (2018-03-11)
 * @author Gene
 *
 */

'use strict';

function GestureHelper() {
  const SCROLL_MODE_NONE = 0;
  const SCROLL_MODE_HORIZONTAL = 1;
  const SCROLL_MODE_VERTICAL = 2;
  const GESTURE_TAP_TIMEOUT = 750;

  let scrollMode = SCROLL_MODE_NONE;
  let touchPointer;
  let ignoreSession = false;
  let passiveMode = true;
  let startGap = -1;
  let currentGesture;
  let swipeDirection;
  let speedMeter;
  let mouseButtonDown = false;
  let lastTapTime = new Date().getTime();

  // Math.sign Polyfill
  Math.sign = Math.sign || function(x) {
    return ((x>0)-(x<0))||+x;
  };

  const cp = this;
  cp.init = function() {
    const options = cp.options();
    options.html = false;
    options.css = false;
    passiveMode = options.passive !== false && passiveMode;
    startGap = options.startGap || startGap;
  };

  cp.create = function() {
    // TODO: should use event "capturing" instead of "bubbling"
    const target = passiveMode ? zuix.$(window) : cp.view();
    target.on('dragstart', {
      handler: function(e) {
        if (!ignoreSession && !passiveMode) {
          e.preventDefault();
        }
      },
      passive: passiveMode
    }).on('mousedown', {
      handler: function(e) {
        const targetElement = zuix.$(e.target);
        ignoreSession = document.elementsFromPoint(e.x, e.y).indexOf(cp.view().get()) === -1;
        if (!ignoreSession && e.which === 1 && targetElement.parent('[class*="no-gesture"]').length() === 0 && e.x > startGap) {
          mouseButtonDown = true;
          ignoreSession = false;
          // targetElement.css('touch-action', 'none');
          // TODO: add 'cp.options().preventDrag'
          targetElement.get().draggable = false;
          touchStart(e, e.x, e.y);
        } else ignoreSession = true;
      },
      passive: passiveMode
    }).on('mousemove', {
      handler: function(e) {
        if (!ignoreSession && mouseButtonDown) {
          touchMove(e, e.x, e.y);
        }
      },
      passive: passiveMode
    }).on('mouseup', function(e) {
      if (e.which === 1 && !ignoreSession) {
        mouseButtonDown = false;
        touchStop(e);
      }
    }).on('touchstart', {
      handler: function(e) {
        const targetElement = zuix.$(e.target);
        ignoreSession = document.elementsFromPoint(e.touches[0].clientX, e.touches[0].clientY).indexOf(cp.view().get()) === -1;
        if (!ignoreSession && targetElement.parent('[class*="no-gesture"]').length() === 0 && e.touches[0].clientX > startGap) {
          ignoreSession = false;
          // targetElement.css('touch-action', 'none');
          targetElement.get().draggable = false;
          touchStart(e, e.touches[0].clientX, e.touches[0].clientY);
        } else ignoreSession = true;
      },
      passive: passiveMode
    }).on('touchmove', {
      handler: function(e) {
        if (!ignoreSession) {
          touchMove(e, e.touches[0].clientX, e.touches[0].clientY);
        }
      },
      passive: passiveMode
    }).on('touchend', function(e) {
      if (!ignoreSession) {
        touchStop(e);
      }
    });
  };

  function touchStart(e, x, y) {
    const timestamp = new Date().getTime();
    touchPointer = {
      // original event + cancel method
      event: e,
      cancel: function() {
        touchPointer.event.cancelBubble = true;
        if (!passiveMode) {
          touchPointer.event.preventDefault();
        }
      },
      // initial touch position
      startX: x,
      startY: y,
      startTime: timestamp,
      // relative movement
      shiftX: 0,
      shiftY: 0,
      endTime: 0,
      // relative movement at every step
      stepX: 0,
      stepY: 0,
      stepTime: timestamp,
      // actual position and speed
      velocity: 0,
      x: x,
      y: y,
      scrollIntent: function() {
        switch (scrollMode) {
          case SCROLL_MODE_HORIZONTAL:
            return 'horizontal';
          case SCROLL_MODE_VERTICAL:
            return 'vertical';
        }
        return false;
      }
    };
    speedMeter = speedObserver(touchPointer);
    cp.trigger('gesture:touch', touchPointer);
  }
  function touchMove(e, x, y) {
    if (touchPointer != null) {
      touchPointer.event = e;
      touchPointer.x = x;
      touchPointer.y = y;
      touchPointer.shiftX = (x - touchPointer.startX);
      touchPointer.shiftY = (y - touchPointer.startY);
      touchPointer.endTime = new Date().getTime();
      // detect actual gesture
      const gesture = detectGesture();
      if (gesture != null && currentGesture !== false) {
        if (swipeDirection != null && swipeDirection !== touchPointer.direction) {
          // stop gesture detection if not coherent
          currentGesture = false;
          swipeDirection = 'cancel';
        } else {
          currentGesture = gesture;
          swipeDirection = touchPointer.direction;
        }
      }
      cp.trigger('gesture:pan', touchPointer);
    }
  }

  function touchStop(e) {
    if (touchPointer != null) {
      speedMeter.update();
      touchPointer.event = e;
      if (currentGesture == null) {
        currentGesture = detectGesture();
      }
      if (currentGesture != null && currentGesture !== false) {
        cp.trigger(currentGesture, touchPointer);
      }
    }
    cp.trigger('gesture:release', touchPointer);
    scrollMode = SCROLL_MODE_NONE;
    swipeDirection = null;
    currentGesture = null;
    touchPointer = null;
  }

  function detectGesture() {
    let gesture = null;
    // update touchPointer.velocity data
    speedMeter.update();
    // update tap gesture and swipe direction
    const minStepDistance = 2; // <--- !!! this should not be greater than 2 for best performance
    const angle = Math.atan2(touchPointer.shiftY-touchPointer.stepY, touchPointer.shiftX-touchPointer.stepX) * 180 / Math.PI;
    if ((touchPointer.shiftX) === 0 && (touchPointer.shiftY) === 0 && touchPointer.startTime > lastTapTime+100 && touchPointer.stepTime < GESTURE_TAP_TIMEOUT) {
      // gesture TAP
      lastTapTime = new Date().getTime();
      gesture = 'gesture:tap';
    } else if ((scrollMode === SCROLL_MODE_NONE || scrollMode === SCROLL_MODE_HORIZONTAL) &&
            touchPointer.stepDistance > minStepDistance && ((angle >= 135 && angle <= 180) || (angle >= -180 && angle <= -135))) {
      // gesture swipe RIGHT
      touchPointer.direction = 'left';
      gesture = 'gesture:swipe';
      scrollMode = SCROLL_MODE_HORIZONTAL;
    } else if ((scrollMode === SCROLL_MODE_NONE || scrollMode === SCROLL_MODE_HORIZONTAL) &&
            touchPointer.stepDistance > minStepDistance && ((angle >= 0 && angle <= 45) || (angle >= -45 && angle < 0))) {
      // gesture swipe LEFT
      touchPointer.direction = 'right';
      gesture = 'gesture:swipe';
      scrollMode = SCROLL_MODE_HORIZONTAL;
    } else if ((scrollMode === SCROLL_MODE_NONE || scrollMode === SCROLL_MODE_VERTICAL) &&
            touchPointer.stepDistance > minStepDistance && (angle > 45 && angle < 135)) {
      // gesture swipe UP
      touchPointer.direction = 'down';
      gesture = 'gesture:swipe';
      scrollMode = SCROLL_MODE_VERTICAL;
    } else if ((scrollMode === SCROLL_MODE_NONE || scrollMode === SCROLL_MODE_VERTICAL) &&
            touchPointer.stepDistance > minStepDistance && (angle > -135 && angle < -45)) {
      // gesture swipe DOWN
      touchPointer.direction = 'up';
      gesture = 'gesture:swipe';
      scrollMode = SCROLL_MODE_VERTICAL;
    }
    // reset touch step data
    if (touchPointer.stepDistance > minStepDistance) {
      speedMeter.step();
    }
    return gesture;
  }

  function speedObserver(tp) {
    let direction;
    const startData = {
      time: 0,
      x: 0, y: 0
    };
    const stopData = {
      time: 0,
      x: 0, y: 0
    };
    const step = function() {
      tp.stepTime = tp.endTime;
      tp.stepX = tp.shiftX;
      tp.stepY = tp.shiftY;
      tp.stepSpeed = 0;
      tp.stepDistance = 0;
    };
    const reset = function() {
      // direction changed: reset
      direction = tp.direction;
      startData.time = new Date().getTime();
      startData.x = tp.x;
      startData.y = tp.y;
      tp.velocity = 0;
      tp.distance = 0;
      step();
    };
    reset();
    return {
      update: function() {
        stopData.time = new Date().getTime();
        stopData.x = tp.x;
        stopData.y = tp.y;
        if (direction != null && direction !== tp.direction) {
          reset();
          return;
        } else if (direction == null && tp.direction !== direction) {
          direction = tp.direction;
        }
        const elapsedTime = stopData.time - startData.time;
        let l = {x: (stopData.x - startData.x), y: (stopData.y - startData.y)};
        const d = Math.sqrt(l.x*l.x + l.y*l.y);
        tp.distance = d;
        // movement speed in px/ms
        const speed = (d / elapsedTime);
        // add the direction info
        tp.velocity = (tp.direction === 'left' || tp.direction === 'up') ? -speed : speed;
        // update "step" speed data
        tp.stepTime = (tp.endTime-tp.stepTime);
        l = {x: (tp.shiftX-tp.stepX), y: (tp.shiftY-tp.stepY)};
        tp.stepDistance = Math.sqrt(l.x*l.x+l.y*l.y);
        tp.stepSpeed = (tp.stepDistance / tp.stepTime);
      },
      step: step
    };
  }
}

module.exports = GestureHelper;
