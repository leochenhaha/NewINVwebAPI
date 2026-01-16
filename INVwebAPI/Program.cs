using INVwebAPI.Data.Db; // 註解：EINV_WEBContext
using INVwebAPI.Service; // 註解：JwtOptions、TokenService、JwtTokenService、RefreshTokenStore
using Microsoft.AspNetCore.Authentication.JwtBearer; // 註解：JwtBearerDefaults
using Microsoft.EntityFrameworkCore; // 註解：UseSqlServer
using Microsoft.IdentityModel.Tokens; // 註解：TokenValidationParameters
using Microsoft.OpenApi.Models; // 註解：Swagger OpenApi
using System.Text; // 註解：Encoding
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers(); // 註解：啟用 Controller

// DbContext
builder.Services.AddDbContext<EINV_WEBContext>(options => // 註解：註冊 EF Core DbContext
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("EINV_WEB")); // 註解：讀取連線字串
});

// Options: Jwt
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt")); // 註解：綁定 JwtOptions
builder.Services.AddSingleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>();


// DI: Services
builder.Services.AddSingleton<IRefreshTokenStore, InMemoryRefreshTokenStore>(); // 註解：RefreshToken 暫存（單例）
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>(); // 註解：JWT 產生器（每次 request 一份）
builder.Services.AddScoped<ITokenService, TokenService>(); // 註解：Token 商業邏輯（每次 request 一份）
builder.Services.AddScoped<FileService>(); // 註解：你原本的 FileService（如果 UploadLogo 需要）

// 新增 DI：讓服務層可取得 HttpContext 並註冊 CurrentUserAccessor
builder.Services.AddHttpContextAccessor(); // 註解：讓服務層可取得 HttpContext
builder.Services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>(); // 註解：註冊目前登入者資訊存取器

// AuthN/AuthZ: JWT Bearer
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme) // 註解：啟用 JWT Bearer
    .AddJwtBearer(options => // 註解：JWT 驗證設定
    {
        var jwt = builder.Configuration.GetSection("Jwt"); // 註解：讀 Jwt 設定
        var issuer = jwt["Issuer"] ?? ""; // 註解：Issuer
        var audience = jwt["Audience"] ?? ""; // 註解：Audience
        var signingKey = jwt["SigningKey"] ?? ""; // 註解：SigningKey

        options.TokenValidationParameters = new TokenValidationParameters // 註解：驗證規則
        {
            ValidateIssuer = true, // 註解：驗 Issuer
            ValidateAudience = true, // 註解：驗 Audience
            ValidateIssuerSigningKey = true, // 註解：驗簽章
            ValidateLifetime = true, // 註解：驗過期
            ValidIssuer = issuer, // 註解：Issuer 值
            ValidAudience = audience, // 註解：Audience 值
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)), // 註解：驗簽 key
            ClockSkew = TimeSpan.FromSeconds(30) // 註解：允許 30 秒誤差
        };
    });

builder.Services.AddAuthorization(); // 註解：授權

// Swagger
builder.Services.AddEndpointsApiExplorer(); // 註解：Swagger 掃描 endpoints
builder.Services.AddSwaggerGen(options => // 註解：Swagger 設定
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "INV Web API", Version = "v1" }); // 註解：文件資訊

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme // 註解：定義 Bearer
    {
        Name = "Authorization", // 註解：Header 名稱
        Type = SecuritySchemeType.Http, // 註解：HTTP auth
        Scheme = "bearer", // 註解：bearer
        BearerFormat = "JWT", // 註解：JWT
        In = ParameterLocation.Header, // 註解：Header
        Description = "輸入格式: Bearer {你的JWT}" // 註解：提示
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement // 註解：全域套用 Bearer
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Swagger UI
if (app.Environment.IsDevelopment()) // 註解：開發環境才開 Swagger
{
    app.UseSwagger(); // 註解：產生 swagger.json
    app.UseSwaggerUI(); // 註解：Swagger UI
}

app.UseHttpsRedirection(); // 註解：HTTPS

app.UseAuthentication(); // 註解：先驗證
app.UseAuthorization(); // 註解：再授權

app.MapControllers(); // 註解：掛上 controllers
app.Run(); // 註解：啟動
