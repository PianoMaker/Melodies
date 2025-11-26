document.addEventListener('DOMContentLoaded', () => {
    const form = document.getElementById('langform');
    const sw = document.getElementById('langSwitch');
    const hidden = document.getElementById('langHidden');
    if (!form || !sw || !hidden) return;

    sw.addEventListener('change', () => {
        hidden.value = sw.checked ? 'en' : 'uk';
        form.submit();
    });
});