using API.Data;
using API.Helpers;
using API.Interfaces;
using API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Connectors.OpenAI;

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
            services.AddHttpContextAccessor();
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
            services.AddScoped<IContactRepository, ContactRepository>();
            services.AddScoped<IUserChatHistoryRepository, UserChatHistoryRepository>();
            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

            //AI agent services
            services.AddScoped<Kernel>(sp =>
            {
                var azureOpenAIConfig = config.GetSection("AzureOpenAI") ?? throw new Exception("AzureOpenAI configuration not found");
                var deploymentName = azureOpenAIConfig["DeploymentName"] ?? throw new Exception("AzureOpenAI deployment name not found");
                var endPoint = azureOpenAIConfig["Endpoint"] ?? throw new Exception("AzureOpenAI end point not found");
                var ApiKey = azureOpenAIConfig["ApiKey"] ?? throw new Exception("AzureOpenAI api key not found");
                var model = azureOpenAIConfig["Model"] ?? throw new Exception("AzureOpenAI model not found");

                var kernel = Kernel.CreateBuilder()
                    .AddAzureOpenAIChatCompletion(deploymentName, endPoint, ApiKey, modelId: model)
                    .Build();

                return kernel;
            });

            // Register AzureOpenAIPromptExecutionSettings
            services.AddSingleton(new AzureOpenAIPromptExecutionSettings
            { 
                MaxTokens = 4096, Temperature = 0.5, TopP = 0.8, 
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
            });
            //AI agent services

            return services;
        }
    }
}
