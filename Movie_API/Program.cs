using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Movie_API.CustomActionFilter;
using Movie_API.Data;
using Movie_API.Repositories;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ===== Logging (Serilog) =====
var logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("Logs/Movie_log.txt", rollingInterval: RollingInterval.Day)
    .MinimumLevel.Information()
    .CreateLogger();
builder.Logging.ClearProviders();
builder.Logging.AddSerilog(logger);

// ===== Services =====
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins", p => p
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod());
});

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDbContext<MovieAuthDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("MovieAuthConnection")));

builder.Services.AddIdentityCore<IdentityUser>()
    .AddRoles<IdentityRole>()
    .AddTokenProvider<DataProtectorTokenProvider<IdentityUser>>("Movie")
    .AddEntityFrameworkStores<MovieAuthDbContext>()
    .AddDefaultTokenProviders();

builder.Services.Configure<IdentityOptions>(o =>
{
    o.Password.RequireDigit = false;
    o.Password.RequireLowercase = false;
    o.Password.RequireNonAlphanumeric = false;
    o.Password.RequireUppercase = false;
    o.Password.RequiredLength = 6;
    o.Password.RequiredUniqueChars = 1;
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.Zero,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

// ===== Upload limits 500MB =====
const long FiveHundredMB = 500L * 1024 * 1024;
builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = FiveHundredMB;
});
builder.WebHost.ConfigureKestrel(o =>
{
    o.Limits.MaxRequestBodySize = FiveHundredMB;
});

// ===== DI =====
builder.Services.AddScoped<IMovieRepository, SQLMovieRepository>();
builder.Services.AddScoped<IActorRepository, SQLActorRepository>();
builder.Services.AddScoped<IStudioRepository, SQLStudioRepository>();
builder.Services.AddScoped<IMovie_ActorRepository, SQLMovie_ActorRepository>();
builder.Services.AddScoped<IMovieActorRepository, SQLMovieActorRepository>();
builder.Services.AddScoped<ITokenRepository, TokenRepository>();
builder.Services.AddScoped<IImageRepository, LocalImageRepository>();

builder.Services.AddScoped<ValidateStudioExistsAttribute>();
builder.Services.AddScoped<ValidateActorCanDeleteAttribute>();
builder.Services.AddScoped<ValidateMovieActorNotExistsAttribute>();
builder.Services.AddScoped<Movie_API.Services.IThumbnailService, Movie_API.Services.ThumbnailService>();

// ===== Swagger =====
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // ✅ THÊM DÒNG NÀY: để Swagger hiểu input là file khi dùng IFormFile
    options.MapType<IFormFile>(() => new OpenApiSchema
    {
        Type = "string",
        Format = "binary"
    });

    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Movie API", Version = "v1" });
    options.AddServer(new OpenApiServer { Url = "http://localhost:5099" });
    options.AddServer(new OpenApiServer { Url = "https://localhost:7138" });

    options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = JwtBearerDefaults.AuthenticationScheme
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = JwtBearerDefaults.AuthenticationScheme
                },
                Scheme = "Oauth2",
                Name   = JwtBearerDefaults.AuthenticationScheme,
                In     = ParameterLocation.Header
            },
            new List<string>()
        }
    });
});

var app = builder.Build();

// ===== Pipeline =====
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowAllOrigins");

app.UseAuthentication();
app.UseAuthorization();

// 1) wwwroot (nếu có)
app.UseStaticFiles();

// 2) Serve /uploads (ContentRoot/uploads) cho GET + HEAD + hỗ trợ Range
app.MapMethods("/uploads/{*path}", new[] { "GET", "HEAD" }, (HttpContext ctx, string? path) =>
{
    var full = Path.Combine(builder.Environment.ContentRootPath, "uploads", path ?? string.Empty);
    if (!System.IO.File.Exists(full))
        return Results.NotFound();

    var provider = new FileExtensionContentTypeProvider();
    if (!provider.TryGetContentType(full, out var contentType))
        contentType = "application/octet-stream";

    return Results.File(full, contentType, enableRangeProcessing: true);
});

// 3) (Tuỳ chọn) map wwwroot/Images -> /Images với content-type & cache
var webRoot = builder.Environment.WebRootPath
              ?? Path.Combine(builder.Environment.ContentRootPath, "wwwroot");
var imagesDir = Path.Combine(webRoot, "Images");
Directory.CreateDirectory(imagesDir);

var typeProvider = new FileExtensionContentTypeProvider();
typeProvider.Mappings[".mp4"] = "video/mp4";
typeProvider.Mappings[".mov"] = "video/quicktime";
typeProvider.Mappings[".webm"] = "video/webm";

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(imagesDir),
    RequestPath = "/Images",
    ContentTypeProvider = typeProvider,
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers["Cache-Control"] = "public,max-age=604800";
        ctx.Context.Response.Headers["Accept-Ranges"] = "bytes";
    }
});

// (nếu cần debug uploads, bạn có thể mở lại endpoint này)
// app.MapGet("/api/debug/uploads", () =>
// {
//     var dir = Path.Combine(builder.Environment.ContentRootPath, "uploads");
//     Directory.CreateDirectory(dir);
//     var files = Directory.GetFiles(dir).Select(Path.GetFileName).ToList();
//     return Results.Ok(new { dir, exists = Directory.Exists(dir), files });
// });

app.MapControllers();
app.Run();
