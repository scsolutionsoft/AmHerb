(() => {
    const request = document.getElementById('transfer-request'), context = document.getElementById('transfer-context');
    if (!request && !context) return;
    const error = document.getElementById('transfer-stock-error'), quantity = document.getElementById('transfer-quantity');
    let snapshot, sequence = 0, step = 0, pendingForm, posting = false;
    let showStockError = () => error.scrollIntoView({ block: 'center' });
    const count = () => Number(quantity?.value || context?.dataset.quantity || 0);
    const message = text => { error.textContent = text; error.hidden = !text; };
    function financialPreview() {
        const target = document.getElementById('transfer-financial-preview');
        if (!target || !request) return;
        const method = request.elements.paymentMethod.value, tier = request.elements.priceTier.value;
        request.elements.priceTier.disabled = method === 'NoCharge';
        if (method === 'NoCharge') { target.textContent = 'รายการไม่เรียกเก็บเงิน · ต้องเป็นผู้ดูแลที่มีสิทธิ์'; return; }
        const price = snapshot?.prices?.find(x => x.tier === tier);
        target.textContent = price ? `${tier} · ${Number(price.unitPrice).toLocaleString('th-TH', { minimumFractionDigits: 2 })} บาท/ชิ้น · ยอด PO ${(Number(price.unitPrice) * count()).toLocaleString('th-TH', { minimumFractionDigits: 2 })} บาท` : `ไม่พบราคาที่ใช้ได้สำหรับ ${tier}`;
    }
    function project() {
        const target = document.getElementById('transfer-projection');
        if (!snapshot) { target.textContent = ''; financialPreview(); return; }
        const n = count(), status = context?.dataset.status;
        target.textContent = status === 'Received' ? 'รับเข้าปลายทางแล้ว ยอดด้านบนเป็นยอดปัจจุบันหลังรวมรายการขายและจองอื่น ๆ'
            : status === 'Cancelled' ? 'ใบเบิกยกเลิกแล้ว ไม่มีการตัดสต๊อก'
            : status === 'Dispatched' ? `ต้นทางตัดแล้ว ${n} ชิ้น · เมื่อรับครบ คงคลังปลายทางจะเป็น ${snapshot.destination.onHand + n} ชิ้น (ก่อนรายการอื่นเปลี่ยนแปลง)`
            : n > snapshot.source.available ? `ขอ ${n} ชิ้น แต่พร้อมโอน ${snapshot.source.available} ชิ้น คำขอรอได้ แต่ยังอนุมัติจ่ายไม่ได้`
            : `เมื่อจ่าย ${n} ชิ้น ต้นทางพร้อมโอนจะเหลือ ${snapshot.source.available - n} ชิ้น · ปลายทางเพิ่มเมื่อยืนยันรับเท่านั้น`;
        financialPreview();
    }
    async function load() {
        const mine = ++sequence;
        const query = context ? { transferId: context.dataset.transferId } : { sourceId: request.elements.sourceId.value, destinationId: request.elements.destinationId.value, skuId: request.elements.skuId.value };
        snapshot = null; project(); message('กำลังตรวจยอดล่าสุด…');
        document.querySelectorAll('[data-transfer-details]').forEach(b => b.disabled = true);
        for (const side of ['source', 'destination']) { document.getElementById(`transfer-${side}-ready`).textContent = '—'; document.getElementById(`transfer-${side}-info`).textContent = ''; }
        try {
            const response = await fetch('/Transfers/Preview?' + new URLSearchParams(query), { cache: 'no-store', headers: { Accept: 'application/json' } });
            const data = response.headers.get('content-type')?.includes('application/json') ? await response.json() : {};
            if (!response.ok || !data.source) throw new Error(data.message || 'ไม่สามารถดูข้อมูลคลังนี้ได้ กรุณาตรวจสิทธิ์และเลือกคลังอีกครั้ง');
            if (mine !== sequence) return null;
            snapshot = data;
            for (const side of ['source', 'destination']) {
                const stock = data[side];
                document.getElementById(`transfer-${side}-name`).textContent = stock.name;
                document.getElementById(`transfer-${side}-ready`).textContent = `${stock.available.toLocaleString()} ชิ้น พร้อมโอน`;
                document.getElementById(`transfer-${side}-info`).textContent = `คงคลัง ${stock.onHand} · จอง ${stock.reserved} · ใช้ไม่ได้ ${stock.unavailable} · กำลังเข้า ${stock.incoming} · คำขอรอจ่าย ${stock.requested}`;
            }
            document.getElementById('transfer-check-time').textContent = `ตรวจล่าสุด ${data.checkedAt} · ${data.product} (${data.code})`;
            document.querySelectorAll('[data-transfer-details]').forEach(b => b.disabled = false);
            message(''); project(); return data;
        } catch (e) { if (mine === sequence) message(e.message); return null; }
    }
    document.getElementById('transfer-refresh').addEventListener('click', load);
    quantity?.addEventListener('input', project);
    request?.elements.paymentMethod?.addEventListener('change', financialPreview);
    request?.elements.priceTier?.addEventListener('change', financialPreview);
    if (request) {
        const steps = [...request.querySelectorAll('[data-transfer-step]')], next = document.getElementById('transfer-next'), back = document.getElementById('transfer-back'), submit = document.getElementById('transfer-request-submit');
        request.noValidate = true;
        function render() { steps.forEach((s, i) => s.hidden = step !== i); back.hidden = step === 0; next.hidden = step === 2; submit.hidden = step !== 2; document.getElementById('transfer-step-label').textContent = `ขั้นตอน ${step + 1} / 3 · ${['เลือกคลังและสินค้า', 'ตรวจยอดและจำนวน', 'ระบุเหตุผลและยืนยัน'][step]}`; }
        showStockError = () => { step = 1; render(); error.scrollIntoView({ block: 'center' }); };
        next.addEventListener('click', async () => {
            if (![...steps[step].querySelectorAll('input,select')].every(x => x.reportValidity())) return;
            if (step === 0) { next.disabled = true; const result = await load(); next.disabled = false; if (!result) { step = 1; render(); return; } }
            else if (!snapshot) { message('โหลดข้อมูลคลังสำเร็จก่อนดำเนินการต่อ'); return; }
            step++; render();
        });
        back.addEventListener('click', () => { step--; render(); });
        for (const name of ['sourceId', 'destinationId', 'skuId']) request.elements[name].addEventListener('change', () => { sequence++; snapshot = null; });
        render();
    } else load();
    document.querySelectorAll('[data-transfer-details]').forEach(button => button.addEventListener('click', () => {
        if (!snapshot) return; const stock = snapshot[button.dataset.transferDetails];
        document.getElementById('transfer-product-title').textContent = stock.name;
        document.getElementById('transfer-product-image').src = snapshot.image;
        document.getElementById('transfer-product-name').textContent = `${snapshot.product} · ${snapshot.code}`;
        document.getElementById('transfer-product-description').textContent = snapshot.description;
        document.getElementById('transfer-lot-caption').textContent = `แสดง ${Math.min(stock.totalLots, 100)} จาก ${stock.totalLots} ล็อต · ยอดสรุปด้านบนรวมทุกล็อต`;
        const tbody = document.getElementById('transfer-lot-rows'); tbody.replaceChildren();
        for (const lot of stock.lots) { const row = document.createElement('tr'); for (const value of [lot.lotNo, lot.expiry, lot.available, lot.reserved, lot.unavailable]) { const td = document.createElement('td'); td.textContent = value; row.append(td); } tbody.append(row); }
        if (!stock.lots.length) { const row = document.createElement('tr'), td = document.createElement('td'); td.colSpan = 5; td.textContent = 'ยังไม่มีล็อตสินค้านี้ในคลัง'; row.append(td); tbody.append(row); }
        bootstrap.Modal.getOrCreateInstance(document.getElementById('transfer-product-modal')).show();
    }));
    const confirmModal = document.getElementById('transfer-confirm-modal'), confirmSave = document.getElementById('transfer-confirm-save'), counted = document.getElementById('transfer-counted');
    document.querySelectorAll('[data-transfer-form]').forEach(form => form.addEventListener('submit', async e => {
        e.preventDefault(); if (posting) return;
        if (form === request && step !== 2) { document.getElementById('transfer-next').click(); return; }
        if (!form.reportValidity()) return;
        const submit = form.querySelector('button[type=submit], button:not([type])'); if (submit) submit.disabled = true;
        const data = await load(); if (submit) submit.disabled = false; if (!data) { showStockError(); return; }
        const action = form.dataset.transferForm, n = count();
        if ((action === 'request' || action === 'dispatch') && (!data.source.active || !data.destination.active)) { message('คลัง เจ้าของคลัง หรือสินค้าปิดใช้งาน กรุณาตรวจสอบก่อนโอน'); showStockError(); return; }
        if (action === 'dispatch' && data.source.available < n) { message('สต๊อกพร้อมโอนล่าสุดไม่เพียงพอ หลังหักยอดจองและรายการอื่นแล้ว'); error.scrollIntoView(); return; }
        pendingForm = form; counted.checked = false; document.getElementById('transfer-receive-check').hidden = action !== 'receive'; confirmSave.disabled = action === 'receive';
        document.getElementById('transfer-confirm-title').textContent = ({ request: 'ยืนยันคำขอเบิกสินค้า', dispatch: 'ยืนยันจ่ายจากคลังต้นทาง', receive: 'ยืนยันรับเข้าคลังปลายทาง', cancel: 'ยืนยันยกเลิกใบเบิก' })[action];
        document.getElementById('transfer-confirm-summary').textContent = `${data.product} (${data.code})\nจาก ${data.source.name}\nไป ${data.destination.name}\nจำนวน ${n} ชิ้น\nต้นทางพร้อมโอน ${data.source.available} · ปลายทางคงคลัง ${data.destination.onHand}\nเหตุผล: ${form.elements.reason.value}`;
        document.getElementById('transfer-confirm-impact').textContent = ({ request: `ยังไม่ตัดหรือกันสต๊อก รอผู้ดูแลอนุมัติจ่าย${n > data.source.available ? ' · ยอดพร้อมโอนขณะนี้ไม่พอ ต้องเติมสินค้าก่อนจ่าย' : ''}`, dispatch: 'ตัดเฉพาะยอดพร้อมโอนต้นทาง เลือกล็อตหมดอายุก่อน และบันทึกเป็นสินค้าระหว่างทาง', receive: `เพิ่มคงคลังปลายทาง ${n} ชิ้นตามล็อตจริง ล็อตหมดอายุจะไม่นับเป็นพร้อมขาย`, cancel: 'ยกเลิกได้ก่อนจ่ายสินค้า ไม่กระทบยอดขายหรือ Token' })[action];
        bootstrap.Modal.getOrCreateInstance(confirmModal).show();
    }));
    counted.addEventListener('change', () => confirmSave.disabled = !counted.checked);
    confirmSave.addEventListener('click', () => { if (!pendingForm || posting || confirmSave.disabled) return; posting = true; confirmSave.disabled = true; confirmSave.textContent = 'กำลังบันทึก…'; HTMLFormElement.prototype.submit.call(pendingForm); });
    const notice = document.getElementById('transfer-notice-modal'); if (notice) bootstrap.Modal.getOrCreateInstance(notice).show();
    setInterval(() => { if (context && !document.hidden && !document.querySelector('.modal.show') && !posting) load(); }, 15000);
})();
