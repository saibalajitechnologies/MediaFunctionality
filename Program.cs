using FunctionalitiesWebAPI.Helper;
using FunctionalitiesWebAPI.Middlewares;
using FunctionalitiesWebAPI.Processing;
using FunctionalitiesWebAPI.Services;
using FunctionalitiesWebAPI.Services.Interfaces;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddHangfire(x => x.UseMemoryStorage());
builder.Services.AddHangfireServer();

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 500 * 1024 * 1024;
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 500 * 1024 * 1024;
});

// ✅ Render Port Fix
var port = Environment.GetEnvironmentVariable("PORT") ?? "5284";
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(int.Parse(port));
});

builder.Services.AddScoped<IAudioVideoSyncService, AudioVideoSyncService>();
builder.Services.AddScoped<IFFmpegProcessor, FFmpegProcessor>();
builder.Services.AddScoped<IVideoService, VideoService>();
builder.Services.AddScoped<IVideoGenerator, VideoGenerator>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "FunctionalitiesWebAPI",
        Version = "v1"
    });
});

var app = builder.Build();

// ✅ Enable Swagger on Render (Production also)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "FunctionalitiesWebAPI v1");
});

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseCors("AllowAll");

app.UseRouting();

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();
