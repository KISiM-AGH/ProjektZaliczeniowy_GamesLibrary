using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using GamesLibraryAPI;
using GamesLibraryAPI.Entities;
using GamesLibraryAPI.Middleware;
using GamesLibraryAPI.Services.Account;
using GamesLibraryAPI.Services.Games;
using GamesLibraryAPI.Validators;
using GamesLibraryShared;
using GamesLibraryShared.Games;
using GamesLibraryShared.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

IConfiguration configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

var allowSpecificOrigin = "ReactClient";

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: allowSpecificOrigin,
        builder =>
        {
            builder.WithOrigins("http://localhost:3000").AllowAnyMethod().AllowAnyHeader();
        });
});

// Add services to the container.

builder.Services.AddHttpContextAccessor();
builder.Services.AddDbContext<AppDbContext>();

//builder.Services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
//Test

var jwtSettings = new JwtSettings();
configuration.GetSection("JwtSettings").Bind(jwtSettings);
builder.Services.AddSingleton(jwtSettings);

builder.Services.AddSingleton((jwtSettings));

// Getting token from header and setting in context

builder.Services.AddAuthentication(option =>
{
    option.DefaultAuthenticateScheme = "Bearer";
    option.DefaultScheme = "Bearer";
    option.DefaultChallengeScheme = "Bearer";
}).AddJwtBearer(cfg =>
{
    cfg.RequireHttpsMetadata = false;
    cfg.SaveToken = true;
    cfg.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        ValidateIssuer = false,
        ValidateAudience = false,
        ClockSkew = TimeSpan.Zero,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret))
    };
});

// Add custom validation errors response

builder.Services.AddMvc().ConfigureApiBehaviorOptions(options =>
{
    options.InvalidModelStateResponseFactory = c =>
    {
        var errors = string.Join(" | ", c.ModelState.Values.Where(v => v.Errors.Count > 0)
            .SelectMany(v => v.Errors)
            .Select(v => v.ErrorMessage));

        return new BadRequestObjectResult(new BaseResponse
        {
            Error = true,
            Message = errors
        });
    };
});

builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddControllers().AddFluentValidation();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<ErrorHandlingMiddleware>();

builder.Services.AddScoped<IValidator<UserRegisterRequest>, RegisterUserValidator>();
builder.Services.AddScoped<IValidator<UserLoginRequest>, LoginUserValidator>();
builder.Services.AddScoped<IValidator<GameUserRequest>, AddGameToUserValidator>();
builder.Services.AddScoped<IValidator<GameAdminRequest>, AddGameAdminValidator>();

builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddScoped<IUserContextService, UserContextService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(allowSpecificOrigin);

app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseAuthentication();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();