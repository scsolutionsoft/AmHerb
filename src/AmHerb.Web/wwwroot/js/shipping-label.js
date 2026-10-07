(() => {
    const sheet = document.querySelector('.label-sheet');
    const resize = () => { sheet.style.zoom = Math.min(1, (document.querySelector('.preview').clientWidth - 32) / (148 * 96 / 25.4)); };
    window.addEventListener('resize', resize);
    resize();
    for (const field of ['name', 'phone', 'address', 'postcode']) {
        document.getElementById('sender-' + field).addEventListener('input', e => {
            document.querySelector('[data-sender="' + field + '"]').textContent = e.target.value;
        });
    }
    document.getElementById('print-label').addEventListener('click', () => {
        const error = document.getElementById('print-error');
        const missing = ['name', 'address'].some(f => !document.getElementById('sender-' + f).value.trim());
        const invalidPostcode = !/^[1-9][0-9]{4}$/.test(document.getElementById('sender-postcode').value.trim());
        const recipientMissing = !document.querySelector('[data-recipient-name]').textContent.trim()
            || !document.querySelector('[data-recipient-address]').textContent.trim()
            || !/^[1-9][0-9]{4}$/.test(document.querySelector('[data-recipient-postcode]')?.dataset.recipientPostcode || '');
        // At CSS's fixed 96dpi, A5 is 793.7px tall. Do not silently clip long addresses.
        const oversized = sheet.offsetHeight > 796;
        error.hidden = !missing && !invalidPostcode && !recipientMissing && !oversized;
        error.textContent = missing ? 'กรอกชื่อและที่อยู่ผู้ส่งก่อนพิมพ์ โดยเปิดแก้ข้อมูลผู้ส่งด้านบน'
            : invalidPostcode ? 'กรอกรหัสไปรษณีย์ผู้ส่งเป็นเลขอารบิก 5 หลัก'
            : recipientMissing ? 'ข้อมูลผู้รับไม่ครบ กรุณาตรวจชื่อ ที่อยู่ และรหัสไปรษณีย์ในออเดอร์ก่อนพิมพ์'
            : 'ข้อความยาวเกินกระดาษ A5 กรุณาตรวจและย่อข้อมูลที่อยู่ก่อนพิมพ์';
        if (!error.hidden) { if (missing || invalidPostcode) document.querySelector('details').open = true; error.scrollIntoView({ block: 'center' }); return; }
        window.print();
    });
})();
