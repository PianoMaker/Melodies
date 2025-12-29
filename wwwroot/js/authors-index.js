const authorSearchInputIndex = document.getElementById('authorSearchInputIndex');
const authorRows = Array.from(document.querySelectorAll('#authorsTable tbody tr.author-row'));
const authorSearchInfo = document.getElementById('authorSearchInfo');
const authorSearchCount = document.getElementById('authorSearchCount');

function normalize(s) {
	if (!s) return '';
	// toLower, trim and map Ukrainian '´' to 'ã' so they are treated equal
	return s.toString().trim().toLowerCase().replace(/´/g, 'ã');
}

function fieldContains(fieldValue, q) {
	if (!fieldValue) return false;
	return normalize(fieldValue).indexOf(q) !== -1;
}

function matchesAuthorRow(row, q) {
	if (!q) return true;

	const d = row.dataset || {};

	// Explicitly check each field: Name, Surname, NameEn, SurnameEn
	if (fieldContains(d.name, q)) return true;
	if (fieldContains(d.surname, q)) return true;
	if (fieldContains(d.nameen, q)) return true;
	if (fieldContains(d.surnameen, q)) return true;

	// fallback: include visible cell text
	const visibleText = Array.from(row.querySelectorAll('td')).map(td => td.textContent || '').join(' | ');
	if (fieldContains(visibleText, q)) return true;

	return false;
}

let __authorsIndex_timer = null;
function authorsIndexUpdate() {
	const input = document.getElementById('authorSearchInputIndex');
	if (!input) return;
	const val = normalize(input.value || '');

	if (val.length < 3) {
		authorRows.forEach(r => r.style.display = '');
		if (authorSearchInfo) authorSearchInfo.style.display = 'none';
		return;
	}

	let visible = 0;
	authorRows.forEach(r => {
		if (matchesAuthorRow(r, val)) { r.style.display = ''; visible++; }
		else { r.style.display = 'none'; }
	});

	if (authorSearchCount) authorSearchCount.textContent = String(visible);
	if (authorSearchInfo) authorSearchInfo.style.display = '';
}

if (authorSearchInputIndex) {
	authorSearchInputIndex.addEventListener('input', function () {
		clearTimeout(__authorsIndex_timer);
		__authorsIndex_timer = setTimeout(authorsIndexUpdate, 200);
	});

	authorSearchInputIndex.addEventListener('keydown', function (e) {
		if (e.key === 'Escape') {
			authorSearchInputIndex.value = '';
			authorsIndexUpdate();
		}
	});
}
