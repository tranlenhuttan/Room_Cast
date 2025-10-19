(() => {
  const cards = Array.from(document.querySelectorAll('[data-media-card]'));
  if (cards.length === 0) {
    return;
  }

  const isInteractiveChild = (element) => {
    if (!(element instanceof Element)) {
      return false;
    }

    if (element.closest('[data-card-menu]')) {
      return true;
    }

    return Boolean(element.closest('a, button, input, textarea, select, label'));
  };

  const toggleSelection = (card, explicitValue) => {
    const indicator = card.querySelector('[data-selection-indicator]');
    const current = card.classList.contains('is-selected');
    const next = typeof explicitValue === 'boolean' ? explicitValue : !current;

    card.classList.toggle('is-selected', next);
    card.setAttribute('aria-selected', next ? 'true' : 'false');

    if (indicator) {
      indicator.classList.toggle('hidden', !next);
    }
  };

  cards.forEach((card) => {
    card.addEventListener('click', (event) => {
      if (event.defaultPrevented) {
        return;
      }

      if (isInteractiveChild(event.target)) {
        return;
      }

      toggleSelection(card);
    });

    card.addEventListener('keydown', (event) => {
      if (event.target !== card) {
        return;
      }

      if (event.key === ' ' || event.key === 'Enter') {
        event.preventDefault();
        toggleSelection(card);
      }
    });
  });

  const menuRoots = Array.from(document.querySelectorAll('[data-card-menu]'));
  if (menuRoots.length === 0) {
    return;
  }

  let openMenu = null;
  let openMenuCard = null;

  const focusableSelector =
    'a[href], button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [role="menuitem"]';

  const openMenuRoot = (menu) => {
    const trigger = menu.querySelector('[data-card-menu-trigger]');
    const panel = menu.querySelector('[data-card-menu-panel]');
    if (!trigger || !panel) {
      return;
    }

    if (openMenu && openMenu !== menu) {
      closeMenuRoot(openMenu);
    }

    panel.classList.remove('hidden');
    panel.setAttribute('aria-hidden', 'false');
    trigger.setAttribute('aria-expanded', 'true');
    openMenu = menu;

    const card = menu.closest('[data-media-card]');
    if (openMenuCard && openMenuCard !== card) {
      openMenuCard.classList.remove('has-menu-open');
    }
    if (card) {
      card.classList.add('has-menu-open');
      openMenuCard = card;
    } else {
      openMenuCard = null;
    }

    const focusTarget = panel.querySelector(focusableSelector);
    if (focusTarget instanceof HTMLElement) {
      requestAnimationFrame(() => {
        focusTarget.focus();
      });
    }
  };

  const closeMenuRoot = (menu, returnFocus = false) => {
    if (!menu) {
      return;
    }

    const trigger = menu.querySelector('[data-card-menu-trigger]');
    const panel = menu.querySelector('[data-card-menu-panel]');
    if (!trigger || !panel) {
      return;
    }

    panel.classList.add('hidden');
    panel.setAttribute('aria-hidden', 'true');
    trigger.setAttribute('aria-expanded', 'false');

    if (openMenu === menu) {
      openMenu = null;
    }

    const card = menu.closest('[data-media-card]');
    if (card && card === openMenuCard) {
      card.classList.remove('has-menu-open');
      openMenuCard = null;
    } else if (!card && openMenuCard) {
      openMenuCard.classList.remove('has-menu-open');
      openMenuCard = null;
    }

    if (returnFocus && trigger instanceof HTMLElement) {
      trigger.focus();
    }
  };

  menuRoots.forEach((menu) => {
    const trigger = menu.querySelector('[data-card-menu-trigger]');
    const panel = menu.querySelector('[data-card-menu-panel]');
    if (!trigger || !panel) {
      return;
    }

    trigger.addEventListener('click', (event) => {
      event.preventDefault();
      if (openMenu === menu) {
        closeMenuRoot(menu, false);
      } else {
        openMenuRoot(menu);
      }
    });

    trigger.addEventListener('keydown', (event) => {
      if (event.key === 'Escape') {
        closeMenuRoot(menu, true);
      }
    });

    panel.addEventListener('click', (event) => {
      if (!(event.target instanceof Element)) {
        return;
      }

      const actionable = event.target.closest('[data-card-menu-close]');
      if (actionable) {
        closeMenuRoot(menu, false);
      }
    });

    panel.addEventListener('keydown', (event) => {
      if (event.key === 'Escape') {
        event.stopPropagation();
        closeMenuRoot(menu, true);
      }
    });
  });

  document.addEventListener('click', (event) => {
    if (!openMenu) {
      return;
    }

    if (event.target instanceof Element && openMenu.contains(event.target)) {
      return;
    }

    closeMenuRoot(openMenu, false);
  });

  document.addEventListener('keydown', (event) => {
    if (event.key === 'Escape' && openMenu) {
      closeMenuRoot(openMenu, true);
    }
  });
})();
