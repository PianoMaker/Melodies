//створює мелодію за натисканням клавіш піаніно
//читання нотного рядку відбувається по одній ноті у конструкторі Note(string input) 

document.addEventListener("DOMContentLoaded", function () {

	console.log("createMelody.js starts.");


	
	const audioPlayer = document.getElementById('audioPlayer');			// аудіоплеєр
	const audioSource = document.getElementById('audioSource');			// джерело для аудіофайлу
	//------------------
	// клавіатура
	//------------------
	const pianokeys = document.querySelectorAll('#pianoroll button');		// клавіші фортепіано
	let pianodisplay = document.getElementById("pianodisplay");			// 
	const keysInput_save = document.getElementById("keysInput-save")
	const keysInput_search = document.getElementById("keysInput-search")//прихований input для збереження нотного рядку перед відправкою форми


	//------------------------
	// сторінка Створення файлів
	//------------------------

	const authorSaver = document.getElementById("authorSaver");			//тимчасове збереження автора    
	const titleInput = document.getElementById("titleInput");			//введення назви
	const selectAuthor = document.getElementById("selectAuthor");		//вибір автора
	let inputAuthor = document.getElementById("inputAuthor");			//введення нового автора
	let warningField = document.getElementById("authorWarning");		//поле для попередження можливого дублікату автора
	const copyBtn = document.getElementById("copyBtn");					//кнопка копіювання назви твору з назви файлу
	const submitMelodyBtn = document.getElementById("submitMelodyBtn");	//кнопка "Створити" (зберігає мелодію в БД)   
	const melodyFileInput = document.getElementById('melodyFileInput');	// завантажувач файлів MIDI
	let createAuthorBtn = document.getElementById("createAuthorBtn");	//кнопка додати автора    

	//-------------------
	//Елементи введення нотного тексту
	//--------------------
	const createMIDIButton = document.getElementById('createMIDI');	//кнопка "Зберегти" (зберігає MIDI із введених нот)
	const resetBtn = document.getElementById('resetBtn');			// кнопка "Скинути"        
	const playButton = document.getElementById('melodyPlayBtn');	// кнопка відтворення
	const saver = document.getElementById("saver");					//тимчасове збереження мелодії

	// елементи вибору музичного розміру
	const denominatorInput = document.getElementById('TimeSignatureDenominator');
	const numeratorInput = document.getElementById('TimeSignatureNumerator');

	//кількість введених нот
	const outerNoteBoxCount = document.querySelectorAll('.outerNoteBox').length;

	// елементи повідомлення про готовність MIDI
	const midiIsReady = document.getElementById('midiIsReady');
	const midiIsNotReady = document.getElementById('midiIsNotReady');
	const ifNotesEntered = document.getElementById('ifNotesEntered');

	//------------------------
	//сторінка Пошуку файлів
	//------------------------
	const searchButton = document.getElementById('searchBtn');						//кнопка "зберегти"
	const searchAlgorithmInput = document.getElementById("searchAlgorithmInput");	//вибір алгоритму пошуку

	if (!pianodisplay) {
		console.warn('[createMelody]: pianodisplay element not found – aborting createMelody.js initialization');
		return; // без нотного поля немає сенсу виконувати далі
	}


	//========================
	// Початкове визначення готовності MIDI-файлу
	//========================
	let fileIsReady = outerNoteBoxCount > 0 || (melodyFileInput && melodyFileInput.files.length > 0);
	console.log('[createMelody]: initial fileIsReady =', fileIsReady);

	updateButtons();
	searchAuthor();



	if (selectAuthor) selectAuthor.addEventListener("change", updateButtons);
	if (createAuthorBtn) createAuthorBtn.addEventListener("click", hideSelectBtn);


	//========================
	// Відновлення значень музичного розміру з sessionStorage
	//========================

	if (sessionStorage.getItem("savedDenominator")) {
		denominatorInput.value = sessionStorage.getItem("savedDenominator");
		console.log(`[createMelody]: Restored denominator: ${denominatorInput.value}`);
	}
	if (sessionStorage.getItem("savedNumerator")) {
		numeratorInput.value = sessionStorage.getItem("savedNumerator");
		console.log(`[createMelody]: Restored numerator: ${numeratorInput.value}`);
	}

	if (!fileIsReady) hideSubmitBtn();


	if (saver) {
		const saved = (saver.innerText || '').trim();
		if (saved.length > 0) {
			pianodisplay.value = saved;
		}
	}
	const savedTitle = sessionStorage.getItem("savedTitle");
	const savedAuthorId = sessionStorage.getItem("selectedAuthorId");




	console.log(`[createMelody]: outernotebox count: ${outerNoteBoxCount}`);
	if (outerNoteBoxCount > 0) {
		fileIsReady = true;
		// Use safe helper to avoid null reference
		safeStyleDisplay(midiIsNotReady, 'none');
		safeStyleDisplay(midiIsReady, 'inline');
	}


	if (titleInput && savedTitle) {
		console.log(`[createMelody]: restoring saved title: ${savedTitle}`)
		const savedTitleDiv = document.getElementById("savedTitle");
		if (savedTitleDiv) savedTitleDiv.textContent = `savedtitle = ${savedTitle}`;
		titleInput.value = savedTitle;
	}
	else if (titleInput) {
		console.log(`[createMelody]: saved title is null`);
	}
	if (selectAuthor && savedAuthorId) {
		console.log(`[createMelody]: restoring saved authorId: ${savedAuthorId}`)
		selectAuthor.value = savedAuthorId;
	}
	else if (selectAuthor) {
		console.log(`[createMelody]: saved authorId is null`);
	}

	if (saver) saver.style.display = 'none';

	if (titleInput && selectAuthor && submitMelodyBtn && savedTitle && savedAuthorId) {
		showSubmitBtn();
		console.log(`displaying submit btn, fileIsReady = ${fileIsReady}`);
	}

	// =====================
	// ОБРОБНИКИ
	// ======================

	//------------------------
	// СПІЛЬНІ ДЛЯ СТОРІНОК CREATE І SEARCH
	//------------------------

	//----------------------------------
	//обробник кнопок тривалостей
	//----------------------------------
	let duration = '4';
	const durationbuttons = document.querySelectorAll('.durationbutton');
	const restBtn = document.getElementById('pausebutton');
	const backBtn = document.getElementById('backbtn');
	const dotBtn = document.getElementById('dotbutton');

	durationbuttons.forEach((button, index) => {
		button.addEventListener('click', () => {
			duration = String(2 ** index);
			console.log(`[createMelody]: duration: ${duration}`);
		});
	});

	// обробник нот з крапками
	if (dotBtn) {
		dotBtn.addEventListener('click', () => {
			dotBtn.classList.toggle('highlight');
			// no immediate change to textarea, but schedule render to keep UI in sync
			if (window.__scheduleLiveNotationRender) window.__scheduleLiveNotationRender();
		});
	}


	// Допоміжна: чи активна крапка зараз
	function isDottedActive() {
		return document.getElementById('dotbutton')?.classList.contains('highlight') || false;
	}

	//----------------------------------
	// обробники клавіш фортепіано
	//----------------------------------
	pianokeys.forEach(button => {
		button.addEventListener('click', function () {
			const key = this.getAttribute('data-key');
			console.log(`[piano]: key pressed: ${key}`);
			// програвання звуку 
			playNoteFromKey(key);

			// Додаємо крапку до тривалості, якщо активна
			const dotSuffix = isDottedActive() ? '.' : '';
			pianodisplay.value += `${key}${duration}${dotSuffix}_`;
			if (createMIDIButton) createMIDIButton.style.background = "lightgreen";
			if (playButton) {
				playButton.style.background = "lightgray";
				const playIcon = document.querySelector('.fas.fa-play');
				if (playIcon) playIcon.style.color = "gray";
			}
			if (window.__scheduleLiveNotationRender) window.__scheduleLiveNotationRender();
			// оновлення повідомлення
			ifNotesEntered.innerText = "для створення MIDI-файлу натисніть 'зберегти'";
		});

	});

	//----------------------------------
	// обробник зміни музичного розміру
	//----------------------------------
	if (denominatorInput && numeratorInput) {
		denominatorInput.addEventListener('change', function () {
			sessionStorage.setItem("savedDenominator", denominatorInput.value);
			console.log(`[createMelody]: Saved denominator: ${denominatorInput.value}`);
			if (window.__scheduleLiveNotationRender) {
				window.__scheduleLiveNotationRender();
				console.log('[createMelody]: Scheduled live notation render after denominator change');
			}
		});

		numeratorInput.addEventListener('change', function () {
			sessionStorage.setItem("savedNumerator", numeratorInput.value);
			console.log(`[createMelody]: Saved numerator: ${numeratorInput.value}`);
			if (window.__scheduleLiveNotationRender) {
				window.__scheduleLiveNotationRender();
				console.log('[createMelody]: Scheduled live notation render after numerator change');
			}
		});

	}
	else {
		console.warn('[createMelody]: denominatorInput or numeratorInput not found');
	}

	//----------------------------------
	// обробник паузи
	//----------------------------------
	if (restBtn) {
		restBtn.addEventListener('click', function () {
			const dotSuffix = isDottedActive() ? '.' : '';
			pianodisplay.value += `r${duration}${dotSuffix}_`;
			if (createMIDIButton) createMIDIButton.style.background = "lightgreen";
			if (playButton) {
				playButton.style.background = "lightgray";
				const playIcon = document.querySelector('.fas.fa-play');
				if (playIcon) playIcon.style.color = "gray";
			}
			if (window.__scheduleLiveNotationRender) window.__scheduleLiveNotationRender();
		})
	}
	else {
		console.warn('restBtn not found');
	}

	//----------------------------------
	// обробник клавіші повернення
	//----------------------------------

	if (backBtn) {
		backBtn.addEventListener('click', function () {
			try {
				let i = 0.0;
				if (!pianodisplay || !pianodisplay.value) return;

				while (pianodisplay.value.length > 0 && i < 4) {
					const lastChar = pianodisplay.value.charAt(pianodisplay.value.length - 1);
					console.log(`slice: val=${pianodisplay.value} lastChar=${lastChar} i=${i}`)
					if (lastChar !== '_' && i > 0) {
						pianodisplay.value = pianodisplay.value.slice(0, -1)
						console.log(`slice: val=${pianodisplay.value} i=${i}`)
						i++;
					}
					else if (lastChar === '_' && i === 0) {
						pianodisplay.value = pianodisplay.value.slice(0, -1)
						console.log(`slice: val=${pianodisplay.value} i=${i}`)
						i++;
					}
					else {
						console.log(`slice: val=${pianodisplay.value} i=${i} break`);
						break;
					}
				}
				if (window.__scheduleLiveNotationRender) window.__scheduleLiveNotationRender();
			}
			catch (e) {
				console.warn(`imposible to slice notes: ${e}`);
			}
		})
		//перемалювати екран
	}
	else console.warn("no backBtn found");



	//----------------------------------
	//Обробник кнопки "Відтворення"
	//----------------------------------
	if (playButton) {
		playButton.addEventListener('click', function (e) {

			e.preventDefault();
			console.log("Play button clicked, playing pianodisplay");
			playPianodisplay();

			//const previewMp3path = document.getElementById('previewMp3path')
			//if (!previewMp3path) return;
			//var filepath = previewMp3path.textContent.trim();
			//var audioPlayer = document.getElementById('audioPlayer');
			//const audioSource = document.getElementById('audioSource');
			//if (!audioPlayer || !audioSource) return;
			//audioSource.src = filepath;
			//console.log(`Play.js play preview from ${audioSource.src}`);
			//audioPlayer.load();
			//audioPlayer.play().catch(err => {
			//	console.error("Помилка при програванні:", err);
			//});
		});
	}
	else console.warn("no playBtn found");
	//------------------------------
	//Обробник кнопки "Зберегти" (Create) або "попередній перегляд" (Search)
	//------------------------------
	if (createMIDIButton && titleInput && selectAuthor && submitMelodyBtn) {
		// Create page behavior
		createMIDIButton.addEventListener('click', function (event) {
			event.preventDefault();
			var unique = checkIfunique();
			if (unique) {
				if (keysInput_save) keysInput_save.value = pianodisplay.value
				sessionStorage.setItem("savedTitle", titleInput.value);
				sessionStorage.setItem("selectedAuthorId", selectAuthor.value);
				console.log("Відправка форми з Keys:", keysInput_save ? keysInput_save.value : '(no element)');
				console.log(`Збереження Title: ${titleInput.value}, selectedAuthorId: ${selectAuthor.value}`);
				// Викликає OnPostMelody()
				const melodyForm = document.getElementById('melodyForm');
				if (melodyForm) melodyForm.submit();
				midiIsReady.style.display = "inline";
				midiIsNotReady.style.display = "none";
				fileIsReady = true;
				sessionStorage.setItem("fileIsReady", "true");
			}
			else alert("Мелодія даного автора з такою назвою вже існує");

		});
	} else if (createMIDIButton) {
		// Fallback для сторінок без title/author (наприклад, Search)
		createMIDIButton.addEventListener('click', function (event) {
			// Не відміняємо submit, просто встановлюємо Keys перед відправкою
			if (keysInput_save) keysInput_save.value = pianodisplay.value;
			console.log("Preview submit with Keys:", keysInput_save ? keysInput_save.value : '(no element)');
		});
	}

	// ==========================
	// Обробник кнопки "Скинути" - очищає нотний рядок та скидає всі налаштування
	// ==========================
	if (resetBtn) {
		resetBtn.addEventListener('click', function () {
			// Очищення нотного рядка
			if (pianodisplay) pianodisplay.value = '';

			// Скидання музичного розміру до значень за замовчуванням
			denominatorInput.value = '4';
			numeratorInput.value = '4';

			// Приховати повідомлення про готовність MIDI
			midiIsReady.style.display = "none";
			midiIsNotReady.style.display = "inline";

			// Скидання стилів кнопок
			if (createMIDIButton) createMIDIButton.style.background = "";
			if (playButton) playButton.style.background = "";

			// Очистка тимчасового збереження
			saver.innerHTML = '';
			authorSaver.innerHTML = '';

			// Приховати кнопку "Додати мелодію"
			submitMelodyBtn.style.display = 'none';

			// Скидання значень у sessionStorage
			sessionStorage.removeItem("savedTitle");
			sessionStorage.removeItem("selectedAuthorId");
			sessionStorage.removeItem("savedDenominator");
			sessionStorage.removeItem("savedNumerator");

			console.log("Налаштування скинуто до значень за замовчуванням.");
		});
	}
	else console.warn("no reset button found");

	//-------------------------
	//ДЛЯ СТОРІНКИ CREATE
	//-------------------------

	//----------------------------------
	//обробник завантажувача файлів (присутній лише на сторінці Create)
	//----------------------------------
	if (melodyFileInput) {
		melodyFileInput.addEventListener('change', function () {
			console.log("melodyFileInput change event triggered");
			const fileInput = this;
			if (fileInput.files.length > 0) {
				fileIsReady = true;
				const copyBtnLocal = document.getElementById('copyBtn');
				if (copyBtnLocal) copyBtnLocal.style.display = 'inline-block';
				// 
				if (midiIsReady) midiIsReady.style.display = "inline";
				if (midiIsNotReady) midiIsNotReady.style.display = "none";
				showSubmitBtn();

			} else {
				if (midiIsReady) midiIsReady.style.display = "none";
				if (midiIsNotReady) midiIsNotReady.style.display = "inline";
			}
		});
	}
	//----------------------------------
	// обробник кнопки "Копіювати з назви"
	//----------------------------------

	if (copyBtn) {

		copyBtn.addEventListener("click", copytitle);
	}

	//----------------------------------
	// обробник поля "Назва" (Title)
	//----------------------------------
	if (titleInput && selectAuthor && submitMelodyBtn) {
		titleInput.addEventListener("input", function () {

			if (titleInput.value.length > 2 && selectAuthor.value) {
				showSubmitBtn();
				console.log(`displaying submit btn, fileIsReady = ${fileIsReady}`);
			}
			else submitMelodyBtn.style.display = 'none';
		});
	}
	//----------------------------------
	// обробник поля введення нового автора
	//----------------------------------

	if (inputAuthor) {
		hideSelectBtn();
		inputAuthor.addEventListener("input", async function () {
			let author = inputAuthor.value.trim();
			console.log(`search author for duplicates ${author}`);
			if (author.length > 4) {
				try {
					let response = await fetch(`/Melodies/Create?handler=CheckAuthor&author=${encodeURIComponent(author)}`);
					let result = await response.json();
					console.log(result.exists);
					if (result.exists) {
						warningField.innerHTML = "❗ Можливо, такий автор вже існує!";
						warningField.style.color = "red";
					} else {
						warningField.innerHTML = "";
					}
				} catch (error) {
					console.error("Помилка перевірки автора:", error);
				}
			} else {
				warningField.innerHTML = "";
				warningField.style.color = "";
			}
		});
		showSubmitBtn();
	}

	// Оновлення видимості кнопок
	function updateButtons() {

		let selectAuthor = document.getElementById("selectAuthor");

		if (!selectAuthor) {
			console.log("selectAuthor error");
			return;
		}

		let createAuthorBtn = document.getElementById("createAuthorBtn");//кнопка додати автора  

		const selectedOption = selectAuthor.options[selectAuthor.selectedIndex];

		if (selectedOption && selectedOption.text === '(невідомо)') {
			console.log("Значення selectOption:", selectedOption.text);
			submitMelodyBtn.style.display = 'none';
			createAuthorBtn.style.display = 'inline-block';

		} else if (selectedOption === undefined) {
			console.log("Значення selectOption is undefined");
			submitMelodyBtn.style.display = 'none';
		}
		else {
			console.log("Значення selectOption:", selectedOption);
			submitMelodyBtn.style.display = 'inline-block';
			createAuthorBtn.style.display = 'none';
			showSubmitBtn();
		}

	}

	//-----------------------------------
	//Обробник поля введення автора, пошук на наявність подібного
	//------------------------------------

	function searchAuthor() {
		console.log("try to search author");
		if (!warningField)
			console.warn("no warning Field!");

		if (inputAuthor) {
			hideSelectBtn();
			inputAuthor.addEventListener("input", async function () {
				let author = inputAuthor.value.trim();
				console.log(`search author for duplicates ${author}`);
				if (author.length > 4) {
					try {
						let response = await fetch(`/Melodies/Create?handler=CheckAuthor&author=${encodeURIComponent(author)}`);
						let result = await response.json();
						console.log(result.exists);
						if (result.exists) {
							warningField.innerHTML = "❗ Можливо, такий автор вже існує!";
							warningField.style.color = "red";
						} else {
							warningField.innerHTML = "";
						}
					} catch (error) {
						console.error("Помилка перевірки автора:", error);
					}
				} else {
					warningField.innerHTML = "";
					warningField.style.color = "";
				}
			});
			showSubmitBtn();

		}
	}


	function hideSelectBtn() {
		console.log("Create button pressed => hide select buttons");

		let selectAuthor = document.getElementById("selectAuthor");
		let createAuthorBtn = document.getElementById("createAuthorBtn");
		let submitMelodyBtn = document.getElementById("submitMelodyBtn");
		let selectAuthorLabel = document.getElementById("selectAuthorLabel");

		if (selectAuthor) selectAuthor.style.display = 'none';
		if (createAuthorBtn) createAuthorBtn.style.display = 'none';
		if (submitMelodyBtn) submitMelodyBtn.style.display = 'none';
		if (selectAuthorLabel) selectAuthorLabel.style.display = 'none';
	}


	//----------------------------
	//перевіряє унікальність назви + композитора
	//---------------------------
	async function checkIfunique() {
		if (!titleInput || !selectAuthor) return false;
		var title = titleInput.value;
		var authorId = selectAuthor.value;

		try {
			const response = await fetch(`/Melodies/Create?handler=CheckFileExists&title=${encodeURIComponent(title)}&authorId=${authorId}`);
			if (!response.ok) {
				console.error("Помилка сервера:", response.statusText);
				return false;
			}
			const exist = await response.json();
			return !exist;
		} catch (error) {
			console.error("Помилка при виконанні запиту:", error);
			return false;
		}
	}

	//----------------------------
	// показує кнопку "Додати мелодію", якщо файл готовий   
	//----------------------------
	function showSubmitBtn() {
		if (fileIsReady) {
			safeStyleDisplay(submitMelodyBtn, 'inline-block');
			safeStyleDisplay(midiIsNotReady, 'none');
			safeStyleDisplay(midiIsReady, 'inline');
		}
	}

	function hideSubmitBtn() {
		// Only hide when submit button exists and file is not ready
		if (!fileIsReady) {
			safeStyleDisplay(submitMelodyBtn, 'none');
		}
	}

	//=========================
	//ДЛЯ СТОРІНКИ SEARCH
	//=========================

	//-----------
	//обрпобник кнопки "Пошук"
	//-----------
	if (searchButton) {
		searchButton.addEventListener('click', function (event) {
			event.preventDefault();
			if (keysInput_search) keysInput_search.value = pianodisplay.value
			console.log("Відправка форми з Keys:", keysInput_search ? keysInput_search.value : '(no element)');

			// синхронізувати вибраний алгоритм пошуку
			const selectedAlg = document.querySelector('input[name="SearchAlgorithm"]:checked');
			if (selectedAlg && searchAlgorithmInput) {
				searchAlgorithmInput.value = selectedAlg.value;
				console.log("Selected algorithm:", selectedAlg.value);
			}

			// Відправка форми
			const searchForm = document.getElementById('notesearchForm');
			if (searchForm) searchForm.submit();

		});
	}



	// ======================
	// КОПІЮВАННЯ НАЗВИ З ФАЙЛУ
	// ======================
	function copytitle(event) {
		event.preventDefault();
		console.log("copytitle function is running");
		const fileInput = document.getElementById("melodyFileInput");
		const titleInput = document.getElementById("titleInput"); // стандартний id для asp-for="Melody.Title"

		if (fileInput && fileInput.files.length > 0 && titleInput) {
			const filename = fileInput.files[0].name;
			let nameWithoutExtension = filename.replace(/\.[^/.]+$/, "");
			let nameCapitalized = nameWithoutExtension.charAt(0).toUpperCase() + nameWithoutExtension.slice(1);
			titleInput.value = nameCapitalized;
			titleInput.dispatchEvent(new Event("input", { bubbles: true }));
		} else {
			alert("Файл не вибрано");
		}
	}

	//-------------
	// ІНІЦІАЛІЗАЦІЯ НОТНОГО РЯДКУ (працює з midirender/setupLiveNotationCreate.js)
	//-------------
	if (typeof window.setupLiveNotationOnCreate === 'function') {
		window.setupLiveNotationOnCreate({
			container: document.getElementById('innerNotesContainer'),
			pianodisplay: document.getElementById('pianodisplay'),
			numeratorInput: document.getElementById('TimeSignatureNumerator'),
			denominatorInput: document.getElementById('TimeSignatureDenominator'),
			pianoKeysContainer: document.getElementById('pianoroll'),
			restBtn: document.getElementById('pausebutton'),
			backBtn: document.getElementById('backbtn'),
			noNotesMsg: document.getElementById('noNotesMsg')
		});
	} else {
		console.warn('[createMelody]: setupLiveNotationOnCreate is not loaded. Ensure /lib/midirender/setupLiveNotationCreate.js is included before createMelody.js.');
	}
});

//----------------------------
// safe helper to set element.style.display if element exists
//----------------------------
function safeStyleDisplay(el, display) {
	if (!el) return;
	try {
		el.style.display = display;
	} catch (e) {
		console.warn('[createMelody] safeStyleDisplay failed', e);
	}
}

//----------------------------
// показує кнопку "Додати мелодію", якщо файл готовий   
//----------------------------
function showSubmitBtn() {
	if (fileIsReady) {
		safeStyleDisplay(submitMelodyBtn, 'inline-block');
		safeStyleDisplay(midiIsNotReady, 'none');
		safeStyleDisplay(midiIsReady, 'inline');
	}
}

function hideSubmitBtn() {
	// Only hide when submit button exists and file is not ready
	if (!fileIsReady) {
		safeStyleDisplay(submitMelodyBtn, 'none');
	}
}



//========================================
// Audio helpers and token→MIDI mapping
//========================================
const AudioContextClass = window.AudioContext || window.webkitAudioContext;
let __audioCtx = null;

function ensureAudioContext() {
	try {
		if (!__audioCtx) __audioCtx = new AudioContextClass();
		console.log('[createMelody] ensureAudioContext -> state:', __audioCtx.state);
		return __audioCtx;
	} catch (e) {
		console.warn('[createMelody] cannot create AudioContext', e);
		return null;
	}
}

function midiToFrequency(midi) {
	return 440 * Math.pow(2, (midi - 69) / 12);
}

// Map piano token (site tokens: c, cis, d, dis, e, f, fis, g, gis, a, b, h and apostrophes or explicit octave like a4)
// Returns MIDI number or null
function pianoTokenToMidi(token) {
	if (!token) return null;

	const apostrophes = (token.match(/['’]+/g) || []).join('').length;
	const baseToken = token.replace(/['’]+/g, '').toLowerCase();

	// site tokens mapping (matches _pianoKeys.cshtml)
	const tokenMap = {
		c: 0, cis: 1, d: 2, dis: 3, e: 4,
		f: 5, fis: 6, g: 7, gis: 8, a: 9,
		b: 10, // 'b' mapped to A# in tone.js mapping used in project
		h: 11
	};

	if (!(baseToken in tokenMap)) return null;

	const semitone = tokenMap[baseToken];
	const BASE_OCTAVE = 4; // UI labels show C1 first row; adjust if you want different default
	const octave = BASE_OCTAVE + apostrophes;
	console.debug('[createMelody][piano] final MIDI number', (octave + 1) * 12 + semitone);
	return (octave + 1) * 12 + semitone;
}

function playTone(freq, duration = 1.0, type = 'sine', volume = 0.82) {
	try {
		const ctx = ensureAudioContext();
		if (!ctx) { console.debug('[createMelody][piano] no AudioContext'); return; }
		const osc = ctx.createOscillator();
		const gain = ctx.createGain();
		osc.type = type;
		osc.frequency.value = freq;
		gain.gain.value = volume;
		osc.connect(gain);
		gain.connect(ctx.destination);
		const now = ctx.currentTime;
		osc.start(now);
		// smooth release
		gain.gain.setValueAtTime(volume, now);
		gain.gain.exponentialRampToValueAtTime(0.0001, now + duration);
		osc.stop(now + duration + 0.02);
		console.debug('[createMelody][piano] playTone', { freq: +freq.toFixed(2), duration, type });
	} catch (e) {
		console.warn('[createMelody][piano] playTone failed', e);
	}
}

// Play all notes from `pianodisplay.value` sequentially.
// Expected token format (as used elsewhere): <note><duration><optionalDot>_
// Examples: "c4_", "cis8._", "r4_" (rest), "a4'4_" (apostrophes or explicit octave supported)
function playPianodisplay() {
	try {
		if (!pianodisplay) {
			console.warn('[createMelody] playPianodisplay: pianodisplay not found');
			return;
		}
		const raw = (pianodisplay.value || '').trim();
		if (!raw) {
			console.debug('[createMelody] playPianodisplay: nothing to play');
			return;
		}

		// Ensure AudioContext ready
		const ctx = ensureAudioContext();
		if (ctx && ctx.state === 'suspended') {
			ctx.resume().catch(e => console.warn('[createMelody] resume audio failed', e));
		}

		// tokens separated by underscore; ignore empty tokens
		const tokens = raw.split('_').map(t => t.trim()).filter(t => t.length > 0);
		if (tokens.length === 0) {
			console.debug('[createMelody] no tokens parsed');
			return;
		}

		const quarterSeconds = 1.0; // quarter note = 1.0s (adjust if you want different tempo)
		let elapsedMs = 0;

		tokens.forEach(token => {
			// parse duration at end: digits possibly followed by dot
			const m = token.match(/(\d+)(\.)?$/);
			if (!m) {
				console.warn('[createMelody] cannot parse token duration:', token);
				return;
			}
			const durationNum = parseInt(m[1], 10);
			const dotted = !!m[2];
			const notePart = token.slice(0, m.index);

			// compute seconds length: durations are expressed like 1(whole),2(half),4(quarter),8(eighth)...
			let seconds = (4 / durationNum) * quarterSeconds;
			if (dotted) seconds *= 1.5;

			// schedule play or rest
			if (notePart && notePart.startsWith('r')) {
				// rest: nothing to play, just advance time
				console.debug('[createMelody] scheduling rest', { token, seconds });
			} else {
				const midi = pianoTokenToMidi(notePart);
				if (midi === null) {
					console.warn('[createMelody] unknown note token:', notePart);
				} else {
					const freq = midiToFrequency(midi);
					// schedule call to playTone at the right elapsed time
					setTimeout(() => {
						playTone(freq, seconds, 'sine', 0.82);
					}, elapsedMs);
					console.debug('[createMelody] scheduled note', { notePart, midi, freq: +freq.toFixed(2), seconds, startInMs: elapsedMs });
				}
			}

			// advance elapsed time by this note/rest duration (ms)
			elapsedMs += Math.round(seconds * 1000);
		});
	} catch (e) {
		console.error('[createMelody] playPianodisplay error', e);
	}
}

// Expose to global for easy calling from console or other scripts
window.playPianodisplay = playPianodisplay;


function playNoteFromKey(key) {
	try {
		console.debug('[createMelody][piano] playNoteFromKey token=', key);
		const midi = pianoTokenToMidi(key);
		if (midi !== null) {
			const freq = midiToFrequency(midi);
			// play exactly 1 second per requirement
			playTone(freq, 2.0, 'sine', 0.82);
			return true;
		}

		// fallback: play audio file if provided on button
		const btn = document.querySelector(`#pianoroll button[data-key="${key}"]`);
		if (btn) {
			const src = btn.getAttribute('data-audiosrc');
			if (src && audioPlayer && audioSource) {
				audioSource.src = src;
				audioPlayer.currentTime = 0;
				audioPlayer.play().catch(err => console.warn('[createMelody] audio fallback play failed', err));
				return true;
			}
		}

		console.warn('[createMelody] cannot resolve token to note:', key);
		return false;
	} catch (e) {
		console.error('[createMelody] playNoteFromKey error', e);
		return false;
	}
}

//
// Handlers: resume AudioContext on first user gesture and play on pointerdown
//
const pianoArea = document.getElementById('pianoroll');
if (pianoArea) {
	pianoArea.addEventListener('pointerdown', function () {
		try {
			const ctx = ensureAudioContext();
			if (ctx && ctx.state === 'suspended') {
				ctx.resume().then(() => console.debug('[createMelody] AudioContext resumed on pointerdown')).catch(e => console.warn(e));
			} else {
				console.debug('[createMelody] AudioContext state:', ctx ? ctx.state : 'none');
			}
		} catch (e) { /* ignore */ }
	}, { once: true, passive: true });
}




