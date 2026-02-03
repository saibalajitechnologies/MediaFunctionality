using FunctionalitiesWebAPI.Helper;
using FunctionalitiesWebAPI.Processing;
using FunctionalitiesWebAPI.Services;
using FunctionalitiesWebAPI.Services.Interfaces;
using Hangfire;
using Hangfire.MemoryStorage; // Required for UseInMemoryStorage
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
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


// Increase upload limit if needed
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 100 * 1024 * 1024; // 100 MB
});

builder.Services.AddScoped<IAudioVideoSyncService, AudioVideoSyncService>();
builder.Services.AddScoped<IFFmpegProcessor, FFmpegProcessor>();

builder.Services.AddScoped<IVideoGenerator, VideoGenerators>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "FunctionalitiesWebAPI",
        Version = "v1"
    });

    // Force Swagger 2.0 spec (not recommended, just for troubleshooting)
    //c.SerializeAsV2 = true;

    //c.EnableAnnotations();
});


builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(5284); // HTTP
    serverOptions.ListenAnyIP(7219, listenOptions =>
    {
        listenOptions.UseHttps();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage(); // Show detailed errors
    //app.UseSwagger();
    //app.UseSwaggerUI();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "FunctionalitiesWebAPI v1");
    });

}


app.UseHttpsRedirection();

app.UseStaticFiles(); //yyyyyyyyServe video from wwwroot/media

app.UseCors("AllowAll");

app.UseRouting();

app.UseAuthorization();

app.MapControllers();

app.Run();
