// lang-switcher.js

//встановити cookie для мови		
function setCultureCookie(culture) {
	var cookieName = '.AspNetCore.Culture';
	var cookieValue = 'c=' + culture + '|uic=' + culture;
	var expires = new Date();
	expires.setFullYear(expires.getFullYear() + 1);
	document.cookie = cookieName + '=' + cookieValue + '; expires=' + expires.toUTCString() + '; path=/';
}

// застосувати кольори на основі мови
function applyThemeForCulture(culture) {
	try {
		if (culture && culture.toLowerCase() === 'en') {
			var root = document.documentElement;
			var englishColor = getComputedStyle(root).getPropertyValue('--brand-english').trim() || '#2a5d84';
			root.style.setProperty('--brand-maincolor', englishColor);
			root.style.setProperty('--brand-maincolor-light', '#5b7fa0');
		}
	} catch (e) {
		if (console && console.warn) console.warn('applyLangColors failed', e);
	}
}

// обробник події зміни мови
document.addEventListener('DOMContentLoaded', function () {
	var inputs = document.querySelectorAll('input[name="culture-switch"]');
	if (inputs && inputs.length) {
		inputs.forEach(function (input) {
			input.addEventListener('change', function () {
				var culture = this.value;
				setCultureCookie(culture);

				// Для сторінок Privacy/Privacy.en, 
				var path = window.location.pathname || '';
				var search = window.location.search || '';
				if (path.startsWith('/Privacy')) {
					if (culture === 'en') {
						if (!path.startsWith('/Privacy.en')) {
							window.location.href = '/Privacy.en' + search;
							return;
						}						
						window.location.reload();
						return;
					} else {						
						if (path.startsWith('/Privacy.en')) {
							window.location.href = '/Privacy' + search;
							return;
						}
						window.location.reload();
						return;
					}
				}

				// default: reload so server middleware reads cookie
				window.location.reload();
			});
		});
	}

	var culture = window.__currentCulture || navigator.language || navigator.userLanguage || 'uk';
	if (typeof culture === 'string') {		
		culture = culture.split('-')[0];
	}
	applyThemeForCulture(culture);
});
