(() => {
    const form = document.querySelector('[data-checkout-wizard]');
    if (!form) return;
    const steps = [...form.querySelectorAll('[data-checkout-step]')];
    let step = 0;
    const field = name => form.querySelector('[name="Checkout.' + name + '"]');
    const province = document.querySelector('#address-province');
    const district = document.querySelector('#address-district');
    const sub = field('SubdistrictCode'), village = field('VillageCode'), postcode = field('PostalCode');
    const areaError = form.querySelector('[data-address-error]');
    const areaSummary = form.querySelector('[data-address-summary]');
    let subdistricts = [];
    const reset = (select, label) => { select.replaceChildren(new Option(label, '')); select.disabled = true; select.dataset.request = ''; };
    async function children(parent, target, label) {
        const key = parent + ':' + Date.now(); reset(target, 'กำลังโหลด…'); target.dataset.request = key;
        areaError.textContent = '';
        try {
            const response = await fetch('/Addresses/Children?parent=' + encodeURIComponent(parent));
            if (!response.ok) throw new Error();
            const rows = await response.json();
            if (target.dataset.request !== key) return [];
            target.replaceChildren(new Option(label, ''), ...rows.map(x => new Option(x.name, x.id)));
            target.disabled = false; return rows;
        } catch {
            if (target.dataset.request === key) { reset(target, 'โหลดไม่สำเร็จ'); areaError.textContent = 'โหลดพื้นที่ไม่สำเร็จ กรุณาเลือกจังหวัดหรืออำเภออีกครั้ง'; }
            return [];
        }
    }
    const clearArea = () => { reset(postcode, 'เลือกพื้นที่ก่อน'); field('VillageName').value = ''; areaSummary.textContent = 'กรุณาเลือกพื้นที่จัดส่ง'; };
    province.addEventListener('change', () => {
        reset(district, 'เลือกจังหวัดก่อน'); reset(sub, 'เลือกอำเภอก่อน'); reset(village, 'ไม่ระบุ / กรอกเอง'); clearArea();
        if (province.value) children(province.value, district, 'เลือกอำเภอ / เขต');
    });
    district.addEventListener('change', async () => {
        reset(sub, 'เลือกอำเภอก่อน'); reset(village, 'ไม่ระบุ / กรอกเอง'); clearArea();
        if (district.value) subdistricts = await children(district.value, sub, 'เลือกตำบล / แขวง');
    });
    sub.addEventListener('change', () => {
        reset(village, 'ไม่ระบุ / กรอกเอง'); clearArea();
        const row = subdistricts.find(x => x.id === sub.value);
        if (!row) return;
        postcode.replaceChildren(...row.postcodes.map(x => new Option(x, x))); postcode.disabled = false;
        areaSummary.textContent = [row.name, district.selectedOptions[0].text, province.selectedOptions[0].text].join(' · ');
        children(row.id, village, 'ไม่ระบุ / กรอกเอง');
    });
    village.addEventListener('change', () => field('VillageName').value = village.value ? village.selectedOptions[0].text : '');
    form.querySelector('[data-address-confirm]').addEventListener('click', () => {
        if (!sub.value || !postcode.value) { areaError.textContent = 'กรุณาเลือกจังหวัด อำเภอ และตำบลให้ครบ'; return; }
        bootstrap.Modal.getOrCreateInstance(document.querySelector('#address-picker')).hide();
    });
    const show = index => {
        step = index;
        steps.forEach((x, i) => x.classList.toggle('d-none', i !== step));
        form.querySelectorAll('[data-step-label]').forEach((x, i) => { x.classList.toggle('fw-bold', i === step); x.setAttribute('aria-current', i === step ? 'step' : 'false'); });
        const carrier = field('ShippingProviderId');
        form.querySelector('[data-checkout-summary]').textContent = [field('CustomerName').value, field('Phone').value, field('HouseNumber').value, field('VillageName').value, field('AddressExtra').value, areaSummary.textContent, postcode.value, carrier.selectedOptions[0]?.text].filter(Boolean).join(' · ');
    };
    const validate = index => {
        if (index === 1 && (!sub.value || !postcode.value)) {
            areaError.textContent = 'กรุณาเลือกพื้นที่จัดส่งให้ครบ'; bootstrap.Modal.getOrCreateInstance(document.querySelector('#address-picker')).show(); return false;
        }
        const invalid = [...steps[index].querySelectorAll('input,select,textarea')].find(x => !x.checkValidity());
        if (invalid) { invalid.reportValidity(); return false; } return true;
    };
    form.querySelectorAll('[data-next]').forEach(x => x.addEventListener('click', () => { if (validate(step)) show(step + 1); }));
    form.querySelectorAll('[data-back]').forEach(x => x.addEventListener('click', () => show(step - 1)));
    form.addEventListener('submit', event => {
        for (let i = 0; i < steps.length; i++) { show(i); if (!validate(i)) { event.preventDefault(); return; } }
        form.querySelector('[type=submit]').disabled = true;
    });
    show(0);
})();
