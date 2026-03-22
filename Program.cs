using FunctionalitiesWebAPI.DTO;
using FunctionalitiesWebAPI.Helper;
using FunctionalitiesWebAPI.Middlewares;
using FunctionalitiesWebAPI.Processing;
using FunctionalitiesWebAPI.Services;
using FunctionalitiesWebAPI.Services.Interfaces;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);


// -----------------------------
// Add Services
// -----------------------------

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

// Hangfire
builder.Services.AddHangfire(x => x.UseMemoryStorage());
builder.Services.AddHangfireServer();

/*
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme =
    JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme =
    JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters =
    new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],

        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
});
*/

// Increase upload limit
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 500 * 1024 * 1024; // 500 MB
});

// MERGED Kestrel Configuration (IMPORTANT FIX)
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 500 * 1024 * 1024;

    options.ListenAnyIP(5284); // HTTP

    options.ListenAnyIP(7219, listenOptions =>
    {
        listenOptions.UseHttps();
    });
});

// Video services
builder.Services.AddScoped<IAudioVideoSyncService, AudioVideoSyncService>();
builder.Services.AddScoped<IFFmpegProcessor, FFmpegProcessor>();
builder.Services.AddScoped<IVideoService, VideoService>();
builder.Services.AddScoped<IVideoGenerator, VideoGenerator>();

builder.Services.AddSingleton<VideoJobStore>();
builder.Services.AddSingleton<IVideoQueue, VideoQueue>();
builder.Services.AddSingleton<VideoProcessingService>();
builder.Services.AddHostedService<VideoProcessingWorker>();

builder.Services.AddScoped<ITestUserService, TestUserService>();

// PDF service
builder.Services.AddSingleton<PdfService>();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "FunctionalitiesWebAPI",
        Version = "v1"
    });
});


// -----------------------------
// Build App
// -----------------------------

var app = builder.Build();


// -----------------------------
// Middleware Pipeline
// -----------------------------

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();

    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "FunctionalitiesWebAPI v1");
    });
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseCors("AllowAll");

app.UseRouting();

// Keep global exception AFTER routing but BEFORE endpoints
//app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();