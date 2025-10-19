(() => {
  const form = document.querySelector('[data-trim-form]');
  if (!form) {
    return;
  }

  const video = form.querySelector('[data-trim-video]');
  const startHidden = form.querySelector('[data-trim-start-hidden]');
  const endHidden = form.querySelector('[data-trim-end-hidden]');
  const durationHidden = form.querySelector('[data-trim-duration-hidden]');
  const startSlider = form.querySelector('[data-trim-slider-start]');
  const endSlider = form.querySelector('[data-trim-slider-end]');
  const startNumber = form.querySelector('[data-trim-start-input]');
  const endNumber = form.querySelector('[data-trim-end-input]');
  const startLabel = form.querySelector('[data-trim-start-label]');
  const endLabel = form.querySelector('[data-trim-end-label]');
  const lengthLabel = form.querySelector('[data-trim-length-label]');
  const timelineTrack = form.querySelector('[data-trim-track]');
  const selectionVisual = form.querySelector('[data-trim-selection]');
  const startPin = form.querySelector('[data-trim-pin-start]');
  const endPin = form.querySelector('[data-trim-pin-end]');
  const markStartButton = form.querySelector('[data-trim-mark-start]');
  const markEndButton = form.querySelector('[data-trim-mark-end]');
  const resetButton = form.querySelector('[data-trim-reset]');
  const previewButton = form.querySelector('[data-trim-preview]');
  const submitButton = form.querySelector('[data-trim-submit]');

  const toNumber = (value) => {
    const parsed = parseFloat(value);
    return Number.isFinite(parsed) ? parsed : 0;
  };

  const formatSeconds = (seconds) => {
    const safeSeconds = Math.max(0, seconds);
    const hours = Math.floor(safeSeconds / 3600);
    const minutes = Math.floor((safeSeconds % 3600) / 60);
    const secs = safeSeconds % 60;
    const formattedSeconds = secs.toFixed(3).padStart(6, '0');
    if (hours > 0) {
      return `${String(hours).padStart(2, '0')}:${String(minutes).padStart(2, '0')}:${formattedSeconds}`;
    }
    return `${String(minutes).padStart(2, '0')}:${formattedSeconds}`;
  };

  let duration = toNumber(durationHidden?.value ?? '0');

  const clamp = (value, min, max) => {
    if (value < min) return min;
    if (value > max) return max;
    return value;
  };

  const getStartValue = () => toNumber(startSlider?.value ?? startHidden?.value ?? '0');
  const getEndValue = () => toNumber(endSlider?.value ?? endHidden?.value ?? '0');

  const getBaselineDuration = () => {
    if (duration > 0) {
      return duration;
    }
    const fallback = Math.max(getEndValue(), getStartValue(), 0.001);
    return fallback;
  };

  const setStartSeconds = (seconds) => {
    const maxSeconds = duration > 0 ? duration : Math.max(getEndValue(), seconds, 0.001);
    const next = clamp(seconds, 0, maxSeconds);
    if (startSlider) {
      startSlider.value = next.toFixed(3);
    } else if (startHidden) {
      startHidden.value = next.toFixed(3);
    }

    if (endSlider && next > getEndValue()) {
      endSlider.value = next.toFixed(3);
    } else if (!endSlider && endHidden && next > getEndValue()) {
      endHidden.value = next.toFixed(3);
    }

    updateDisplay();
  };

  const setEndSeconds = (seconds) => {
    const maxSeconds = duration > 0 ? duration : Math.max(getEndValue(), getStartValue(), seconds, 0.001);
    const next = clamp(seconds, 0, maxSeconds);
    if (endSlider) {
      endSlider.value = next.toFixed(3);
    } else if (endHidden) {
      endHidden.value = next.toFixed(3);
    }

    if (startSlider && next < getStartValue()) {
      startSlider.value = next.toFixed(3);
    } else if (!startSlider && startHidden && next < getStartValue()) {
      startHidden.value = next.toFixed(3);
    }

    updateDisplay();
  };

  const getSecondsFromClientX = (clientX) => {
    if (!timelineTrack) {
      return 0;
    }
    const rect = timelineTrack.getBoundingClientRect();
    if (!rect || rect.width <= 0) {
      return 0;
    }
    let ratio = (clientX - rect.left) / rect.width;
    ratio = clamp(ratio, 0, 1);
    const base = getBaselineDuration();
    return ratio * base;
  };

  const adjustSelection = (type, delta) => {
    if (type === 'start') {
      setStartSeconds(getStartValue() + delta);
    } else {
      setEndSeconds(getEndValue() + delta);
    }
  };

  const syncNumbers = () => {
    if (startNumber) {
      startNumber.value = toNumber(startSlider?.value ?? startHidden?.value ?? '0').toFixed(3);
    }
    if (endNumber) {
      endNumber.value = toNumber(endSlider?.value ?? endHidden?.value ?? '0').toFixed(3);
    }
  };

  const updateDisplay = () => {
    const baseDuration = getBaselineDuration();
    const startValue = getStartValue();
    const endValue = getEndValue();
    const clippedStart = clamp(startValue, 0, baseDuration);
    const clippedEnd = clamp(endValue, clippedStart, baseDuration);
    const selection = Math.max(clippedEnd - clippedStart, 0);
    const denominator = baseDuration > 0 ? baseDuration : 1;
    const startPercent = (clippedStart / denominator) * 100;
    const endPercent = (clippedEnd / denominator) * 100;

    if (startHidden) {
      startHidden.value = clippedStart.toFixed(3);
    }
    if (endHidden) {
      endHidden.value = clippedEnd.toFixed(3);
    }
    if (startSlider) {
      startSlider.value = clippedStart.toFixed(3);
      startSlider.max = baseDuration.toFixed(3);
    }
    if (endSlider) {
      endSlider.value = clippedEnd.toFixed(3);
      endSlider.max = baseDuration.toFixed(3);
    }
    if (startLabel) {
      startLabel.textContent = formatSeconds(clippedStart);
    }
    if (endLabel) {
      endLabel.textContent = formatSeconds(clippedEnd);
    }
    if (lengthLabel) {
      lengthLabel.textContent = formatSeconds(selection);
    }
    const summary = form.querySelector('[data-trim-summary]');
    if (summary) {
      summary.textContent = formatSeconds(clippedStart);
      const endSummary = summary.nextElementSibling;
      if (endSummary) {
        endSummary.textContent = formatSeconds(clippedEnd);
      }
    }

    if (selectionVisual) {
      const clampedStartPercent = Math.min(Math.max(startPercent, 0), 100);
      const widthPercent = Math.min(Math.max(endPercent - startPercent, 0), 100);
      selectionVisual.style.left = `${clampedStartPercent}%`;
      selectionVisual.style.width = `${widthPercent}%`;
    }
    if (startPin) {
      startPin.style.left = `${Math.min(Math.max(startPercent, 0), 100)}%`;
      startPin.setAttribute('aria-valuemin', '0');
      startPin.setAttribute('aria-valuemax', clippedEnd.toFixed(3));
      startPin.setAttribute('aria-valuenow', clippedStart.toFixed(3));
      startPin.setAttribute('aria-valuetext', formatSeconds(clippedStart));
    }
    if (endPin) {
      endPin.style.left = `${Math.min(Math.max(endPercent, 0), 100)}%`;
      endPin.setAttribute('aria-valuemin', clippedStart.toFixed(3));
      endPin.setAttribute('aria-valuemax', baseDuration.toFixed(3));
      endPin.setAttribute('aria-valuenow', clippedEnd.toFixed(3));
      endPin.setAttribute('aria-valuetext', formatSeconds(clippedEnd));
    }

    syncNumbers();
  };

  const ensureDuration = (value) => {
    if (value > 0 && durationHidden) {
      durationHidden.value = value.toFixed(3);
    }
    if (value > 0) {
      duration = value;
      if (startSlider) {
        startSlider.max = value.toFixed(3);
      }
      if (endSlider) {
        endSlider.max = value.toFixed(3);
      }
    }
  };

  if (video) {
    video.addEventListener('loadedmetadata', () => {
      if (Number.isFinite(video.duration) && video.duration > 0) {
        ensureDuration(video.duration);
        if (!endSlider) {
          if (endHidden && toNumber(endHidden.value) <= toNumber(startHidden?.value ?? '0')) {
            endHidden.value = video.duration.toFixed(3);
          }
        } else if (toNumber(endSlider.value) <= toNumber(startSlider?.value ?? '0')) {
          endSlider.value = video.duration.toFixed(3);
        }
        updateDisplay();
      }
    });

    video.addEventListener('ended', () => {
      video.pause();
    });
  }

  if (startSlider) {
    startSlider.addEventListener('input', () => {
      setStartSeconds(toNumber(startSlider.value));
    });
  }

  if (endSlider) {
    endSlider.addEventListener('input', () => {
      setEndSeconds(toNumber(endSlider.value));
    });
  }

  if (startNumber) {
    startNumber.addEventListener('input', () => {
      setStartSeconds(toNumber(startNumber.value));
    });
  }

  if (endNumber) {
    endNumber.addEventListener('input', () => {
      setEndSeconds(toNumber(endNumber.value));
    });
  }

  if (markStartButton && video) {
    markStartButton.addEventListener('click', (event) => {
      event.preventDefault();
      const current = clamp(video.currentTime, 0, getBaselineDuration());
      setStartSeconds(current);
    });
  }

  if (markEndButton && video) {
    markEndButton.addEventListener('click', (event) => {
      event.preventDefault();
      const current = clamp(video.currentTime, 0, getBaselineDuration());
      setEndSeconds(current);
    });
  }

  const attachPinDrag = (pin, type) => {
    if (!pin || !timelineTrack) {
      return;
    }

    pin.addEventListener('pointerdown', (event) => {
      if (event.button !== 0) {
        return;
      }

      event.preventDefault();
      event.stopPropagation();

      const pointerId = event.pointerId;
      try {
        pin.setPointerCapture(pointerId);
      } catch {
        // Ignore if pointer capture is not supported.
      }
      pin.classList.add('is-dragging');

      const onMove = (moveEvent) => {
        if (moveEvent.pointerId !== pointerId) {
          return;
        }
        const seconds = getSecondsFromClientX(moveEvent.clientX);
        if (type === 'start') {
          setStartSeconds(seconds);
        } else {
          setEndSeconds(seconds);
        }
      };

      const cleanup = () => {
        pin.classList.remove('is-dragging');
        try {
          pin.releasePointerCapture(pointerId);
        } catch {
          // ignore
        }
        pin.removeEventListener('pointermove', onMove);
        pin.removeEventListener('pointerup', onUp);
        pin.removeEventListener('pointercancel', onCancel);
      };

      const onUp = (upEvent) => {
        if (upEvent.pointerId !== pointerId) {
          return;
        }
        cleanup();
      };

      const onCancel = (cancelEvent) => {
        if (cancelEvent.pointerId !== pointerId) {
          return;
        }
        cleanup();
      };

      pin.addEventListener('pointermove', onMove);
      pin.addEventListener('pointerup', onUp);
      pin.addEventListener('pointercancel', onCancel);
    });
  };

  const handlePinKeydown = (event, type) => {
    const smallStep = 0.1;
    const largeStep = 1;
    let handled = true;

    switch (event.key) {
      case 'ArrowLeft':
      case 'ArrowDown':
        adjustSelection(type, -(event.shiftKey ? largeStep : smallStep));
        break;
      case 'ArrowRight':
      case 'ArrowUp':
        adjustSelection(type, event.shiftKey ? largeStep : smallStep);
        break;
      case 'Home':
        if (type === 'start') {
          setStartSeconds(0);
        } else {
          setEndSeconds(getStartValue());
        }
        break;
      case 'End':
        if (type === 'start') {
          setStartSeconds(getEndValue());
        } else {
          setEndSeconds(getBaselineDuration());
        }
        break;
      default:
        handled = false;
        break;
    }

    if (handled) {
      event.preventDefault();
    }
  };

  if (timelineTrack) {
    timelineTrack.addEventListener('pointerdown', (event) => {
      if (event.button !== 0) {
        return;
      }

      if (event.target === startPin || event.target === endPin) {
        return;
      }

      event.preventDefault();
      const seconds = getSecondsFromClientX(event.clientX);
      const startValue = getStartValue();
      const endValue = getEndValue();
      if (Math.abs(seconds - startValue) <= Math.abs(seconds - endValue)) {
        setStartSeconds(seconds);
      } else {
        setEndSeconds(seconds);
      }
    });
  }

  attachPinDrag(startPin, 'start');
  attachPinDrag(endPin, 'end');

  if (startPin) {
    startPin.addEventListener('keydown', (event) => handlePinKeydown(event, 'start'));
  }
  if (endPin) {
    endPin.addEventListener('keydown', (event) => handlePinKeydown(event, 'end'));
  }

  if (resetButton) {
    resetButton.addEventListener('click', (event) => {
      event.preventDefault();
      const base = getBaselineDuration();
      setStartSeconds(0);
      setEndSeconds(base);
      if (video) {
        video.currentTime = 0;
        video.pause();
      }
    });
  }

  if (previewButton && video) {
    previewButton.addEventListener('click', (event) => {
      event.preventDefault();
      const startValue = toNumber(startHidden?.value ?? '0');
      const endValue = toNumber(endHidden?.value ?? '0');
      if (endValue <= startValue) {
        return;
      }

      const handleTimeUpdate = () => {
        if (video.currentTime >= endValue || video.currentTime < startValue) {
          video.pause();
          video.removeEventListener('timeupdate', handleTimeUpdate);
        }
      };

      video.removeEventListener('timeupdate', handleTimeUpdate);
      video.currentTime = startValue;
      video.addEventListener('timeupdate', handleTimeUpdate);
      video.play().catch(() => {
        video.removeEventListener('timeupdate', handleTimeUpdate);
      });
    });
  }

  if (submitButton) {
    form.addEventListener('submit', () => {
      submitButton.disabled = true;
      submitButton.classList.add('cursor-wait', 'opacity-75');
    });
  }

  updateDisplay();
})();
