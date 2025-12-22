# Локалізація Melodies25 - Структура ресурсів

Ця папка містить ресурсні файли для двомовної підтримки проекту Melodies25 (українська та англійська мови).

## Структура папок

Структура Resources/Pages повністю повторює структуру папки Pages проекту:

```
Resources/
??? Pages/
?   ??? Account/       # Ресурси для сторінок акаунту
?   ?   ??? Account.uk.resx        # Українські ресурси
?   ?   ??? Account.en.resx        # Англійські ресурси
?   ?
?   ??? Admin/   # Ресурси для адмінських сторінок
?   ?   ??? Admin.uk.resx          # Українські ресурси
?   ?   ??? Admin.en.resx       # Англійські ресурси
? ?
?   ??? Authors/           # Ресурси для сторінок авторів
?   ?   ??? Authors.uk.resx        # Українські ресурси
?   ?   ??? Authors.en.resx        # Англійські ресурси
?   ?
?   ??? Countries/ # Ресурси для сторінок країн
?   ?   ??? Countries.uk.resx      # Українські ресурси
?   ?   ??? Countries.en.resx      # Англійські ресурси
?   ?
?   ??? Melodies/            # Ресурси для сторінок мелодій
?   ?   ??? Melodies.uk.resx       # Українські ресурси
?   ?   ??? Melodies.en.resx       # Англійські ресурси
?   ?
?   ??? Shared/         # Ресурси для спільних компонентів
?   ?   ??? Shared.uk.resx     # Українські ресурси
?   ?   ??? Shared.en.resx     # Англійські ресурси
?   ?
?   ??? Pages.uk.resx          # Головні ресурси (українська)
?   ??? Pages.en.resx # Головні ресурси (англійська)
?
??? SharedResource.uk.resx          # Існуючі спільні ресурси
```

## Опис ресурсних файлів

### Pages/Account/ - Акаунти користувачів
- Логін, реєстрація, зміна пароля
- Профіль користувача, управління акаунтом
- Повідомлення про доступ та безпеку

### Pages/Admin/ - Адміністративні функції  
- Панель адміністратора
- Управління файлами та системними налаштуваннями
- Кольорові схеми, нотація, MIDI

### Pages/Authors/ - Автори мелодій
- Список, додавання, редагування авторів
- Деталі автора (ім'я, прізвище, біографія, дати)
- Країна походження, кількість мелодій

### Pages/Countries/ - Країни
- Список, додавання, редагування країн
- Назва, код, столиця, населення
- Кількість авторів та мелодій з країни

### Pages/Melodies/ - Мелодії
- Список, пошук, додавання мелодій
- Деталі мелодії (назва, автор, жанр, тональність)
- Відтворення, завантаження, ноти
- Пошук за нотами та схожими мелодіями

### Pages/Shared/ - Спільні компоненти
- Навігація, меню
- Повідомлення про помилки та успіх  
- Пагінація, форми валідації
- Загальні елементи інтерфейсу

### Pages/Pages.* - Головні сторінки
- Головна сторінка, помилки
- Політика конфіденційності, про сайт
- Загальні заголовки та описи

## Використання в коді

Для використання локалізації в Razor Pages:

```csharp
// В PageModel
public class IndexModel : PageModel
{
    private readonly IStringLocalizer<IndexModel> _localizer;
    
    public IndexModel(IStringLocalizer<IndexModel> localizer)
    {
      _localizer = localizer;
 }
}
```

```html
<!-- У .cshtml файлі -->
@inject IStringLocalizer<MelodiesModel> Localizer
<h1>@Localizer["Melodies"]</h1>
```

## Налаштування в Program.cs

Для підключення локалізації додайте:

```csharp
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { "uk", "en" };
    options.DefaultRequestCulture = new RequestCulture("uk");
    options.SupportedCultures = supportedCultures.Select(c => new CultureInfo(c)).ToList();
    options.SupportedUICultures = supportedCultures.Select(c => new CultureInfo(c)).ToList();
});
```

## Формат ключів ресурсів

Ключі ресурсів використовують англійську мову в PascalCase:
- `AddAuthor` = "Додати автора" (UK) / "Add Author" (EN)
- `SearchMelodies` = "Пошук мелодій" (UK) / "Search Melodies" (EN)
- `AreYouSureDelete` = "Ви впевнені, що хочете видалити?" (UK) / "Are you sure you want to delete?" (EN)

## Додавання нових ресурсів

1. Визначте ключ англійською мовою
2. Додайте переклади в обидва файли (.uk.resx та .en.resx)
3. Використайте ключ в коді через IStringLocalizer
4. Перевірте роботу обох мов

---

Створено для проекту Melodies25  
Підтримувані мови: українська (uk), англійська (en)