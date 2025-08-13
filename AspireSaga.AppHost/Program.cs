var builder = DistributedApplication.CreateBuilder(args);

var rabbitMQ = builder.AddRabbitMQ("messaging-rabbit-mq")
    .WithManagementPlugin()
    .WithLifetime(ContainerLifetime.Persistent);

builder.AddProject<Projects.AspireSaga_Catalog>("service-catalog")
    .WithReference(rabbitMQ);

builder.AddProject<Projects.AspireSaga_Inventory>("service-inventory")
    .WithReference(rabbitMQ);

builder.AddProject<Projects.AspireSaga_Basket>("service-basket")
    .WithReference(rabbitMQ);

builder.AddProject<Projects.AspireSaga_SalesOrder>("service-sales-order")
    .WithReference(rabbitMQ);

builder.AddProject<Projects.AspireSaga_Payment>("service-payment")
    .WithReference(rabbitMQ);

builder.AddProject<Projects.AspireSaga_Wallet>("service-wallet")
    .WithReference(rabbitMQ);

builder.Build().Run();
