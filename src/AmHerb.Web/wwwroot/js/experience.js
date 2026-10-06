'use strict';
document.addEventListener('click', event => {
  const image = event.target.closest('[data-image]');
  if (image) { const preview = document.getElementById('image-modal-image'); preview.src = image.dataset.image; preview.alt = image.dataset.caption || 'ภาพสินค้า'; document.getElementById('image-modal-title').textContent = preview.alt; bootstrap.Modal.getOrCreateInstance(document.getElementById('image-modal')).show(); }
  const toggle = event.target.closest('[data-password-toggle]');
  if (toggle) { const input = document.getElementById(toggle.dataset.passwordToggle); input.type = input.type === 'password' ? 'text' : 'password'; toggle.textContent = input.type === 'password' ? 'แสดง' : 'ซ่อน'; toggle.setAttribute('aria-label', input.type === 'password' ? 'แสดงรหัสผ่าน' : 'ซ่อนรหัสผ่าน'); }
  if (event.target.closest('[data-print]')) {
    const copy = document.querySelector('main').cloneNode(true);
    copy.querySelectorAll('.no-print,form,script,button').forEach(x => x.remove());
    copy.querySelectorAll('[id]').forEach(x => x.removeAttribute('id'));
    copy.querySelectorAll('details').forEach(x => x.open = true);
    const target = document.querySelector('.report-preview'); target.replaceChildren(copy);
    bootstrap.Modal.getOrCreateInstance(document.getElementById('report-modal')).show();
  }
  if (event.target.closest('[data-print-confirm]')) {
    const popup = window.open('', '_blank', 'width=1100,height=800');
    if (!popup) { window.print(); return; }
    popup.document.title = document.title;
    const base = popup.document.createElement('base'); base.href = location.origin + '/'; popup.document.head.append(base);
    document.querySelectorAll('link[rel="stylesheet"]').forEach(x => { const link = popup.document.createElement('link'); link.rel = 'stylesheet'; link.href = x.href; popup.document.head.append(link); });
    const body = document.querySelector('.report-preview').cloneNode(true); popup.document.body.append(body);
    const button = popup.document.createElement('button'); button.textContent = 'พิมพ์ / บันทึก PDF'; button.className = 'btn btn-success no-print'; button.addEventListener('click', () => popup.print()); popup.document.body.prepend(button);
    popup.focus();
  }
});
document.querySelectorAll('[data-mark-read]').forEach(form => fetch(form.action, {method:'POST', body:new FormData(form), credentials:'same-origin'}).catch(() => {}));
const badge = document.getElementById('message-badge');
if (badge) { const update = async () => { try { const response = await fetch('/Messages/Unread'); if (!response.ok) return; const result = await response.json(); badge.textContent = result.count ? String(result.count) : ''; badge.hidden = !result.count; } catch {} }; update(); setInterval(update,30000); }
