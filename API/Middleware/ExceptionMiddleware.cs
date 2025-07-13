using API.Errors;
using System.Text.Json;

namespace API.Middleware
{
    public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            // Hook into the response body
            var originalBody = context.Response.Body;
            using var memStream = new MemoryStream();
            context.Response.Body = memStream;
            try
            {
                await next(context);

                // Handle non-exception status codes (like 503)
                if (context.Response.StatusCode == StatusCodes.Status503ServiceUnavailable)
                {
                    await WriteCustomJsonAsync(context, 503, "Service Unavailable (Rate Limit Reached)");
                }
                else if (context.Response.StatusCode == StatusCodes.Status429TooManyRequests)
                {
                    await WriteCustomJsonAsync(context, 429, "Too Many Requests (Rate Limit)");
                }
                else
                {
                    memStream.Position = 0;
                    await memStream.CopyToAsync(originalBody);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                await WriteCustomJsonAsync(context, 500, env.IsDevelopment() ? ex.StackTrace ?? "Internal Server Error" : "Internal Server Error", ex.Message ?? "An error occurred");
            }
            finally
            {
                context.Response.Body = originalBody;
            }
        }

        private async Task WriteCustomJsonAsync(HttpContext context, int statusCode, string message, string? error = null)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;

            var response = new ApiException(statusCode, message, error);
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var json = JsonSerializer.Serialize(response, options);

            await context.Response.WriteAsync(json);
        }
    }
}
