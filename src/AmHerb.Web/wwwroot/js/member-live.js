(() => {
    const panel = document.querySelector('[data-live-page]');
    if (!panel) return;
    const status = panel.querySelector('[data-live-status]');
    const button = panel.querySelector('[data-live-refresh]');
    let busy = false;
    let stopped = false;
    document.addEventListener('input', event => {
        const form = event.target.closest('form');
        if (form) form.dataset.liveDirty = 'true';
    });
    const showTab = () => {
        const tab = [...document.querySelectorAll('[data-bs-toggle="tab"]')]
            .find(x => x.dataset.bsTarget === location.hash);
        if (tab && window.bootstrap) bootstrap.Tab.getOrCreateInstance(tab).show();
    };
    showTab();
    window.addEventListener('hashchange', showTab);
    document.querySelectorAll('[data-bs-toggle="tab"]').forEach(tab =>
        tab.addEventListener('shown.bs.tab', () => history.replaceState(null, '', tab.dataset.bsTarget)));

    async function refresh() {
        if (busy || stopped || document.hidden) return;
        busy = true;
        button.disabled = true;
        const controller = new AbortController();
        const timeout = setTimeout(() => controller.abort(), 10000);
        try {
            const response = await fetch(location.pathname + location.search, {
                cache: 'no-store', credentials: 'same-origin', signal: controller.signal
            });
            if (response.redirected || response.status === 401 || response.status === 403) {
                stopped = true;
                throw new Error('session');
            }
            if (!response.ok) throw new Error('network');
            const next = new DOMParser().parseFromString(await response.text(), 'text/html');
            if (!next.querySelector('[data-live-page]')) throw new Error('response');
            const orderStatus = next.querySelector('[data-order-status]')?.dataset.orderStatus;
            document.querySelectorAll('[data-live-region]').forEach(region => {
                const updated = [...next.querySelectorAll('[data-live-region]')]
                    .find(x => x.dataset.liveRegion === region.dataset.liveRegion);
                if (!updated || region.contains(document.activeElement) || region.querySelector('form[data-live-dirty]')) return;
                const expanded = new Map([...region.querySelectorAll('details[data-node-id]')]
                    .map(x => [x.dataset.nodeId, x.open]));
                updated.querySelectorAll('details[data-node-id]').forEach(x => {
                    if (expanded.has(x.dataset.nodeId)) x.open = expanded.get(x.dataset.nodeId);
                });
                region.replaceWith(updated);
            });
            if (orderStatus && orderStatus !== 'PendingPayment') {
                document.querySelectorAll('[data-pending-payment], form[action$="/Cancel"]')
                    .forEach(x => x.remove());
            }
            status.textContent = 'อัปเดตล่าสุด ' + new Date().toLocaleTimeString('th-TH', { timeZone: 'Asia/Bangkok' }) + ' · อัตโนมัติทุก 5 วินาที';
            status.classList.remove('text-danger');
        } catch {
            status.textContent = stopped ? 'เซสชันสิ้นสุด กรุณาเข้าสู่ระบบใหม่' : 'อัปเดตไม่สำเร็จ · ข้อมูลอาจล้าสมัย กำลังลองใหม่';
            status.classList.add('text-danger');
        } finally {
            clearTimeout(timeout);
            busy = false;
            button.disabled = stopped;
        }
    }
    button.addEventListener('click', refresh);
    window.addEventListener('focus', refresh);
    window.addEventListener('online', refresh);
    document.addEventListener('visibilitychange', refresh);
    setInterval(refresh, 5000);
    refresh();
})();
