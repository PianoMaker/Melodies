//створює мелодію за натисканням клавіш піаніно
//читання нотного рядку відбувається по одній ноті у конструкторі Note(string input) 

document.addEventListener("DOMContentLoaded", function () {

	console.log("createMelody.js starts.");


	//------------------
	// клавіатура
	//------------------
	const pianokeys = document.querySelectorAll('#pianoroll button');		// клавіші фортепіано
	const pianodisplay = document.getElementById("pianodisplay");			// 
	const keysInput_save = document.getElementById("keysInput-save")
	const keysInput_search = document.getElementById("keysInput-search")//прихований input для збереження нотного рядку перед відправкою форми


	//------------------------
	// сторінка Створення файлів
	//------------------------

	const authorSaver = document.getElementById("authorSaver");			//тимчасове збереження автора    
	const titleInput = document.getElementById("titleInput");			//введення назви
	const authorSearch = document.getElementById("authorSearch");		//вибір автора
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



	if (authorSearch) authorSearch.addEventListener("change", updateButtons);
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
	if (authorSearch && savedAuthorId) {
		console.log(`[createMelody]: restoring saved authorId: ${savedAuthorId}`)
		authorSearch.value = savedAuthorId;
	}
	else if (authorSearch) {
		console.log(`[createMelody]: saved authorId is null`);
	}

	if (saver) saver.style.display = 'none';

	if (titleInput && authorSearch && submitMelodyBtn && savedTitle && savedAuthorId) {
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
	if (createMIDIButton && titleInput && authorSearch && submitMelodyBtn) {
		// Create page behavior
		createMIDIButton.addEventListener('click', function (event) {
			event.preventDefault();

			var unique = checkIfunique();
			if (unique) {

				if (keysInput_save) keysInput_save.value = pianodisplay.value
				sessionStorage.setItem("savedTitle", titleInput.value);
				sessionStorage.setItem("selectedAuthorId", authorSearch.value);
				console.log("Відправка форми з Keys:", keysInput_save ? keysInput_save.value : '(no element)');
				console.log(`Збереження Title: ${titleInput.value}, selectedAuthorId: ${authorSearch.value}`);
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
	if (titleInput && authorSearch && submitMelodyBtn) {
		titleInput.addEventListener("input", function () {

			if (titleInput.value.length > 2 && authorSearch.value) {
				updateButtons();
				console.log(`displaying submit btn, fileIsReady = ${fileIsReady}`);
			}
			else updateButtons();
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
		updateButtons();
	}

	// Оновлення видимості кнопок
	function updateButtons() {

		console.log("[updateButtons] called");
		console.debug(`[updateButtons] titleInput.value='${titleInput ? titleInput.value : 'N/A'}', authorSearch.value='${authorSearch ? authorSearch.value : 'N/A'}', fileIsReady=${fileIsReady}`);

		if (titleInput && authorSearch && submitMelodyBtn) {
			if (titleInput.value.length > 2 && authorSearch.value && fileIsReady) {
				showSubmitBtn();
				console.log(`[updateButtons] Showing submit button`);
			} else {
				hideSubmitBtn();
				console.log(`[updateButtons] Hiding submit button`);
			}

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
			updateButtons();

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
		if (!titleInput || !authorSearch) return false;
		var title = titleInput.value;
		var authorId = authorSearch.value;

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

	// ensure the create form includes Keys before submit
	const createForm = document.getElementById('createForm');
	const keysInput_create = document.getElementById('keysInput-create');


	if (createForm) {
		createForm.addEventListener('submit', function (ev) {
			// copy current notation into hidden field so model binder receives Keys
			try {
				if (keysInput_create && pianodisplay) {
					keysInput_create.value = pianodisplay.value || '';
					console.log('[createMelody] copied pianodisplay to keysInput-create:', keysInput_create.value);
				}
			} catch (e) {
				console.warn('[createMelody] failed to copy Keys before submit', e);
			}
		});
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





