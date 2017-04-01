using Owin;
using Microsoft.Owin;

[assembly: OwinStartup(typeof(AzureMediaServicesDemo.Startup))]
namespace AzureMediaServicesDemo
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.MapSignalR();
        }
    }
}