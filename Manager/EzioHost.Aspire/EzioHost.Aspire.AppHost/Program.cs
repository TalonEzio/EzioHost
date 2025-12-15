using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var keycloak = builder
    .AddKeycloak("Keycloak", 18080)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume();

// var mssql = builder.AddSqlServer("mssql")
//     .WithLifetime(ContainerLifetime.Persistent);

//var garnet = builder.AddGarnet("garnet", 18119)
//    .WithLifetime(ContainerLifetime.Persistent);

var webApi = builder.AddProject<EzioHost_WebAPI>("WebApi")
        .WaitFor(keycloak)
    // .WaitFor(mssql)
    //.WaitFor(garnet)
    ;
var webApp = builder.AddProject<EzioHost_WebApp>("WebBlazor")
        .WaitFor(webApi)
    ;
var reverseProxy = builder.AddProject<EzioHost_ReverseProxy>("ReverseProxy")
        .WaitFor(webApp)
    //.WithReference(garnet)
    ;


builder.Build().Run();