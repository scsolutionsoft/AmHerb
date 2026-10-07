(() => {
    const modalElement = document.getElementById('attachment-preview-modal');
    if (!modalElement) return;
    const image = document.getElementById('attachment-preview-image');
    const frame = document.getElementById('attachment-preview-frame');
    const office = document.getElementById('attachment-preview-office');
    const title = document.getElementById('attachment-preview-title');
    const download = document.getElementById('attachment-preview-download');
    document.querySelectorAll('[data-attachment-preview]').forEach(button => button.addEventListener('click', () => {
        const url = button.dataset.url, type = button.dataset.type, name = button.dataset.name;
        title.textContent = name; download.href = `${url}?download=true`; download.download = name;
        const isImage = type.startsWith('image/'), isBrowserDocument = type === 'application/pdf' || type === 'text/plain';
        image.hidden = !isImage; frame.hidden = !isBrowserDocument; office.hidden = isImage || isBrowserDocument;
        if (isImage) { image.src = url; image.alt = `พรีวิว ${name}`; frame.removeAttribute('src'); }
        else if (isBrowserDocument) { frame.src = url; image.removeAttribute('src'); }
        else { image.removeAttribute('src'); frame.removeAttribute('src'); }
        bootstrap.Modal.getOrCreateInstance(modalElement).show();
    }));
    modalElement.addEventListener('hidden.bs.modal', () => { image.removeAttribute('src'); frame.removeAttribute('src'); });
})();
