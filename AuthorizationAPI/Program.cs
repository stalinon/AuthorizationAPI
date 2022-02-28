using AuthorizationAPI;
using AuthorizationAPI.Authorization;
using AuthorizationAPI.DataAccess;
using AuthorizationAPI.Helpers;
using AuthorizationAPI.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;


var builder = WebApplication.CreateBuilder(args);

{
    var services = builder.Services;
    var env = builder.Environment;

    string connection = builder.Configuration.GetConnectionString("DefaultConnection");
    builder.Services.AddDbContext<DataContext>(options => options.UseNpgsql(connection));

    services.AddCors();
    services.AddControllers().AddJsonOptions(x =>
    {
        x.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

    var mapper = MappingConfig.RegisterMaps().CreateMapper();
    builder.Services.AddSingleton(mapper);
    builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

    services.AddEndpointsApiExplorer();
    services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Version = "v1",
            Title = "Auth API",
            Description = "ASP.NET Core Web API"
        });
    });

    services.Configure<AuthSettings>(builder.Configuration.GetSection("AppSettings"));

    services.AddScoped<IJwtUtils, JwtUtils>();
    services.AddScoped<IAccountService, AccountService>();
    services.AddScoped<IEmailService, EmailService>();
    services.AddScoped<IAccountRepository, AccountRepository>();
}

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();
    dataContext.Database.Migrate();
}

{
    
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Auth API V1");
    });

    app.UseCors(x => x
        .SetIsOriginAllowed(origin => true)
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials());

    app.UseMiddleware<ErrorHandlerMiddleware>();

    app.UseMiddleware<JwtMiddleware>();

    app.MapControllers();
}

app.Run("http://localhost:4000");