using System;
using Microsoft.Owin;
using Owin;
using SharedWeb;

[assembly: OwinStartupAttribute(typeof(Web.Startup))]
namespace Web
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            AppDomain.CurrentDomain.Load(typeof(MyHub).Assembly.FullName);

            app.MapSignalR();
            ConfigureAuth(app);
        }
    }
}
