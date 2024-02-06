using System;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Play.Common.Identity;
using Play.Common.MassTransit;
using Play.Common.MongoDB;
using Play.Common.Settings;
using Play.Trading.Service.StateMachines;
using Play.Trading.Service.Entities;
using System.Reflection;
using Play.Trading.Service.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Play.Trading.Service.Settings;
using Play.Inventory.Contracts;
using Play.Identity.Contracts;
using Microsoft.AspNetCore.SignalR;
using Play.Trading.Service.SignalR;
using System.Text.Json.Serialization;
using Play.Common.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

const string AllowedOriginSetting = "AllowedOrigin";
// Add services to the container.

builder.Services.AddMongo()
                    .AddMongoRepository<CatalogItem>("catalogitems")
                    .AddMongoRepository<InventoryItem>("inventoryitems")
                    .AddMongoRepository<ApplicationUser>("users")
                    .AddJwtBearerAuthentication();


AddMassTransit(builder.Services);
builder.Services.AddControllers(options =>
{
    options.SuppressAsyncSuffixInActionNames = false;
})
    .AddJsonOptions(options => options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull);
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks()
    .AddMongoDb();
builder.Services.AddSingleton<IUserIdProvider, UserIdProvider>()
    .AddSingleton<MessageHub>()
    .AddSignalR();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors(b =>
                {
                    b.WithOrigins(builder.Configuration[AllowedOriginSetting])
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapPlayEconomyHealthChecks();

app.MapControllers();
app.MapHub<MessageHub>("/messagehub");

app.Run();

void AddMassTransit(IServiceCollection services)
{
    services.AddMassTransit(configure =>
    {
        configure.UsingPlayEconomyRabbitMq(retryConfigurator =>
                {
                    retryConfigurator.Interval(3, TimeSpan.FromSeconds(5));
                    retryConfigurator.Ignore(typeof(UnknownItemException));
                });
        configure.AddConsumers(Assembly.GetEntryAssembly());
        configure.AddSagaStateMachine<PurchaseStateMachine, PurchaseState>(sagaConfig =>
            {
                sagaConfig.UseInMemoryOutbox();
            })
            .MongoDbRepository(r =>
            {
                var serviceSettings = builder.Configuration.GetSection(nameof(ServiceSettings))
                                                   .Get<ServiceSettings>();
                var mongoSettings = builder.Configuration.GetSection(nameof(MongoDbSettings))
                                                   .Get<MongoDbSettings>();

                r.Connection = mongoSettings.ConnectionString;
                r.DatabaseName = serviceSettings.ServiceName;
            });
    });
    var queueSettings = builder.Configuration.GetSection(nameof(QueueSettings))
                                               .Get<QueueSettings>();
    EndpointConvention.Map<GrantItems>(new Uri(queueSettings.GrantItemsQueueAddress));
    EndpointConvention.Map<DebitGil>(new Uri(queueSettings.DebitGilQueueAddress));
    EndpointConvention.Map<SubtractItems>(new Uri(queueSettings.SubtractItemsQueueAddress));
    //services.AddMassTransitHostedService();

}