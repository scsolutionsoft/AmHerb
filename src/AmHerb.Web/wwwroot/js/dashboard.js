(() => {
    'use strict';
    const host = document.getElementById('sales-chart');
    if (!host) return;
    const days = JSON.parse(host.dataset.days);
    if (!days.length) return;
    const ns = 'http://www.w3.org/2000/svg';
    const create = (name, attributes, parent, text) => {
        const node = document.createElementNS(ns, name);
        Object.entries(attributes || {}).forEach(([key, value]) => node.setAttribute(key, value));
        if (text !== undefined) node.textContent = text;
        parent.appendChild(node);
        return node;
    };
    const svg = create('svg', { viewBox: '0 0 900 300', role: 'img', 'aria-labelledby': 'sales-chart-title sales-chart-description' }, host);
    create('title', { id: 'sales-chart-title' }, svg, 'แนวโน้มยอดขายรายวันและช่วงก่อนหน้า');
    create('desc', { id: 'sales-chart-description' }, svg, 'เลือกวันด้วยแถบเลื่อนใต้กราฟ หรือเลื่อนเมาส์บนกราฟเพื่อดูยอดขาย');
    const left = 74, top = 20, width = 806, height = 226;
    const maximum = Math.max(1, ...days.map(day => Math.max(day.net, day.previous)));
    const x = index => left + index * width / Math.max(1, days.length - 1);
    const y = value => top + height * (1 - value / maximum);
    const number = new Intl.NumberFormat('th-TH', { maximumFractionDigits: 2 });
    for (let i = 0; i <= 4; i++) {
        const value = maximum * i / 4;
        create('line', { x1: left, x2: left + width, y1: y(value), y2: y(value), stroke: '#e9eee4' }, svg);
        create('text', { x: left - 10, y: y(value) + 4, 'text-anchor': 'end' }, svg, number.format(value));
    }
    const points = key => days.map((day, index) => `${x(index)},${y(day[key])}`).join(' ');
    create('polygon', { points: `${left},${y(0)} ${points('net')} ${x(days.length - 1)},${y(0)}`, fill: '#eef4e8' }, svg);
    const previous = create('polyline', { points: points('previous'), fill: 'none', stroke: '#b2be9b', 'stroke-width': 2, 'stroke-dasharray': '6 6', 'stroke-linejoin': 'round' }, svg);
    create('polyline', { points: points('net'), fill: 'none', stroke: '#28724d', 'stroke-width': 3, 'stroke-linejoin': 'round' }, svg);
    const indices = [...new Set([0, Math.floor((days.length - 1) / 2), days.length - 1])];
    indices.forEach(index => create('text', { x: x(index), y: 279, 'text-anchor': index === 0 ? 'start' : index === days.length - 1 ? 'end' : 'middle' }, svg, days[index].day));
    const marker = create('circle', { cx: 0, cy: 0, r: 5, fill: '#28724d', stroke: '#fff', 'stroke-width': 2 }, svg);
    const slider = document.getElementById('chart-day');
    const detail = document.getElementById('chart-detail');
    const compare = document.getElementById('compare-sales');
    const select = index => {
        const day = days[index];
        slider.value = index;
        marker.setAttribute('cx', x(index)); marker.setAttribute('cy', y(day.net));
        detail.textContent = `${day.day} · ยอดขาย ฿${number.format(day.net)}${compare.checked ? ` · วันเทียบเคียงช่วงก่อนหน้า ฿${number.format(day.previous)}` : ''}`;
        slider.setAttribute('aria-valuetext', detail.textContent);
    };
    slider.addEventListener('input', () => select(Number(slider.value)));
    compare.addEventListener('change', () => { previous.style.display = compare.checked ? '' : 'none'; select(Number(slider.value)); });
    svg.addEventListener('pointermove', event => {
        const rectangle = svg.getBoundingClientRect();
        const coordinate = (event.clientX - rectangle.left) * 900 / rectangle.width;
        select(Math.max(0, Math.min(days.length - 1, Math.round((coordinate - left) / width * (days.length - 1)))));
    });
    select(days.length - 1);
})();
