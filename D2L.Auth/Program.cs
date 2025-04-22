using Microsoft.AspNetCore.Builder;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;
using static Microsoft.AspNetCore.Http.StatusCodes;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();



//builder.Services.AddHttpsRedirection(options => {
//    options.RedirectStatusCode = Status307TemporaryRedirect;
//    options.HttpsPort = 3001;
//    });
// Application Insights isn't enabled by default. See https://aka.ms/AAt8mw4.
// builder.Services
//     .AddApplicationInsightsTelemetryWorkerService()
//     .ConfigureFunctionsApplicationInsights();


builder.Build().Run();