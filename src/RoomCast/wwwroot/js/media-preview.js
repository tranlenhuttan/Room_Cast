(() => {
  const hasWindow = typeof window !== 'undefined';
  const ViewerGlobal = hasWindow ? window.Viewer : undefined;

  const previewRoot = document.querySelector('[data-preview-root]');
  if (!previewRoot) {
    return;
  }

  const mediaCards = Array.from(document.querySelectorAll('[data-media-card]'));
  if (mediaCards.length === 0) {
    return;
  }

  const state = {
    imageViewer: null,
    imageCardIndexMap: new Map(),
    videoModal: null,
    activeAbortController: null,
    revokeObjectUrl: null,
    lastFocusedElement: null
  };

  const handleKeydown = (event) => {
    if (event.key === 'Escape') {
      event.preventDefault();
      closeVideoModal();
    }
  };

  const isInteractiveChild = (element) => {
    if (!(element instanceof Element)) {
      return false;
    }
    return Boolean(element.closest('a, button, input, textarea, select, label'));
  };

  if (typeof ViewerGlobal === 'function') {
    setupImagePreview();
  }

  setupVideoPreview();

  function setupImagePreview() {
    const imageCards = mediaCards.filter((card) => {
      const type = (card.getAttribute('data-media-type') ?? '').trim().toLowerCase();
      return type === 'picture' || type === 'image';
    });

    if (imageCards.length === 0) {
      return;
    }

    const gallery = document.createElement('div');
    gallery.setAttribute('data-media-gallery', '');
    gallery.setAttribute('aria-hidden', 'true');
    gallery.style.display = 'none';

    imageCards.forEach((card) => {
      const source = card.getAttribute('data-media-src');
      if (!source) {
        return;
      }
      const title = card.getAttribute('data-media-title') ?? '';

      const img = document.createElement('img');
      img.src = source;
      img.alt = title || 'Preview image';
      if (title) {
        img.title = title;
      }

      const index = gallery.childElementCount;
      gallery.appendChild(img);
      state.imageCardIndexMap.set(card, index);
    });

    if (gallery.childElementCount === 0) {
      return;
    }

    previewRoot.appendChild(gallery);

    const viewer = new ViewerGlobal(gallery, {
      navbar: false,
      toolbar: {
        zoomIn: 1,
        zoomOut: 1,
        oneToOne: 1,
        reset: 1,
        rotateLeft: 1,
        rotateRight: 1,
        flipHorizontal: 1,
        flipVertical: 1,
        prev: 1,
        play: 0,
        next: 1
      },
      title(image) {
        return image && image.alt ? image.alt : '';
      },
      tooltip: false,
      movable: true,
      transition: false,
      loop: true
    });

    state.imageViewer = viewer;

    imageCards.forEach((card) => {
      card.addEventListener('dblclick', (event) => {
        if (isInteractiveChild(event.target)) {
          return;
        }

        event.preventDefault();
        const index = state.imageCardIndexMap.get(card);
        if (typeof index === 'number') {
          viewer.view(index);
        }
      });
    });
  }

  function setupVideoPreview() {
    const videoCards = mediaCards.filter((card) => {
      const type = (card.getAttribute('data-media-type') ?? '').trim().toLowerCase();
      return type === 'video';
    });

    if (videoCards.length === 0) {
      return;
    }

    videoCards.forEach((card) => {
      card.addEventListener('dblclick', (event) => {
        if (isInteractiveChild(event.target)) {
          return;
        }

        event.preventDefault();
        openVideoPreview(card);
      });
    });
  }

  function ensureVideoModal() {
    if (state.videoModal) {
      return state.videoModal;
    }

    const backdrop = document.createElement('div');
    backdrop.className = 'media-preview-backdrop';
    backdrop.style.display = 'none';

    const dialog = document.createElement('div');
    dialog.className = 'media-preview-dialog';
    dialog.setAttribute('role', 'dialog');
    dialog.setAttribute('aria-modal', 'true');
    dialog.setAttribute('aria-label', 'Video preview');
    dialog.setAttribute('tabindex', '-1');

    const toolbar = document.createElement('div');
    toolbar.className = 'media-preview-toolbar';

    const toolbarMain = document.createElement('div');
    toolbarMain.className = 'media-preview-toolbar__main';

    const titleGroup = document.createElement('div');
    titleGroup.className = 'media-preview-title';

    const titleHeading = document.createElement('h2');
    titleHeading.dataset.previewTitle = '';
    titleHeading.textContent = 'Video preview';

    const metaLine = document.createElement('p');
    metaLine.dataset.previewMeta = '';
    metaLine.textContent = '';

    titleGroup.appendChild(titleHeading);
    titleGroup.appendChild(metaLine);

    toolbarMain.appendChild(titleGroup);

    const toolbarActions = document.createElement('div');
    toolbarActions.className = 'media-preview-toolbar__actions';

    const openOriginal = document.createElement('a');
    openOriginal.className = 'inline-flex items-center gap-2 rounded-md border border-slate-400/40 bg-white/10 px-3 py-1.5 text-sm font-medium text-slate-100 transition hover:bg-white/20 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-slate-200';
    openOriginal.target = '_blank';
    openOriginal.rel = 'noopener noreferrer';
    openOriginal.dataset.previewOpen = '';
    openOriginal.textContent = 'Open original';

    const closeButton = document.createElement('button');
    closeButton.type = 'button';
    closeButton.className = 'media-preview-close';
    closeButton.setAttribute('aria-label', 'Close preview');
    closeButton.innerHTML = '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M4.22 4.22a.75.75 0 0 1 1.06 0L10 8.94l4.72-4.72a.75.75 0 1 1 1.06 1.06L11.06 10l4.72 4.72a.75.75 0 0 1-1.06 1.06L10 11.06l-4.72 4.72a.75.75 0 0 1-1.06-1.06L8.94 10 4.22 5.28a.75.75 0 0 1 0-1.06z" clip-rule="evenodd" /></svg>';

    toolbarActions.appendChild(openOriginal);
    toolbarActions.appendChild(closeButton);

    toolbar.appendChild(toolbarMain);
    toolbar.appendChild(toolbarActions);

    const body = document.createElement('div');
    body.className = 'media-preview-body';

    const surface = document.createElement('div');
    surface.className = 'media-preview-surface';

    const content = document.createElement('div');
    content.className = 'media-preview-surface__content';

    const status = document.createElement('div');
    status.className = 'media-preview-surface__message text-slate-200';
    status.dataset.previewStatus = '';

    const statusIcon = document.createElement('i');
    statusIcon.className = 'bi bi-hourglass-split';

    const statusText = document.createElement('span');
    statusText.dataset.previewStatusText = '';
    statusText.textContent = 'Preparing preview…';

    status.appendChild(statusIcon);
    status.appendChild(statusText);

    const video = document.createElement('video');
    video.className = 'media-preview-surface__video hidden';
    video.setAttribute('playsinline', '');
    video.setAttribute('preload', 'metadata');
    video.controls = true;

    content.appendChild(status);
    content.appendChild(video);
    surface.appendChild(content);
    body.appendChild(surface);

    dialog.appendChild(toolbar);
    dialog.appendChild(body);
    backdrop.appendChild(dialog);
    document.body.appendChild(backdrop);

    backdrop.addEventListener('click', (event) => {
      if (event.target === backdrop) {
        closeVideoModal();
      }
    });

    dialog.addEventListener('click', (event) => {
      event.stopPropagation();
    });

    closeButton.addEventListener('click', () => {
      closeVideoModal();
    });

    const modal = {
      backdrop,
      dialog,
      closeButton,
      openOriginal,
      status,
      statusIcon,
      statusText,
      titleHeading,
      metaLine,
      video
    };

    state.videoModal = modal;
    return modal;
  }

  function openVideoPreview(card) {
    const modal = ensureVideoModal();
    const { backdrop, dialog, closeButton, openOriginal, status, statusIcon, statusText, titleHeading, metaLine, video } = modal;

    state.lastFocusedElement = document.activeElement instanceof HTMLElement ? document.activeElement : null;

    const title = (card.getAttribute('data-media-title') ?? '').trim() || 'Video preview';
    titleHeading.textContent = title;

    const type = (card.getAttribute('data-media-type') ?? '').trim();
    const readableType = type ? type.charAt(0).toUpperCase() + type.slice(1) : 'Video';

    const durationSeconds = Number.parseFloat(card.getAttribute('data-media-duration') ?? '');
    const fileSize = Number.parseInt(card.getAttribute('data-media-size') ?? '', 10);

    const metaItems = [];
    if (readableType) {
      metaItems.push(readableType);
    }
    const durationLabel = formatDuration(durationSeconds);
    if (durationLabel) {
      metaItems.push(durationLabel);
    }
    const sizeLabel = formatBytes(fileSize);
    if (sizeLabel) {
      metaItems.push(sizeLabel);
    }
    metaLine.textContent = metaItems.join(' • ');

    const src = card.getAttribute('data-media-src');
    if (src) {
      openOriginal.href = src;
      openOriginal.classList.remove('hidden');
    } else {
      openOriginal.removeAttribute('href');
      openOriginal.classList.add('hidden');
    }

    const poster = card.getAttribute('data-media-poster');
    if (poster) {
      video.setAttribute('poster', poster);
    } else {
      video.removeAttribute('poster');
    }

    const updateStatus = (message, variant = 'info') => {
      if (!message) {
        status.classList.add('hidden');
        status.dataset.state = '';
        statusText.textContent = '';
        statusIcon.className = 'bi';
        return;
      }

      status.classList.remove('hidden');
      status.dataset.state = variant;
      statusText.textContent = message;

      status.classList.remove('text-slate-200', 'text-red-200', 'text-emerald-200');
      let icon = 'bi-hourglass-split';

      if (variant === 'error') {
        status.classList.add('text-red-200');
        icon = 'bi-exclamation-octagon';
      } else if (variant === 'success') {
        status.classList.add('text-emerald-200');
        icon = 'bi-check-circle';
      } else {
        status.classList.add('text-slate-200');
      }

      statusIcon.className = `bi ${icon}`;
    };

    updateStatus('Preparing preview…', 'info');
    video.pause();
    video.removeAttribute('src');
    video.load();
    video.classList.add('hidden');

    if (state.revokeObjectUrl) {
      state.revokeObjectUrl();
      state.revokeObjectUrl = null;
    }

    if (state.activeAbortController) {
      state.activeAbortController.abort();
      state.activeAbortController = null;
    }

    const srcContentType = (card.getAttribute('data-media-content-type') ?? '').trim().toLowerCase();

    if (!src) {
      updateStatus('Video source is not available.', 'error');
    } else {
      const controller = new AbortController();
      state.activeAbortController = controller;

      prepareVideoSource(
        {
          src,
          contentType: srcContentType,
          fileSize,
          duration: Number.isFinite(durationSeconds) ? durationSeconds : null
        },
        controller.signal,
        updateStatus
      )
        .then((result) => {
          if (controller.signal.aborted) {
            return;
          }

          updateStatus(result.fromTranscode ? 'Preview ready.' : 'Loading video…', result.fromTranscode ? 'success' : 'info');

          if (result.cleanup) {
            state.revokeObjectUrl = () => {
              result.cleanup?.();
              state.revokeObjectUrl = null;
            };
          }

          video.setAttribute('data-preview-source', result.fromTranscode ? 'transcoded' : 'original');
          video.src = result.url;
          video.classList.remove('hidden');
          video.load();

          const attemptPlay = () => {
            video.play().catch(() => {
              // Autoplay might be blocked; user can press play manually.
            });
          };

          if (video.readyState >= 2) {
            attemptPlay();
          } else {
            video.addEventListener(
              'loadeddata',
              () => {
                attemptPlay();
              },
              { once: true }
            );
          }

          if (!result.fromTranscode) {
            video.addEventListener(
              'playing',
              () => {
                updateStatus('', 'info');
              },
              { once: true }
            );
          }
        })
        .catch((error) => {
          if (controller.signal.aborted) {
            return;
          }

          console.error('Failed to prepare video preview', error);
          updateStatus('We could not generate this preview. Use the Open original link instead.', 'error');
        })
        .finally(() => {
          if (state.activeAbortController === controller) {
            state.activeAbortController = null;
          }
        });
    }

    backdrop.style.display = 'flex';
    document.body.classList.add('overflow-hidden');
    document.addEventListener('keydown', handleKeydown);

    requestAnimationFrame(() => {
      closeButton.focus();
    });
  }

  function closeVideoModal() {
    if (!state.videoModal) {
      return;
    }

    const { backdrop, video } = state.videoModal;

    if (state.activeAbortController) {
      state.activeAbortController.abort();
      state.activeAbortController = null;
    }

    if (state.revokeObjectUrl) {
      state.revokeObjectUrl();
      state.revokeObjectUrl = null;
    }

    video.pause();
    video.removeAttribute('src');
    video.load();
    video.classList.add('hidden');

    document.body.classList.remove('overflow-hidden');
    document.removeEventListener('keydown', handleKeydown);

    backdrop.style.display = 'none';

    if (state.lastFocusedElement && typeof state.lastFocusedElement.focus === 'function') {
      state.lastFocusedElement.focus();
      state.lastFocusedElement = null;
    }
  }

  const SUPPORTED_DIRECT_TYPES = ['video/mp4', 'video/webm', 'video/ogg', 'video/quicktime', 'video/x-matroska'];
  const TRANSCODE_SIZE_LIMIT = 80 * 1024 * 1024;

  function prepareVideoSource(options, signal, updateStatus) {
    const { src, contentType, fileSize, duration } = options;
    if (!src) {
      throw new Error('Video source is required.');
    }

    const normalizedType = contentType ?? '';
    const supportsDirectPlayback = SUPPORTED_DIRECT_TYPES.some((type) => normalizedType.includes(type));

    if (supportsDirectPlayback) {
      updateStatus('Loading video…');
      return Promise.resolve({
        url: src,
        type: normalizedType || 'video/mp4',
        fromTranscode: false
      });
    }

    if (!hasFFmpegSupport()) {
      updateStatus('Preview tools are unavailable. Loading original video...', 'info');
      return Promise.resolve({
        url: src,
        type: normalizedType || 'video/mp4',
        fromTranscode: false
      });
    }

    if (Number.isFinite(fileSize) && fileSize > TRANSCODE_SIZE_LIMIT) {
      updateStatus('Video is large; streaming original file instead.', 'info');
      return Promise.resolve({
        url: src,
        type: normalizedType || 'video/mp4',
        fromTranscode: false
      });
    }

    return transcodePreviewClip(
      {
        src,
        extension: guessExtension(src, normalizedType),
        duration
      },
      signal,
      updateStatus
    ).catch((error) => {
      if (error.name === 'AbortError') {
        throw error;
      }

      console.error('Transcoding preview failed; falling back to original video.', error);
      updateStatus('Falling back to the original video.', 'error');
      return {
        url: src,
        type: normalizedType || 'video/mp4',
        fromTranscode: false
      };
    });
  }

  function hasFFmpegSupport() {
    const ffmpegGlobal = globalThis.FFmpeg;
    return Boolean(ffmpegGlobal && typeof ffmpegGlobal.createFFmpeg === 'function');
  }

  const loadFFmpegInstance = (() => {
    let instancePromise;
    return async (signal) => {
      if (!instancePromise) {
        if (!hasFFmpegSupport()) {
          throw new Error('ffmpeg.wasm is not available.');
        }

        const { createFFmpeg } = globalThis.FFmpeg;
        const ffmpeg = createFFmpeg({
          log: false,
          corePath: '/lib/ffmpeg/ffmpeg-core.js'
        });

        instancePromise = ffmpeg.load().then(() => ({ ffmpeg }));
      }

      const instance = await instancePromise;
      if (signal?.aborted) {
        throw new DOMException('Aborted', 'AbortError');
      }
      return instance;
    };
  })();

  async function transcodePreviewClip(params, signal, updateStatus) {
    const { src, extension, duration } = params;

    updateStatus('Loading preview tools…');
    const { ffmpeg } = await loadFFmpegInstance(signal);
    if (signal.aborted) {
      throw new DOMException('Aborted', 'AbortError');
    }

    updateStatus('Fetching video data…');
    const fileData = await fetchBinary(src, signal);
    if (signal.aborted) {
      throw new DOMException('Aborted', 'AbortError');
    }

    const inputName = `input.${extension}`;
    const outputName = 'preview.mp4';
    const previewDuration = computeClipDuration(duration);

    ffmpeg.FS('writeFile', inputName, fileData);

    try {
      updateStatus('Generating preview clip…');
      await ffmpeg.run(
        '-i',
        inputName,
        '-t',
        previewDuration.toString(),
        '-c:v',
        'libx264',
        '-preset',
        'veryfast',
        '-c:a',
        'aac',
        '-movflags',
        'faststart',
        '-y',
        outputName
      );

      if (signal.aborted) {
        throw new DOMException('Aborted', 'AbortError');
      }

      const outputData = ffmpeg.FS('readFile', outputName);
      const blob = new Blob([outputData.buffer], { type: 'video/mp4' });
      const objectUrl = URL.createObjectURL(blob);

      updateStatus('Preview ready.', 'success');

      return {
        url: objectUrl,
        type: 'video/mp4',
        cleanup: () => URL.revokeObjectURL(objectUrl),
        fromTranscode: true
      };
    } finally {
      try {
        ffmpeg.FS('unlink', inputName);
      } catch {
        // Ignore missing file errors.
      }
      try {
        ffmpeg.FS('unlink', outputName);
      } catch {
        // Ignore missing file errors.
      }
    }
  }

  async function fetchBinary(url, signal) {
    const response = await fetch(url, { signal });
    if (!response.ok) {
      throw new Error(`Failed to fetch video (${response.status})`);
    }

    const buffer = await response.arrayBuffer();
    return new Uint8Array(buffer);
  }

  function computeClipDuration(duration) {
    if (Number.isFinite(duration) && duration > 0) {
      const clamped = Math.min(10, Math.max(3, Math.round(duration)));
      return clamped;
    }

    return 8;
  }

  function guessExtension(src, contentType) {
    if (contentType) {
      if (contentType.includes('quicktime')) {
        return 'mov';
      }
      const match = contentType.match(/video\/([a-z0-9]+)/);
      if (match && match[1]) {
        return match[1];
      }
    }

    try {
      const url = new URL(src, window.location.origin);
      const path = url.pathname;
      const extMatch = path.match(/\.([a-z0-9]+)$/i);
      if (extMatch && extMatch[1]) {
        return extMatch[1].toLowerCase();
      }
    } catch {
      // Ignore errors deriving from invalid URLs.
    }

    return 'mp4';
  }

  function formatBytes(bytes) {
    if (!Number.isFinite(bytes) || bytes <= 0) {
      return null;
    }

    const units = ['B', 'KB', 'MB', 'GB', 'TB'];
    let value = bytes;
    let unitIndex = 0;

    while (value >= 1024 && unitIndex < units.length - 1) {
      value /= 1024;
      unitIndex += 1;
    }

    const precision = value >= 100 ? 0 : 1;
    return `${value.toFixed(precision)} ${units[unitIndex]}`;
  }

  function formatDuration(seconds) {
    if (!Number.isFinite(seconds) || seconds <= 0) {
      return null;
    }

    const totalSeconds = Math.round(seconds);
    const hours = Math.floor(totalSeconds / 3600);
    const minutes = Math.floor((totalSeconds % 3600) / 60);
    const remainingSeconds = totalSeconds % 60;

    if (hours > 0) {
      return `${hours}:${String(minutes).padStart(2, '0')}:${String(remainingSeconds).padStart(2, '0')}`;
    }

    return `${minutes}:${String(remainingSeconds).padStart(2, '0')}`;
  }
})();
