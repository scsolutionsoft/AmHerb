(() => {
    const form = document.getElementById('register-wizard'), steps = [...form.querySelectorAll('[data-register-step]')];
    const back = form.querySelector('[data-register-back]'), next = form.querySelector('[data-register-next]'), submit = form.querySelector('#register-submit');
    const password = form.querySelector('#Password'), confirm = form.querySelector('#ConfirmPassword');
    let step = 0;
    form.noValidate = true;
    function render(focus = true) {
        steps.forEach((s, i) => s.hidden = i !== step);
        document.querySelectorAll('[data-register-indicator]').forEach((s, i) => { s.className = 'badge ' + (i === step ? 'text-bg-success' : 'text-bg-light'); if (i === step) s.setAttribute('aria-current', 'step'); else s.removeAttribute('aria-current'); });
        back.hidden = step === 0; next.hidden = step === 2; submit.hidden = step !== 2;
        form.querySelector('[data-review-name]').textContent = form.querySelector('#Name').value;
        form.querySelector('[data-review-email]').textContent = form.querySelector('#Email').value;
        form.querySelector('#register-review').hidden = false;
        if (focus) steps[step].querySelector('h2').focus();
    }
    function valid(index) {
        confirm.setCustomValidity(password.value === confirm.value ? '' : 'รหัสผ่านทั้งสองช่องต้องตรงกัน');
        for (const input of steps[index].querySelectorAll('input')) {
            if (!input.checkValidity()) { step = index; render(); input.reportValidity(); return false; }
        }
        return true;
    }
    next.addEventListener('click', () => { if (valid(step)) { step++; render(); } });
    back.addEventListener('click', () => { step--; render(); });
    confirm.addEventListener('input', () => confirm.setCustomValidity(''));
    password.addEventListener('input', () => confirm.setCustomValidity(''));
    form.querySelector('#show-register-password').addEventListener('change', e => { password.type = confirm.type = e.target.checked ? 'text' : 'password'; });
    form.addEventListener('submit', e => {
        if (step < 2) { e.preventDefault(); if (valid(step)) { step++; render(); } return; }
        if (!valid(0) || !valid(1)) { e.preventDefault(); return; }
        submit.disabled = true; submit.textContent = 'กำลังสร้างบัญชี…';
    });
    render(false);
})();
