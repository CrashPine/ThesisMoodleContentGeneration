using Backend.API.Configuration;
using Backend.BLL.Interfaces;
using Backend.BLL.QuestionGenerationModule;
using Backend.BLL.Services;
using Backend.DAL;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Подключаем базу SQLite с явным полным путем
// Поднимаемся на три уровня: bin/Debug/net9.0 -> Backend.API
var projectRoot = Path.Combine(AppContext.BaseDirectory, "..", "..", "..");
var dbPath = Path.Combine(projectRoot, "moodle_tests.db");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

Console.WriteLine("SQLite DB path: " + Path.GetFullPath(dbPath));

// 2. Подключаем AutoMapper
builder.Services.AddAutoMapper(cfg => { }, AppDomain.CurrentDomain.GetAssemblies());

// 3. Подключаем credentials.json
builder.Configuration.AddJsonFile("credentials.json", optional: false, reloadOnChange: true);

// 4. Считываем настройки для QuizOrchestrator
var orchestratorSettings = builder.Configuration
    .GetSection("QuizOrchestrator")
    .Get<QuizOrchestratorSettings>();

// 5. Регистрируем QuizOrchestrator
builder.Services.AddSingleton(new QuizOrchestrator(
    laptopOllamaUrl: orchestratorSettings.Url,
    cloudOllamaUrl: orchestratorSettings.Url,
    apiKey: orchestratorSettings.ApiKey,
    cloudModel: orchestratorSettings.SummaryModel,
    ocrModel: orchestratorSettings.OcrModel));

// 6. Регистрируем сервис
builder.Services.AddScoped<ITestService, TestService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 7. Автоматическое применение миграций при старте
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate(); // создаёт таблицы, если их нет
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();