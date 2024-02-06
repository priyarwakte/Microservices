using Microsoft.OpenApi.Models;
using Play.Catalog.Service.Entities;
using Play.Common.MongoDB;
using Play.Common.Settings;
using Play.Common.MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Play.Catalog.Service;
using Play.Common.Identity;
using Play.Common.HealthChecks;
using Play.Common.Configuration;

const string AllowedOriginSetting = "AllowedOrigin";
var builder = WebApplication.CreateBuilder(args);
builder.Host.ConfigureAzureKeyVault();
// Add services to the container.

ServiceSettings serviceSettings = builder.Configuration.GetSection(nameof(ServiceSettings)).Get<ServiceSettings>();

builder.Services.AddMongo()
                .AddMongoRepository<Item>("items")
                .AddMassTransitWithMessageBroker(builder.Configuration)
                .AddJwtBearerAuthentication();

builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy(Policies.Read, policy =>
                {
                    policy.RequireRole("Admin");
                    policy.RequireClaim("scope", "catalog.readaccess", "catalog.fullaccess");
                });

                options.AddPolicy(Policies.Write, policy =>
                {
                    policy.RequireRole("Admin");
                    policy.RequireClaim("scope", "catalog.writeaccess", "catalog.fullaccess");
                });
            });


builder.Services.AddControllers(options =>
            {
                options.SuppressAsyncSuffixInActionNames = false;
            });

builder.Services.AddControllers(options =>
            {
                options.SuppressAsyncSuffixInActionNames = false;
            });
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Play.Catalog.Service", Version = "v1" });
            });
builder.Services.AddHealthChecks()
    .AddMongoDb();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Play.Catalog.Service v1"));
    app.UseCors(b =>
                {
                    b.WithOrigins(builder.Configuration[AllowedOriginSetting])
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseEndpoints(endpoints =>
{
    endpoints.MapDefaultControllerRoute();
    endpoints.MapPlayEconomyHealthChecks();
});

app.MapControllers();

app.Run();











