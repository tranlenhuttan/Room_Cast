(() => {
  const ViewerGlobal = typeof window !== 'undefined' ? window.Viewer : undefined;
  if (typeof ViewerGlobal !== 'function') {
    return;
  }

  const previewRoot = document.querySelector('[data-preview-root]');
  if (!previewRoot) {
    return;
  }

  const mediaCards = Array.from(document.querySelectorAll('[data-media-card]'));
  if (mediaCards.length === 0) {
    return;
  }

  const gallery = document.createElement('div');
  gallery.setAttribute('data-media-gallery', '');
  gallery.setAttribute('aria-hidden', 'true');
  gallery.style.display = 'none';

  const cardIndexMap = new Map();

  mediaCards.forEach((card) => {
    const type = (card.getAttribute('data-media-type') ?? '').trim().toLowerCase();
    if (type !== 'picture' && type !== 'image') {
      return;
    }

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
    cardIndexMap.set(card, index);
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

  const isInteractiveChild = (element) => {
    if (!(element instanceof Element)) {
      return false;
    }
    return Boolean(element.closest('a, button, input, textarea, select, label'));
  };

  cardIndexMap.forEach((index, card) => {
    card.addEventListener('dblclick', (event) => {
      if (isInteractiveChild(event.target)) {
        return;
      }

      event.preventDefault();
      viewer.view(index);
    });
  });
})();
