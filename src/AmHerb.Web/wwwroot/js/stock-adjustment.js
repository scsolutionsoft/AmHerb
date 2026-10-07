(() => {
    const modal = document.getElementById('stock-adjust-modal'); if (!modal) return;
    const form = document.getElementById('stock-adjust-form'), select = form.elements.batchId, delta = form.elements.delta, save = document.getElementById('stock-adjust-save');
    const error = document.getElementById('stock-adjust-error'), reload = document.getElementById('stock-adjust-reload');
    let lots = [], url, revision = 0, busy = false;
    function showError(message) { error.textContent = message; error.hidden = !message; }
    function preview() {
        const lot = lots.find(x => String(x.id) === select.value), amount = Number(delta.value);
        const valid = lot && delta.value !== '' && Number.isInteger(amount) && amount !== 0 && lot.available + amount >= 0 && lot.available + amount <= 2147483647 && (form.elements.kind.value === 'Adjust' || amount < 0);
        delta.setCustomValidity(valid || delta.value === '' ? '' : 'ตรวจจำนวนที่ปรับ: ห้ามเป็นศูนย์ ห้ามลดยอดติดลบ และชำรุด/หมดอายุต้องเป็นจำนวนลด');
        document.getElementById('stock-adjust-preview').textContent = valid ? `หลังปรับ: ยังไม่จอง ${lot.available + amount} ชิ้น · รวมจอง ${lot.available + amount + lot.reserved} ชิ้น` : '';
        save.disabled = busy || !valid;
    }
    function choose() {
        const lot = lots.find(x => String(x.id) === select.value);
        form.elements.version.value = lot?.version || '';
        document.getElementById('stock-adjust-product').textContent = lot ? `${lot.product} · ${lot.sku}` : '';
        document.getElementById('stock-adjust-location').textContent = lot ? `${lot.warehouse} · หมดอายุ ${lot.expiry}` : '';
        document.getElementById('stock-adjust-balance').textContent = lot ? `ยังไม่จอง ${lot.available} ชิ้น · จอง ${lot.reserved} ชิ้น` : '';
        preview();
    }
    async function load() {
        const current = ++revision, previous = select.value;
        busy = true; save.disabled = true; showError('');
        try {
            const response = await fetch(url, { cache: 'no-store', headers: { Accept: 'application/json' } });
            if (!response.ok || !response.headers.get('content-type')?.includes('application/json')) throw new Error('โหลดข้อมูลไม่ได้ กรุณาตรวจสิทธิ์หรือเข้าสู่ระบบใหม่');
            const data = await response.json(); if (current !== revision) return;
            lots = data; select.replaceChildren(...lots.map(x => new Option(`ล็อต ${x.lotNo} (#${x.id})`, x.id)));
            if (lots.some(x => String(x.id) === previous)) select.value = previous;
            delta.value = ''; if (!lots.length) showError('ไม่พบล็อตสินค้านี้');
        } catch (e) { if (current !== revision) return; lots = []; select.replaceChildren(); showError(e.message); }
        finally { if (current === revision) { busy = false; choose(); } }
    }
    modal.addEventListener('show.bs.modal', e => {
        form.reset(); lots = []; choose();
        const d = e.relatedTarget.dataset;
        url = '/Stock/Adjustment?' + new URLSearchParams(d.batch ? { batchId: d.batch } : { skuId: d.sku, warehouseId: d.warehouse });
        load();
    });
    select.addEventListener('change', () => { delta.value = ''; choose(); }); delta.addEventListener('input', preview); form.elements.kind.addEventListener('change', preview); reload.addEventListener('click', load);
    form.addEventListener('submit', async e => {
        e.preventDefault(); if (busy || !form.reportValidity() || save.disabled) return;
        busy = true; save.disabled = true; reload.disabled = true; showError('');
        try {
            const response = await fetch(form.action, { method: 'POST', body: new FormData(form), headers: { Accept: 'application/json' } });
            const result = response.headers.get('content-type')?.includes('application/json') ? await response.json() : {};
            if (!response.ok || !result.success) throw new Error(result.message || 'บันทึกไม่สำเร็จ กรุณาโหลดข้อมูลล่าสุดก่อนลองอีกครั้ง');
            location.reload();
        } catch (e) { showError(e.message); }
        finally { busy = false; reload.disabled = false; /* A fresh version is required before retrying. */ save.disabled = true; lots = []; }
    });
})();
