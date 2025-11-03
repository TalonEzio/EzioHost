var builder = DistributedApplication.CreateBuilder(args);

//var keycloak = builder
//        .AddKeycloak("keycloak", 18080)
//        .WithLifetime(ContainerLifetime.Persistent);

//var mssql = builder.AddSqlServer("mssql")
//    .WithLifetime(ContainerLifetime.Persistent);

var webApi = builder.AddProject<Projects.EzioHost_WebAPI>("WebApi")
    //.WaitFor(mssql)
    ;
var webApp = builder.AddProject<Projects.EzioHost_WebApp>("WebBlazor")
    .WaitFor(webApi)
    ;
builder.AddProject<Projects.EzioHost_ReverseProxy>("ReverseProxy")
    .WaitFor(webApp)
    ;


builder.Build().Run();
