var builder = DistributedApplication.CreateBuilder(args);

var webApi = builder.AddProject<Projects.EzioHost_WebAPI>("WebApi");
var webApp = builder.AddProject<Projects.EzioHost_WebApp>("Blazor").WaitFor(webApi);
builder.AddProject<Projects.EzioHost_ReverseProxy>("ReverseProxy").WaitFor(webApp);

builder.Build().Run();
