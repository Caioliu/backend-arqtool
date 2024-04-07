using caiobadev_api_arqtool.ApiArqtool.Domain.Interfaces.Account;
using caiobadev_api_arqtool.Context;
using caiobadev_api_arqtool.Identity;
using caiobadev_api_arqtool.Identity.Services.Interfaces;
using caiobadev_api_arqtool.Identity.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;
using caiobadev_api_arqtool.ViewModels;
using caiobadev_apiconcretapp1.ViewModelsValidator;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddScoped<IValidator<AutenticacaoViewModel>, AutenticacaoViewModelValidator>();
builder.Services.AddScoped<IValidator<UsuarioCadastro>, UsuarioCadastroValidator>();
builder.Services.AddScoped<IValidator<EditUsuarioViewModel>, EditUsuarioViewModelValidator>();

builder.Services.AddControllers().AddJsonOptions(x =>
    x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
builder.Services.AddScoped<IUsuarioLogado, UsuarioLogado>();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => {
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ApiArqTool", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme() {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Header de autoriza��o JWT usando o esquema Bearer.\r\n\r\nInforme 'Bearer'[espa�o] e o seu token.\r\n\r\nExemplo: 'Bearer 12345abcdef' "
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            new string[] {}
        }
    });
});

string connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApiArqtoolContext>(options =>
        options.UseMySql(connectionString,
                ServerVersion.AutoDetect(connectionString)));

builder.Services.AddIdentity<Usuario, Perfil>()
                .AddEntityFrameworkStores<ApiArqtoolContext>()
                .AddDefaultTokenProviders();
builder.Services.AddScoped<IAutenticacaoService, AutenticacaoService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IGeracaoUsuariosPerfisIniciais, GeracaoUsuariosPerfisIniciais>();

builder.Services.AddCors(options => {
    options.AddPolicy("AllowAnyOrigin",
        builder => builder.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader());
});


builder.Services.AddAuthentication(
    JwtBearerDefaults.AuthenticationScheme).
    AddJwtBearer(options =>
     options.TokenValidationParameters = new TokenValidationParameters {
         ValidateIssuer = true,
         ValidateAudience = true,
         ValidateLifetime = true,
         ValidAudience = builder.Configuration["TokenConfiguration:Audience"],
         ValidIssuer = builder.Configuration["TokenConfiguration:Issuer"],
         ValidateIssuerSigningKey = true,
         IssuerSigningKey = new SymmetricSecurityKey(
             Encoding.UTF8.GetBytes(builder.Configuration["Jwt:key"]))
     });

var app = builder.Build();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
GeraUsuariosEPerfis(app);
app.UseRouting();
app.UseCors("AllowAnyOrigin");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

void GeraUsuariosEPerfis(IApplicationBuilder app) {
    using (var serviceScope = app.ApplicationServices.CreateScope()) {
        var geracaoUsuariosPerfisIniciais = serviceScope.ServiceProvider.GetService<IGeracaoUsuariosPerfisIniciais>();

        geracaoUsuariosPerfisIniciais.GerarPerfis();
        geracaoUsuariosPerfisIniciais.GerarUsuarios();
    }
}