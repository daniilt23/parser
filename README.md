# InternetTechLab1 (Лабораторная работа 1)

Консольное приложение на C# (.NET 9), которое:
- по команде пользователя запрашивает данные погоды через OpenWeatherMap API, нормализует их и сохраняет в SQLite;
- выполняет web scraping по введённому URL и сохраняет результат в документоориентированную БД LiteDB.

## Что реализовано
- Меню в консоли с командами из ТЗ.
- Модуль A: OpenWeatherMap API -> SQLite (EF Core).
- Модуль B: scraping URL -> LiteDB (документ целиком).
- Обработка ошибок: некорректный URL, HTTP ошибки, таймауты, пустой HTML, сетевые ошибки.
- Логирование в консоль и файл.
- Конфигурация через `appsettings.json` / `appsettings.local.json`.

## Выбранный API
- OpenWeatherMap Current Weather API
- Документация: https://openweathermap.org/current

### Примеры запросов к API
- `https://api.openweathermap.org/data/2.5/weather?q=Moscow&appid=YOUR_KEY&units=metric&lang=ru`
- `https://api.openweathermap.org/data/2.5/weather?q=Saint%20Petersburg&appid=YOUR_KEY&units=metric&lang=ru`

## Технологии
- .NET 9 (Console)
- EF Core + SQLite
- LiteDB
- HtmlAgilityPack

## Структура репозитория
- `src/` — исходный код приложения
- `docs/sqlite_schema.sql` — SQL-скрипт создания схемы таблицы для модуля API
- `README.md`
- `appsettings.example.json`

## Быстрый запуск
1. Установите .NET SDK 9.
2. Создайте файл локальной конфигурации:
   - `cp appsettings.example.json src/appsettings.local.json`
3. Укажите API ключ OpenWeatherMap в `src/appsettings.local.json`:
   - `OpenWeatherMap.ApiKey`
4. Запустите приложение:
   - `dotnet restore src/parser.csproj`
   - `dotnet run --project src/parser.csproj`

## Команды меню
1. Настроить/показать параметры подключения к БД.
2. Выполнить запрос к OpenWeatherMap.
3. Показать сохранённые API-записи (с фильтром по городу).
4. Выполнить scraping по URL.
5. Показать последние N результатов scraping.
0. Выход.

## Схема данных

### SQLite (`WeatherRecords`)
Поля:
- `Id`
- `City`
- `Country`
- `Temperature`
- `FeelsLike`
- `Humidity`
- `Pressure`
- `WindSpeed`
- `Description`
- `ApiTimestampUtc`
- `SavedAtUtc`
- `Units`
- `RawJson`

SQL-скрипт: `docs/sqlite_schema.sql`.

### LiteDB (коллекция `scrape_results`)
Поля документа:
- `Id`
- `Url`
- `StatusCode`
- `IsSuccess`
- `ErrorMessage`
- `Title`
- `H1`
- `Links[]` (`Text`, `Href`)
- `ScrapedAtUtc`

## Что именно скрапится и по каким правилам
Извлекаются:
- `<title>`: XPath `//title`
- первый `<h1>`: XPath `//h1`
- ссылки (первые N): XPath `//a[@href]`

## Примеры пользовательских сценариев

### Сценарий 1: API
1. Выбрать пункт `2`.
2. Ввести город, например `Moscow`.
3. Получить краткий вывод и raw JSON.
4. Выбрать пункт `3` и увидеть сохранённые записи из SQLite.

### Сценарий 2: Scraping
1. Выбрать пункт `4`.
2. Ввести URL, например `https://google.com`.
3. Увидеть извлечённые поля (`title`, `h1`, ссылки).
4. Выбрать пункт `5` и увидеть последние документы из LiteDB.
