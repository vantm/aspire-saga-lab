var builder = DistributedApplication.CreateBuilder(args);

var rabbitMQ = builder.AddRabbitMQ("messaging-rabbit-mq")
    .WithManagementPlugin(port: 15678)
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

builder.AddProject<Projects.AspireSaga_Runner>("service-runner")
    .WithReference(rabbitMQ);

builder.AddProject<Projects.AspireSaga_Delivery>("service-delivery")
    .WithReference(rabbitMQ);

builder.Build().Run();
