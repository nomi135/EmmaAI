using API.Data;
using API.HangFire;
using API.Helpers;
using API.Interfaces;
using API.Services;
using API.SignalR;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Polly;
using System.Threading.RateLimiting;

namespace API.Extensions
{
    public static class ApplicationServiceExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration config)
        {
            services.AddControllers();
            services.AddDbContext<DataContext>(opt =>
            {
                opt.UseSqlServer(config.GetConnectionString("dbConnection"));
            });

            //Register IHttpContextAccessor first
            services.AddHttpContextAccessor(); // cannot use IHttpContextAccessor due to hangfire
            //Register HttpClient
            services.AddScoped<HttpClient>();
            //Register other services
            services.AddMemoryCache();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddCors();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<LogUserActivity>();
            services.AddScoped<IChatHandlerService, ChatHandlerService>();
            services.AddScoped<IIntentService, IntentService>();
            services.AddScoped<IWeatherService, WeatherService>();
            services.AddScoped<INewsService, NewsService>();
            services.AddScoped<IUserInfoService, UserInfoService>();
            services.AddScoped<ISpeechService, SpeechService>();
            services.AddScoped<IFileService, FileService>();
            services.AddScoped<IDocumentService, DocumentService>();
            services.AddScoped<IReminderService, ReminderService>();
            services.AddScoped<IContactRepository, ContactRepository>();
            services.AddScoped<IUserChatHistoryRepository, UserChatHistoryRepository>();
            services.AddScoped<ISurveyFormRepository, SurveyFormRepository>();
            services.AddScoped<ISurveyFormDataRepository, SurveyFormDataRepository>();
            services.AddScoped<IReminderRepository, ReminderRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddHangfire(cnfg =>
                cnfg.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(config.GetConnectionString("dbConnection")));
            services.AddHangfireServer();
            services.AddSignalR();
            services.AddSingleton<PresenceTracker>();
            services.AddScoped<ReminderJobService>();
            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

            // register resilience timeout
            services.AddHttpClient("AzureOpenAI")
                .AddStandardResilienceHandler(options =>
                {
                    // timeout for each individual attempt (before retry)
                    options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(30);

                    // Retry settings
                    options.Retry.MaxRetryAttempts = 3;
                    options.Retry.BackoffType = DelayBackoffType.Exponential;
                    options.Retry.Delay = TimeSpan.FromSeconds(2);

                    // Circuit Breaker
                    options.CircuitBreaker.FailureRatio = 0.5;
                    options.CircuitBreaker.MinimumThroughput = 3;
                    options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(60);
                    options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(10);
                });

            //AI agent services
            services.AddScoped<Kernel>(sp =>
            {
                var azureOpenAIConfig = config.GetSection("AzureOpenAI") ?? throw new Exception("AzureOpenAI configuration not found");
                var deploymentName = azureOpenAIConfig["DeploymentName"] ?? throw new Exception("AzureOpenAI deployment name not found");
                var endPoint = azureOpenAIConfig["Endpoint"] ?? throw new Exception("AzureOpenAI end point not found");
                var ApiKey = azureOpenAIConfig["ApiKey"] ?? throw new Exception("AzureOpenAI api key not found");
                var model = azureOpenAIConfig["Model"] ?? throw new Exception("AzureOpenAI model not found");
                var textEmbeddingDeploymentName = azureOpenAIConfig["TextEmbeddingDeploymentName"] ?? throw new Exception("AzureOpenAI text embedding deployment name not found");
                var textEmbeddingModel = azureOpenAIConfig["TextEmbeddingModel"] ?? throw new Exception("AzureOpenAI text embedding model not found");
                var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient("AzureOpenAI"); // uses resilience config
#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                var kernel = Kernel.CreateBuilder()
                    .AddAzureOpenAIChatCompletion(deploymentName, endPoint, ApiKey, modelId: model, httpClient: httpClient)
                    .AddAzureOpenAITextEmbeddingGeneration(textEmbeddingDeploymentName, endPoint, ApiKey, textEmbeddingDeploymentName, httpClient: httpClient)
                    .Build();
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

                return kernel;
            });


            // Register AzureOpenAIPromptExecutionSettings
            services.AddSingleton(new AzureOpenAIPromptExecutionSettings
            {
                MaxTokens = 4096,
                Temperature = 0.5,
                TopP = 0.8,
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
            });

            //add rate limit
            services.AddRateLimiter(options =>
            {
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.User.Identity?.Name ?? httpContext.Request.Headers.Host.ToString(),
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 100,
                        QueueLimit = 0,
                        Window = TimeSpan.FromMinutes(1)
                    }));
            });
            
            //AI agent services

            return services;
        }
    }
}
