using Backend.API.Configuration;
using Backend.BLL.Interfaces;
using Backend.BLL.ParserModule;
using Backend.BLL.ParserModule.AI;
using Backend.BLL.QuestionGenerationModule;
using Backend.BLL.Services;
using Backend.DAL;
using Microsoft.EntityFrameworkCore;
using OllamaSharp;

var builder = WebApplication.CreateBuilder(args);

var projectRoot = Path.Combine(AppContext.BaseDirectory, "..", "..", "..");
var dbPath = Path.Combine(projectRoot, "moodle_tests.db");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

Console.WriteLine("SQLite DB path: " + Path.GetFullPath(dbPath));

builder.Services.AddAutoMapper(cfg => { }, AppDomain.CurrentDomain.GetAssemblies());

builder.Configuration.AddJsonFile("credentials.json", optional: false, reloadOnChange: true);

var orchestratorSettings = builder.Configuration
    .GetSection("QuizOrchestrator")
    .Get<QuizOrchestratorSettings>();

builder.Services.AddHttpClient("OllamaClient", client =>
{
    client.BaseAddress = new Uri(orchestratorSettings.Url);
    client.Timeout = TimeSpan.FromMinutes(10);
    if (!string.IsNullOrEmpty(orchestratorSettings.ApiKey))
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {orchestratorSettings.ApiKey}");
});

builder.Services.AddSingleton(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient("OllamaClient");
    return new OllamaApiClient(httpClient);
});

builder.Services.AddSingleton<PdfToImageConverter>();

builder.Services.AddSingleton<OllamaVisionOcr>(sp =>
    new OllamaVisionOcr(sp.GetRequiredService<OllamaApiClient>(), orchestratorSettings.OcrModel));

builder.Services.AddSingleton<CloudContentProcessor>(sp =>
    new CloudContentProcessor(sp.GetRequiredService<OllamaApiClient>(), orchestratorSettings.SummaryModel));

builder.Services.AddSingleton<MoodleXmlGenerator>(sp =>
    new MoodleXmlGenerator(sp.GetRequiredService<OllamaApiClient>(), orchestratorSettings.SummaryModel));

builder.Services.AddSingleton<QuizOrchestrator>(sp =>
    new QuizOrchestrator(
        sp.GetRequiredService<PdfToImageConverter>(),
        sp.GetRequiredService<OllamaVisionOcr>(),
        sp.GetRequiredService<CloudContentProcessor>(),
        sp.GetRequiredService<MoodleXmlGenerator>()));

builder.Services.AddScoped<ITestService, TestService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();