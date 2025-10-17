(() => {
  const infoToggle = document.querySelector('[data-info-toggle]');
  const infoPanel = document.querySelector('[data-info-panel]');
  const infoClose = document.querySelector('[data-info-close]');
  const feedback = document.querySelector('[data-feedback]');
  const renameDisplay = document.querySelector('[data-file-name-display]');
  const renameForm = document.getElementById('renameForm');

  const getFeedbackClasses = (status) => {
    switch (status) {
      case 'success':
        return ['border-emerald-200', 'bg-emerald-50', 'text-emerald-700'];
      case 'error':
        return ['border-red-200', 'bg-red-50', 'text-red-700'];
      default:
        return ['border-slate-200', 'bg-slate-50', 'text-slate-600'];
    }
  };

  const resetFeedbackClasses = () => {
    if (!feedback) {
      return;
    }
    feedback.classList.remove(
      'border-emerald-200',
      'bg-emerald-50',
      'text-emerald-700',
      'border-red-200',
      'bg-red-50',
      'text-red-700',
      'border-slate-200',
      'bg-slate-50',
      'text-slate-600'
    );
  };

  const announce = (message, status = 'neutral') => {
    if (!feedback) {
      return;
    }

    resetFeedbackClasses();
    feedback.textContent = message;
    feedback.classList.remove('hidden');
    feedback.classList.add(...getFeedbackClasses(status));
  };

  const clearFeedback = () => {
    if (!feedback) {
      return;
    }
    feedback.textContent = '';
    feedback.classList.add('hidden');
    resetFeedbackClasses();
  };

  const toggleInfoPanel = (show) => {
    if (!infoPanel || !infoToggle) {
      return;
    }
    const shouldShow = typeof show === 'boolean'
      ? show
      : infoPanel.hasAttribute('hidden');

    if (shouldShow) {
      infoPanel.removeAttribute('hidden');
    } else {
      infoPanel.setAttribute('hidden', 'hidden');
    }

    infoToggle.setAttribute('aria-expanded', shouldShow.toString());
  };

  infoToggle?.addEventListener('click', () => toggleInfoPanel());
  infoClose?.addEventListener('click', () => toggleInfoPanel(false));

  let isEditing = false;
  let isSaving = false;

  const restoreTitle = (container, newTitle, icon) => {
    const span = document.createElement('span');
    span.setAttribute('data-file-name-text', '');
    span.className = 'max-w-xl truncate font-semibold text-slate-900';
    span.textContent = newTitle;
    container.replaceChildren(icon, span);
    isEditing = false;
  };

  const startRename = () => {
    if (!renameDisplay || !renameForm || isEditing) {
      return;
    }

    const iconElement = renameDisplay.querySelector('i');
    const icon = iconElement
      ? iconElement.cloneNode(true)
      : (() => {
        const fallback = document.createElement('i');
        fallback.className = 'bi bi-file-earmark text-lg text-indigo-500';
        return fallback;
      })();
    const currentSpan = renameDisplay.querySelector('[data-file-name-text]');
    if (!currentSpan) {
      return;
    }

    const currentTitle = currentSpan.textContent?.trim() ?? '';
    const input = document.createElement('input');
    input.type = 'text';
    input.value = currentTitle;
    input.maxLength = 200;
    input.className = 'w-full min-w-[12rem] max-w-xl rounded-md border border-indigo-200 bg-white px-3 py-1.5 text-sm font-semibold text-slate-900 shadow-inner focus:border-indigo-400 focus:outline-none focus:ring-2 focus:ring-indigo-400/60';

    renameDisplay.replaceChildren(icon, input);
    isEditing = true;
    clearFeedback();

    const cancelEditing = (force = false) => {
      if (isSaving && !force) {
        return;
      }

      restoreTitle(renameDisplay, currentTitle, icon);
    };

    const submitRename = async () => {
      if (isSaving) {
        return;
      }

      const nextTitle = input.value.trim();
      if (nextTitle.length === 0) {
        announce('Title is required.', 'error');
        input.focus();
        return;
      }

      if (nextTitle === currentTitle) {
        restoreTitle(renameDisplay, currentTitle, icon);
        return;
      }

      if (!(renameForm instanceof HTMLFormElement)) {
        announce('Rename form is not available.', 'error');
        cancelEditing(true);
        return;
      }

      const formData = new FormData(renameForm);
      formData.set('title', nextTitle);
      isSaving = true;
      input.setAttribute('disabled', 'disabled');
      announce('Saving...', 'neutral');

      try {
        const response = await fetch(renameForm.action, {
          method: 'POST',
          body: formData,
          headers: {
            'X-Requested-With': 'XMLHttpRequest'
          }
        });

        const data = await response.json().catch(() => ({}));

        if (!response.ok) {
          const message = typeof data.error === 'string'
            ? data.error
            : 'We could not rename the file right now.';
          announce(message, 'error');
          cancelEditing(true);
          return;
        }

        const updatedTitle = typeof data.title === 'string' && data.title.length > 0
          ? data.title
          : nextTitle;

        announce('File name updated.', 'success');
        restoreTitle(renameDisplay, updatedTitle, icon);
        document.title = `${updatedTitle} preview - RoomCast`;
      } catch (error) {
        console.error('Rename failed', error);
        announce('Something went wrong while renaming. Please try again.', 'error');
        cancelEditing(true);
      } finally {
        isSaving = false;
      }
    };

    input.addEventListener('keydown', (event) => {
      if (event.key === 'Enter') {
        event.preventDefault();
        submitRename();
      }
      else if (event.key === 'Escape') {
        event.preventDefault();
        cancelEditing();
      }
    });

    input.addEventListener('blur', () => {
      if (!isSaving) {
        void submitRename();
      }
    });

    input.focus();
    input.select();
  };

  renameDisplay?.addEventListener('dblclick', startRename);
})();
