using MassTransit;
using Microsoft.EntityFrameworkCore;
using VbvvDemo;
using VbvvDemo.Crediteringen;
using VbvvDemo.Data;
using VbvvDemo.Entities;
using VbvvDemo.FileQueues;
using VbvvDemo.Inventarisaties;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var connectionString = builder.Configuration.GetConnectionString("vbvvdemo");

builder.Services.AddDbContext<VbvvDbContext>((provider, optionsBuilder) =>
{
    optionsBuilder.UseNpgsql(connectionString);
});

builder.Services.AddScoped<CreateFileQueueItemInventarisatieActivity>();
builder.Services.AddScoped<CreatePendingCrediteringFileQueueItemActivity<CrediteringOntvangenEvent>>();
builder.Services.AddScoped<ClaimNextPendingFileQueueItemActivity>();
builder.Services.AddScoped<StartClaimedFileProcessingActivity>();
builder.Services.AddScoped(typeof(UpdateToProcessedFileQueueItemActivity<>));
builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();
    
    x.AddConsumers(typeof(Program).Assembly);
    
    x.AddSagaStateMachine<DossierStateMachine, DossierState>()
        .EntityFrameworkRepository(r =>
        { 
            r.AddDbContext<DbContext, VbvvDbContext>();
            r.UsePostgres();
        });

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(new Uri(builder.Configuration.GetConnectionString("rabbitmq")!));
        cfg.ConfigureEndpoints(context);
        cfg.UseInMemoryOutbox(context);
    });
});

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

await using (var scope = builder.Services.BuildServiceProvider())
{
    var db = scope.GetRequiredService<VbvvDbContext>();
    await db.Database.EnsureCreatedAsync();
}
var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPost("/inventarisaties", async (Guid dossierId, IPublishEndpoint publishEndpoint) =>
{
    await publishEndpoint.Publish(new InventarisatieOntvangenEvent
    {
        DossierId = dossierId,
        FileId = Guid.NewGuid()
    });
    return Results.Accepted();
})
.WithName("IventarisatieOntvangen");

app.Run();

