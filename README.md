1. Общая схема работы и Dependency Injection (Program.cs)

Вся работа разделена на три слоя:

    База данных и сущности (StudentApi.Data/Entities + AppDbContext) — таблицы Groups, StudyPrograms, Students.

    Сервисы и DTO (StudentApi.Data/Services, DTOs) — логика работы с БД через EF Core. Страницы никогда не лезут в AppDbContext напрямую, они общаются с базой только через интерфейсы сервисов (IGroupService, IStudyProgramService).

    Razor Pages (StudentRazorPages/Pages/Groups) — веб-страницы, которые принимают HTTP-запросы, вызывают сервисы и отдают HTML.

Как работает DI (builder.Services.AddScoped<...>):
В Program.cs зарегистрированы:
C#

builder.Services.AddScoped<IGroupService, GroupService>();
builder.Services.AddScoped<IStudyProgramService, StudyProgramService>();

    Когда пользователь открывает, например, /Groups/Create, ASP.NET Core сам создаёт объект класса Create (Create.cshtml.cs).

    Видит, что конструктору нужны IGroupService и IStudyProgramService.

    Смотрит в DI-контейнер, находит GroupService и StudyProgramService, создаёт их (передав им AppDbContext) и подставляет в конструктор.

    Почему AddScoped? Время жизни Scoped означает «один экземпляр на один HTTP-запрос» — это стандарт для сервисов, работающих с DbContext (у DbContext тоже Scoped-время жизни). Если забыть зарегистрировать сервис в Program.cs, приложение упадёт с ошибкой InvalidOperationException (DI не сможет создать модель страницы).

2. Как устроена Razor Page (.cshtml + .cshtml.cs)

Каждая страница состоит из двух связанных файлов:

    .cshtml — шаблон (HTML + C#-вставки Razor), отвечает за внешний вид.

    .cshtml.cs — модель страницы (code-behind, наследник PageModel), отвечает за логику, данные и обработку запросов.

Ключевые директивы:

    @page в первой строке — обязательный маркер. Он говорит ASP.NET Core, что этот файл является самостоятельной страницей со своим URL (например, Pages/Groups/Index.cshtml автоматически получает адрес /Groups или /Groups/Index).

        У частичного представления _GroupRow.cshtml директивы @page нет, потому что это не отдельная страница, а просто кусочек HTML (одна строка таблицы <tr>), который вставляется внутрь Index.cshtml через <partial name="_GroupRow" model="group" />.

    @model StudentRazorPages.Pages.Groups.Index — связывает шаблон с конкретным C#-классом модели (указывает тип свойства Model).

    @model vs @Model:

        @model (с маленькой буквы) — директива сверху файла, задаёт тип данных.

        @Model (с заглавной буквы) — готовый объект этого типа внутри HTML (например, @Model.TotalCountMessage, @Model.Groups).

    ViewData["Title"]: словарь, через который конкретная страница передаёт данные в общий макет Pages/Shared/_Layout.cshtml. В _Layout.cshtml написано <title>@ViewData["Title"] - StudentRazorPages</title> и @RenderBody() (куда подставляется HTML самой страницы).

3. Маршруты, параметры URL и обработчики (OnGetAsync, OnPostAsync, OnPostDeleteAsync)

ASP.NET Core выбирает метод в .cshtml.cs по типу HTTP-запроса:

    При открытии страницы по ссылке или в адресной строке приходит GET → вызывается OnGetAsync().

    При отправке формы с method="post" приходит POST → вызывается OnPostAsync().

Параметр маршрута @page "{id:int}" (Details и Update)

    Фигурные скобки {id:int} означают, что после адреса страницы ожидается числовой параметр (например, /Groups/Details/5).

    :int — это ограничение маршрута (Route Constraint). Если в URL написать /Groups/Details/abc, ASP.NET Core даже не запустит OnGetAsync, а сразу вернёт 404 Not Found.

    Если передано число, например 999, которого нет в базе, GetByIdAsync(999) вернёт null, и наш код if (group is null) return NotFound(); вернёт 404.

Именованный обработчик OnPostDeleteAsync

На странице Index.cshtml таблица показывает список, а кнопка «Удалить» в каждой строке (_GroupRow.cshtml) находится внутри своей мини-формы:
Html

<form method="post" asp-page-handler="Delete" asp-route-id="@Model.Id" ...>

    asp-page-handler="Delete" заставляет ASP.NET Core искать не обычный OnPostAsync, а OnPostDeleteAsync (к имени OnPost + имя handler + Async). В HTML это добавляет к URL ?handler=Delete.

    asp-route-id="@Model.Id" передаёт в параметр int id метода OnPostDeleteAsync(int id) идентификатор удаляемой группы.

    onsubmit="return confirm(...);" — встроенное в браузер окно подтверждения на JS. Если нажать «Отмена», вернётся false и отправка формы отменится, запрос на сервер не уйдёт.

4. Привязка данных (Model Binding): [BindProperty] и поиск

Чтобы данные из HTML-формы автоматически попали в C#-свойства модели страницы, используется Model Binding:

    В Create и Update:
    C#

    [BindProperty]
    public GroupCreateDto Input { get; set; } = new();

    Атрибут [BindProperty] разрешает заполнять свойство Input из тела POST-запроса. Если его забыть, при нажатии «Создать» сервер получит пустой объект Input (Name = "", StudyProgramId = null).

    В поиске на Index:
    C#

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    По умолчанию [BindProperty] работает только для POST-запросов (из соображений безопасности). Так как форма поиска использует method="get" (параметры уходят в адресную строку ?SearchTerm=ИВТ), нужно явно разрешить привязку для GET с помощью SupportsGet = true.

        Кнопка «Сбросить» — это обычная ссылка <a asp-page="./Index">, она открывает /Groups без ?SearchTerm=..., сбрасывая фильтр.

    Как Tag Helpers связывают HTML с C#:
    Когда ты пишешь <input asp-for="Input.Name" />, Razor сам генерирует атрибуты id="Input_Name", name="Input.Name" и подставляет текущее значение из Model.Input.Name (поэтому в Update поля формы уже заполнены текущими данными группы!).

5. Паттерн PRG (Post-Redirect-Get)

Во всех операциях изменения данных (Create, Update, OnPostDeleteAsync) после успеха делается:
C#

return RedirectToPage("./Index"); // или RedirectToPage() для перезагрузки текущей

Зачем это нужно:

    Браузер отправляет POST (например, «Создать группу»).

    Сервер сохраняет данные в БД и отвечает не HTML-страницей, а командой 302 Redirect на адрес /Groups/Index.

    Браузер автоматически делает новый GET-запрос и открывает свежий список.

Что будет, если вместо RedirectToPage вернуть Page()?
Последним запросом в истории браузера останется POST. Если пользователь нажмёт F5 (обновить страницу), браузер повторит POST-запрос и создаст в базе дубликат той же группы. PRG полностью решает эту проблему.
6. Связанная сущность StudyProgram и выпадающий список <select>
Зачем дорабатывали StudentApi.Data?

    В базе у таблицы Groups есть внешний ключ StudyProgramId (nullable int? — группа может быть привязана к направлению, а может и нет).

    В GroupService.GetAllAsync и GetByIdAsync мы добавили .Include(g => g.StudyProgram), чтобы EF Core сделал JOIN с таблицей направлений и вытащил StudyProgramName для отображения в таблице (_GroupRow) и карточке (Details).

    В GroupService добавили GetByIdForUpdateAsync(int id), который возвращает GroupUpdateDto (только Name и StudyProgramId). Это нужно для страницы Update: в OnGetAsync мы загружаем текущие данные группы в Input, чтобы в выпадающем списке сразу выбралось текущее направление группы.

Как работает <select> в Create и Update:

В C# мы готовим коллекцию StudyProgramList:
C#

var programs = await _studyProgramService.GetAllAsync(null);
StudyProgramList = programs.Select(x => new SelectListItem(x.Name, x.Id.ToString()));

    Первый аргумент SelectListItem (x.Name) — Text (то, что видит человек в списке).

    Второй аргумент (x.Id.ToString()) — Value (то, что реально улетит на сервер в свойство Input.StudyProgramId при выборе пункта).

В HTML:
Html

<select asp-for="Input.StudyProgramId" asp-items="Model.StudyProgramList" class="form-select">
    <option value="">-- Выберите направление --</option>
</select>

    Пустой <option value=""> нужен, чтобы по умолчанию не выбиралось первое попавшееся направление из списка (и чтобы можно было оставить поле пустым, т.к. StudyProgramId имеет тип int?).

    Очень частый вопрос на защите: Почему в OnPostAsync при if (!ModelState.IsValid) нужно снова вызывать LoadStudyProgramsAsync()?
    Ответ: При каждом HTTP-запросе создаётся новый экземпляр класса страницы (Create / Update), а StudyProgramList не передаётся обратно в POST-форме (в форме улетает только выбранный StudyProgramId). Если не загрузить список заново перед return Page(), выпадающий список при возврате формы с ошибками окажется пустым!

7. Валидация (клиентская и серверная) и ошибки (ModelState, TempData)

Валидация работает на двух уровнях:

    Правила (Data Annotations) задаются в DTO ([Required], [StringLength]) и над SearchTerm в Index.cshtml.cs.

    Клиентская валидация подключается через:
    Html

    @section Scripts {
        <partial name="Shared/_ValidationScriptsPartial" />
    }

    Эта секция вставляется внизу _Layout.cshtml (@await RenderSectionAsync("Scripts", required: false)), уже после подключения jQuery. Скрипты jquery.validate считывают data-val-* атрибуты, которые сгенерировали Tag Helpers из Data Annotations, и проверяют форму прямо в браузере без отправки на сервер.

    Серверная валидация (if (!ModelState.IsValid)):

        Зачем она нужна, если есть клиентская? Клиентскую легко обойти: отключить JS в браузере, изменить HTML через DevTools или отправить POST напрямую через Postman/curl. Серверу нельзя доверять, поэтому проверка на бэкенде обязательна.

        При наличии ошибок ModelState.IsValid становится false, и мы возвращаем Page(), чтобы показать форму с введёнными данными и сообщениями об ошибках.

Где выводятся ошибки (asp-validation-for vs asp-validation-summary)

    <span asp-validation-for="Input.Name"> — выводит ошибку конкретного поля прямо под ним.

    <div asp-validation-summary="ModelOnly"> — выводит только общие ошибки всей модели (не привязанные к конкретному полю). Например, в Update.cshtml.cs, если группу уже удалили до сохранения (updated == false), мы пишем:
    C#

    ModelState.AddModelError(string.Empty, "Не удалось обновить группу...");

    Первый параметр string.Empty означает «общая ошибка модели». Если бы в asp-validation-summary стояло "All", то ошибки полей дублировались бы и сверху формы, и под каждым полем; значение "ModelOnly" избавляет от дублей.

Зачем [TempData] при удалении?

В OnPostDeleteAsync нельзя записать ошибку в ModelState и сделать RedirectToPage(), потому что ModelState живёт только один запрос и при редиректе стирается.
Атрибут [TempData] сохраняет значение DeleteErrorMessage (обычно в cookie/сессии) ровно до следующего GET-запроса: страница Index откроется после редиректа, прочитает Model.DeleteErrorMessage, покажет красный alert, и после этого сообщение автоматически удалится (при следующем F5 оно уже не появится).
8. Мини-шпаргалка на случай дополнительных вопросов

    Зачем проверять ModelState.IsValid в OnGetAsync поиска?
    На SearchTerm стоит [StringLength(200)]. Если пользователь передаст в URL строку длиннее 200 символов (или введёт в поле при отключённом JS), ModelState.IsValid будет false, мы не будем делать тяжёлый запрос в БД, а вернём пустой список Groups = [] и покажем ошибку под строкой поиска.

    Почему в Create.cshtml.cs return RedirectToPage("./Index"), а в OnPostDeleteAsync просто return RedirectToPage()?
    RedirectToPage("./Index") отправляет со страницы Create на Index. В OnPostDeleteAsync мы и так находимся на Index, поэтому RedirectToPage() без параметров означает «перезагрузить текущую страницу через GET».

    Что изменилось в GroupService по сравнению со стартером?

        В GetAllAsync и GetByIdAsync добавили .Include(g => g.StudyProgram) и маппинг StudyProgramName.

        Добавили GetByIdForUpdateAsync(int id) для предзаполнения формы Update.

        В CreateAsync и UpdateAsync добавили сохранение StudyProgramId.

