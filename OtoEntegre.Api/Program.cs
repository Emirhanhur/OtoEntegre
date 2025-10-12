using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using OtoEntegre.Api.Data;
using OtoEntegre.Api.Repositories;
using OtoEntegre.Api.Services;
using System.Text.Json.Serialization;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// QuestPDF lisans ayarı (Community)
QuestPDF.Settings.License = LicenseType.Community;

// --- CORS Ayarı
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:3000",
                "https://localhost:3000",
                "http://localhost:5000",
                "https://localhost:5000",
                "http://localhost:80",
                "https://kordteknoloji.com",
                "http://kordteknoloji.com",
                "https://api.kordteknoloji.com",
                "http://api.kordteknoloji.com")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// --- DbContext (PostgreSQL)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// --- Services ve Repositories
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<EntegrasyonService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<DealerService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<OtoEntegre.Api.Services.OrderService>();
builder.Services.AddScoped<TelegramService>();
builder.Services.AddScoped<OtostickerService>();
builder.Services.AddScoped<TrendyolService>();
builder.Services.AddScoped<TedarikService>();
builder.Services.AddScoped<PdfLabelService>();
builder.Services.AddHostedService<SiparisAutoSenderService>();
builder.Services.AddHostedService<OrderSyncBackgroundService>();

// --- Controller ve JSON Options
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.WriteIndented = true;
    });

// --- Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "OtoEntegre API", Version = "v1" });
});

builder.Services.AddSignalR();
var app = builder.Build();

// --- Middleware sırası önemli
app.UseRouting();

// CORS middleware, mutlaka UseRouting sonrası, UseAuthentication/Authorization öncesi olmalı
app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

// Static files (opsiyonel, uyarılar göz ardı edilebilir)
app.UseDefaultFiles();
app.UseStaticFiles();

// Swagger
app.UseSwagger();
app.UseSwaggerUI();

// Controller mapping
app.MapControllers();
app.MapHub<OrderHub>("/orderHub");

app.MapFallbackToFile("/index.html");

// Run app
app.Run();
