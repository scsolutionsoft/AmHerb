document.querySelectorAll('[data-slip-upload]').forEach(box => {
    const file = box.querySelector('[data-slip-file]');
    const preview = box.querySelector('[data-slip-preview]');
    const error = box.querySelector('[data-slip-error]');
    let url;
    box.querySelector('[data-slip-camera]').addEventListener('click', () => {
        file.setAttribute('capture', 'environment'); file.click();
    });
    file.addEventListener('pointerdown', () => file.removeAttribute('capture'));
    file.addEventListener('change', () => {
        if (url) URL.revokeObjectURL(url);
        preview.classList.add('d-none'); error.textContent = '';
        const selected = file.files[0];
        if (!selected) return;
        if (!['image/jpeg', 'image/png', 'image/webp'].includes(selected.type) || selected.size > 5 * 1024 * 1024) {
            error.textContent = 'กรุณาเลือกภาพ JPG, PNG หรือ WebP ไม่เกิน 5 MB'; file.value = ''; return;
        }
        url = URL.createObjectURL(selected); preview.src = url; preview.classList.remove('d-none');
    });
});
const paymentMethod = document.querySelector('#method');
if (paymentMethod) {
    const toggle = () => document.querySelectorAll('[data-pos-transfer]').forEach(x => {
        x.hidden = paymentMethod.value !== 'ManualBankTransfer';
        x.querySelectorAll('input').forEach(input => input.disabled = x.hidden);
    });
    paymentMethod.addEventListener('change', toggle); toggle();
}
const guide = document.querySelector('#pos-guide');
if (guide) {
    const steps = [...guide.querySelectorAll('[data-pos-guide-step]')];
    const back = guide.querySelector('[data-pos-guide-back]'), next = guide.querySelector('[data-pos-guide-next]');
    let index = 0;
    const show = () => { steps.forEach((x, i) => x.classList.toggle('d-none', i !== index)); back.disabled = index === 0; next.textContent = index === 2 ? 'พร้อมเริ่มขาย' : 'ถัดไป'; };
    next.addEventListener('click', () => { if (index === 2) bootstrap.Modal.getOrCreateInstance(guide).hide(); else { index++; show(); } });
    back.addEventListener('click', () => { index--; show(); });
    guide.addEventListener('show.bs.modal', () => { index = 0; show(); });
}
