(function () {
	'use strict';

	console.log('authors-create.js loaded');

	const debounce = (fn, ms = 300) => { let t; return (...args) => { clearTimeout(t); t = setTimeout(() => fn(...args), ms); }; };

	function $(sel, ctx = document) { return ctx.querySelector(sel); }

	const nameInput = $('#Author_Name') || $('#Name') || document.querySelector('[name="Author.Name"]') || document.querySelector('[name="Name"]');
	const surnameInput = $('#Author_Surname') || $('#Surname') || document.querySelector('[name="Author.Surname"]') || document.querySelector('[name="Surname"]');
	const form = surnameInput ? surnameInput.closest('form') : document.querySelector('form');
	const submitBtn = form ? (form.querySelector('button[type="submit"], input[type="submit"]')) : null;

	if (!surnameInput || !form) return;

	const warningEl = document.createElement('div');
	warningEl.className = 'field-validation-valid text-warning';
	warningEl.style.marginTop = '0.25rem';
	surnameInput.insertAdjacentElement('afterend', warningEl);

	let fullMatch = false;
	const baseUrl = '/Melodies/Create'; // uses existing page handlers

	async function checkSurname(value) {
		if (!value || value.trim().length < 2) { warningEl.textContent = ''; fullMatch = false; updateSubmit(); return; }
		const url = `${baseUrl}?handler=CheckAuthor&author=${encodeURIComponent(value)}`;
		try {
			const r = await fetch(url);
			if (!r.ok) throw new Error();
			const json = await r.json();
			const exists = (typeof json === 'object') ? (json.exists ?? json) : !!json;
			if (exists) {
				warningEl.textContent = 'Автор з таким прізвищем вже існує.';
				warningEl.style.color = '#b45f06';
			} else {
				warningEl.textContent = '';
			}
		} catch (e) {
			console.warn('Check author failed', e);
		}
		await checkFullMatch();
	}

	async function checkFullMatch() {
		const name = (nameInput && nameInput.value) ? nameInput.value.trim() : '';
		const surname = (surnameInput && surnameInput.value) ? surnameInput.value.trim() : '';
		fullMatch = false;
		if (!surname) { updateSubmit(); return; }
		const url = `${baseUrl}?handler=CheckAuthorFull&name=${encodeURIComponent(name)}&surname=${encodeURIComponent(surname)}`;
		try {
			const r = await fetch(url);
			if (!r.ok) throw new Error();
			const json = await r.json();
			const exists = (typeof json === 'object') ? (json.exists ?? json) : !!json;
			if (exists) {
				warningEl.textContent = 'Автор з таким імʼям і прізвищем вже існує. Збереження заборонено.';
				warningEl.style.color = '#a00';
				fullMatch = true;
			} else {
				if (warningEl.textContent.startsWith('Автор з таким прізвищем')) {
					// keep surname warning
				} else {
					warningEl.textContent = '';
				}
			}
		} catch (e) {
			console.warn('Check full author failed', e);
		}
		updateSubmit();
	}

	function updateSubmit() {
		if (submitBtn) {
			submitBtn.disabled = !!fullMatch;
			if (fullMatch) {
				submitBtn.setAttribute('aria-disabled', 'true');
			} else {
				submitBtn.removeAttribute('aria-disabled');
			}
		}
	}

	const debouncedCheck = debounce(() => checkSurname(surnameInput.value), 350);

	surnameInput.addEventListener('input', debouncedCheck);
	if (nameInput) nameInput.addEventListener('input', debounce(checkFullMatch, 350));

	form.addEventListener('submit', function (e) {
		if (fullMatch) {
			e.preventDefault();
			surnameInput.focus();
			alert('Автор з таким імʼям і прізвищем вже існує. Збереження заборонено.');
		}
	});

})();