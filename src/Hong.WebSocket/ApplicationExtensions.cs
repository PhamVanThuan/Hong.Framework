using Microsoft.Extensions.DependencyInjection;

namespace Hong.WebSocket
{
    public static class ApplicationExtensions
    {
        public static void AddWebSocket(this IServiceCollection services)
        {
            services.AddSingleton(typeof(WebSocketSessionManger));
            services.AddScoped(typeof(WebSocketHandle));
        }
    }
}
