(function () {
    const dropZone = document.getElementById('fileDropZone');
    const fileInput = document.getElementById('fileInput');
    const titleInput = document.getElementById('Title');
    const detectedTypeInput = document.getElementById('detectedType');
    const previewContainer = document.getElementById('previewContainer');
    const previewContent = document.getElementById('previewContent');

    if (!dropZone || !fileInput || !titleInput || !detectedTypeInput || !previewContainer || !previewContent) {
        return;
    }

    const parseAllowedExtensions = () => {
        try {
            const raw = dropZone.dataset.allowedExtensions;
            return raw ? JSON.parse(raw) : {};
        } catch {
            return {};
        }
    };

    const allowedExtensions = parseAllowedExtensions();
    let autoTitle = '';
    let previewUrl = null;

    const setDetectedType = (typeLabel) => {
        detectedTypeInput.value = typeLabel || '';
    };

    const detectTypeFromExtension = (extension) => {
        const loweredExtension = (extension || '').toLowerCase();
        return Object.entries(allowedExtensions).find(([, list]) => list.includes(loweredExtension))?.[0] ?? null;
    };

    const prettifyBytes = (bytes) => {
        if (!Number.isFinite(bytes) || bytes <= 0) {
            return '';
        }

        const units = ['bytes', 'KB', 'MB', 'GB'];
        let unitIndex = 0;
        let value = bytes;

        while (value >= 1024 && unitIndex < units.length - 1) {
            value /= 1024;
            unitIndex++;
        }

        const decimals = unitIndex === 0 ? 0 : 1;
        return `${value.toFixed(decimals)} ${units[unitIndex]}`;
    };

    const clearPreview = () => {
        if (previewUrl) {
            URL.revokeObjectURL(previewUrl);
            previewUrl = null;
        }

        previewContent.innerHTML = '';
        previewContainer.classList.add('hidden');
    };

    const showPreview = (file, detectedType) => {
        clearPreview();

        if (!file) {
            return;
        }

        if (detectedType === 'Picture') {
            const reader = new FileReader();
            reader.onload = (event) => {
                const image = document.createElement('img');
                image.src = event.target?.result ?? '';
                image.alt = 'Selected picture preview';
                image.className = 'mx-auto max-h-80 rounded-xl object-contain shadow-sm';
                previewContent.replaceChildren(image);
                previewContainer.classList.remove('hidden');
            };
            reader.readAsDataURL(file);
        } else if (detectedType === 'Video') {
            previewUrl = URL.createObjectURL(file);
            const video = document.createElement('video');
            video.src = previewUrl;
            video.controls = true;
            video.preload = 'metadata';
            video.className = 'mx-auto w-full max-h-80 rounded-xl shadow-sm';
            previewContent.replaceChildren(video);
            previewContainer.classList.remove('hidden');
        } else {
            const wrapper = document.createElement('div');
            wrapper.className = 'document-preview';
            wrapper.innerHTML = `
                <span class="icon">📄</span>
                <span>
                    ${file.name}
                    <div class="text-xs text-slate-500">Preview unavailable for documents</div>
                </span>`;
            previewContent.replaceChildren(wrapper);
            previewContainer.classList.remove('hidden');
        }
    };

    const updateFromFile = (file) => {
        if (!file) {
            setDetectedType('');
            clearPreview();
            return;
        }

        const fileName = file.name;
        const extension = fileName.includes('.') ? `.${fileName.split('.').pop()?.toLowerCase() ?? ''}` : '';
        const detectedType = detectTypeFromExtension(extension);

        if (!titleInput.dataset.userEdited || titleInput.dataset.userEdited !== 'true') {
            autoTitle = fileName.replace(/\.[^/.]+$/, '');
            titleInput.value = autoTitle;
        }

        const sizeLabel = prettifyBytes(file.size);
        const typeSummary = detectedType ? `${detectedType}${sizeLabel ? ` • ${sizeLabel}` : ''}` : '';
        setDetectedType(typeSummary || `Unknown${sizeLabel ? ` • ${sizeLabel}` : ''}`);
        showPreview(file, detectedType);
    };

    dropZone.addEventListener('click', () => fileInput.click());

    dropZone.addEventListener('dragover', (event) => {
        event.preventDefault();
        dropZone.classList.add('dragover');
    });

    dropZone.addEventListener('dragleave', () => dropZone.classList.remove('dragover'));

    dropZone.addEventListener('drop', (event) => {
        event.preventDefault();
        dropZone.classList.remove('dragover');

        if (event.dataTransfer?.files?.length) {
            fileInput.files = event.dataTransfer.files;
            updateFromFile(fileInput.files[0]);
        }
    });

    fileInput.addEventListener('change', () => {
        updateFromFile(fileInput.files?.[0]);
    });

    titleInput.addEventListener('input', () => {
        const userEdited = titleInput.value !== autoTitle;
        titleInput.dataset.userEdited = userEdited ? 'true' : 'false';
    });
})();
