(() => {
    const guides = {
        store: ['เปิดร้านออนไลน์', [
            ['ตั้งค่าร้านของคุณ', 'สร้างร้าน ตั้งชื่อร้านและเบอร์โทร แล้วเปิดเผยแพร่ร้านเมื่อพร้อมขาย', '/MyStore'],
            ['เลือกสินค้าที่จะขาย', 'เปิดสินค้าที่ต้องการแสดง และตรวจสอบสต็อกร้านให้เพียงพอ', '/MyStore/Products'],
            ['เปิดช่องทางขนส่ง', 'เลือกผู้ขนส่งที่ร้านใช้ หรือเพิ่มรายใหม่ ระบบไม่อนุญาตชื่อซ้ำ', '/MyStore/Shipping'],
            ['แชร์ร้านให้ลูกค้า', 'คัดลอกลิงก์ร้านหรือดาวน์โหลด QR จากหน้าร้านของฉัน', '/MyStore']]],
        buy: ['ซื้อสินค้าและชำระเงิน', [
            ['เลือกสินค้า', 'เพิ่มสินค้าลงตะกร้าและตรวจจำนวนก่อนเริ่มชำระเงิน', '/Catalog'],
            ['กรอกข้อมูลจัดส่ง', 'ระบุผู้รับ เบอร์โทร บ้านเลขที่ จังหวัด อำเภอ ตำบล รหัสไปรษณีย์ และเลือกผู้ขนส่งในตะกร้า', '/Cart'],
            ['โอนเงินและแนบสลิป', 'เปิดเลขที่ใบสั่งซื้อ ดูยอดและบัญชีรับเงิน แนบภาพสลิปพร้อมข้อมูลอ้างอิง การแนบสลิปยังไม่ใช่การยืนยันรับเงิน', '/Member#orders'],
            ['ติดตามผล', 'เปิดออเดอร์เพื่อตรวจการยืนยันชำระและสถานะจัดส่ง ข้อมูลรีเฟรชอัตโนมัติขณะเปิดหน้า', '/Member#orders']]],
        pos: ['ขายหน้าร้าน POS', [
            ['เปิดจุดขาย', 'เลือกจุดขายและสินค้า ตรวจจำนวนกับยอดรวม', '/Pos'],
            ['ระบุลูกค้าและ Token', 'เลือกสมาชิก หากใช้ Token ให้ลูกค้าสร้างรหัสอนุมัติสำหรับจุดขายนี้', '/Pos'],
            ['รับชำระและบันทึก', 'เลือกเงินสดหรือโอนเงิน สำหรับโอนเงินให้เก็บภาพหรือถ่ายสลิป ตรวจยอดก่อนบันทึกการขาย', '/Pos']]],
        ship: ['แพ็กและจัดส่งสินค้า', [
            ['ตรวจการชำระเงิน', 'เปิดเลขที่ออเดอร์เพื่อดูสลิป รอฝ่ายการเงินยืนยันรับเงินก่อนเริ่มเตรียมสินค้า', '/MyStore/Orders'],
            ['เตรียมสินค้าและพิมพ์ A5', 'กดเริ่มเตรียมสินค้า แล้วเลือกพิมพ์ใบจ่าหน้า A5 ตรวจที่อยู่ผู้ส่งและผู้รับ ตั้งกระดาษ A5 แนวตั้ง ขนาดจริง 100%', '/MyStore/Orders'],
            ['บันทึกเลขพัสดุ', 'หลังส่งสินค้า เลือกผู้ขนส่งจริงและกรอกเลขติดตามให้ตรงกับใบรับฝาก', '/MyStore/Orders'],
            ['ยืนยันส่งมอบ', 'ตรวจผลการจัดส่งก่อนกดยืนยันส่งมอบแล้ว ระบบนี้ต้องให้ร้านอัปเดตสถานะเอง', '/MyStore/Orders']]],
        token: ['ตรวจสอบ Token', [
            ['เข้าใจยอดคงเหลือ', 'ยอดพร้อมใช้คือยอดที่ใช้ได้ ยอดจองถูกหักออกจากยอดพร้อมใช้แล้ว ส่วนยอดรอปลดล็อกยังใช้ไม่ได้', '/Member#wallet'],
            ['ตรวจประวัติรายการ', 'ดูรายการรับ ใช้ จอง และคืน Token พร้อมสถานะในแท็บ Token / ประวัติรายการ', '/Member#wallet'],
            ['อนุมัติใช้ที่ POS', 'เปิดส่วนอนุมัติใช้ Token กรอกรหัสจุดขายและวงเงิน รหัสใช้ครั้งเดียวภายใน 5 นาที', '/Member']]],
        team: ['ดูเครือข่ายและยอดขาย', [
            ['แชร์ลิงก์แนะนำ', 'เปิดแท็บเครือข่าย คัดลอกลิงก์หรือดาวน์โหลด QR ของคุณเพื่อแนะนำสินค้า', '/Member#network'],
            ['ดูสมาชิกในสายงาน', 'ขยายต้นไม้เครือข่าย แล้วกดดูรายละเอียดของดาวน์ไลน์ที่ต้องการ', '/Member#network'],
            ['ตรวจยอดขาย', 'ดูยอดขายส่วนตัวและทีมบนแดชบอร์ด หรือเปิดรายงานยอดขายเพื่อดูเพิ่มเติม', '/Dashboard/Mine']]]
    };
    const modal = document.getElementById('member-guide');
    let guide, step = 0;
    function render(focus) {
        const [title, description, link] = guide[1][step];
        document.getElementById('member-guide-title').textContent = guide[0];
        document.getElementById('guide-progress').textContent = `ขั้นตอน ${step + 1} จาก ${guide[1].length}`;
        const bar = document.getElementById('guide-bar'), percent = Math.round((step + 1) / guide[1].length * 100);
        bar.style.width = percent + '%'; bar.parentElement.setAttribute('aria-valuenow', percent);
        document.getElementById('guide-step-title').textContent = title;
        document.getElementById('guide-description').textContent = description;
        document.getElementById('guide-action').href = link;
        document.getElementById('guide-back').disabled = step === 0;
        document.getElementById('guide-next').textContent = step === guide[1].length - 1 ? 'เข้าใจแล้ว' : 'ถัดไป';
        if (focus) document.getElementById('guide-step-title').focus();
    }
    modal.addEventListener('show.bs.modal', e => { guide = guides[e.relatedTarget.dataset.memberGuide]; step = 0; render(false); });
    document.getElementById('guide-back').addEventListener('click', () => { if (step > 0) { step--; render(true); } });
    document.getElementById('guide-next').addEventListener('click', () => {
        if (step < guide[1].length - 1) { step++; render(true); }
        else bootstrap.Modal.getInstance(modal).hide();
    });
    // Links to tabs on this page must also activate Bootstrap's tab state.
    document.addEventListener('click', e => {
        const a = e.target.closest('a[href^="/Member#"]'); if (!a) return;
        const hash = new URL(a.href).hash, tab = document.querySelector(`[data-bs-target="${hash}"]`);
        if (tab) { e.preventDefault(); bootstrap.Modal.getInstance(modal)?.hide(); bootstrap.Tab.getOrCreateInstance(tab).show(); history.replaceState(null, '', hash); tab.scrollIntoView({ block: 'center' }); }
    });
})();
