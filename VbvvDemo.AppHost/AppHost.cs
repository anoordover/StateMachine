var builder = DistributedApplication.CreateBuilder(args);

var username = builder.AddParameter("username", "postgres");
var password = builder.AddParameter("password", "password123!");
var pgServer = builder.AddPostgres("postgres", port: 5432,
    userName: username,
    password: password)
    .WithPgWeb(resourceBuilder =>
    {
        resourceBuilder.WithHostPort(9191);
    });
var db = pgServer.AddDatabase("vbvvdemo");

var rbqUsername = builder.AddParameter("rbqusername", "guest");
var rbqPassword = builder.AddParameter("rbqpassword", "guest");
var rabbitmq = builder.AddRabbitMQ("rabbitmq", port: 5672,
        userName: rbqUsername,
        password: rbqPassword)
    .WithManagementPlugin(9292);

builder.AddProject<Projects.VbvvDemo>("vbvvdemoProject")
    .WithReference(db)
    .WaitFor(db)
    .WithReference(rabbitmq)
    .WaitFor(rabbitmq);

builder.Build().Run();
