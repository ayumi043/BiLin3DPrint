using log4net;
using Nancy;
using Nancy.Authentication.Forms;
using Nancy.Bootstrapper;
using Nancy.Conventions;
using Nancy.Session;
using Nancy.TinyIoc;
using ServiceStack.OrmLite;
using System;
using System.Data;
using System.IO;
using BiLin3D;
using Microsoft.Extensions.Configuration;
using Nancy.Configuration;

namespace Bilin3d {
    public class Bootstrapper : DefaultNancyBootstrapper {

        public override void Configure(INancyEnvironment environment) {
            // 开发模式下不缓存试图，并显示错误信息
            #if DEBUG
                environment.Views(runtimeViewUpdates: true); 
                environment.Tracing(enabled: false, displayErrorTraces: true);
            #endif           
        }

        protected override void ConfigureConventions(NancyConventions conventions) {
            base.ConfigureConventions(conventions);

            //启用错误提示
            //StaticConfiguration.DisableErrorTraces = false;
            
            conventions.StaticContentsConventions.Add(
                StaticContentConventionBuilder.AddDirectory("assets", @"public/assets")
            );  
            conventions.StaticContentsConventions.Add(
                StaticContentConventionBuilder.AddDirectory("resource", @"public/resource")
            );
            conventions.StaticContentsConventions.Add(
                StaticContentConventionBuilder.AddDirectory("", @"public")
            );
        }


        // 只在启动时触发1次
        protected override void ConfigureApplicationContainer(TinyIoCContainer container) {
            base.ConfigureApplicationContainer(container);
           
            // Razor
            //container.Register<IViewEngine, Nancy.ViewEngines.Razor.RazorViewEngine>();
            //container.Register<Nancy.ViewEngines.Razor.IRazorConfiguration, Nancy.ViewEngines.Razor.DefaultRazorConfiguration>();
            
            // log4net
            FileInfo log4NetConfig = new FileInfo(System.IO.Directory.GetCurrentDirectory() + "\\log4net.config");
            log4net.Config.XmlConfigurator.ConfigureAndWatch(log4NetConfig);
            container.Register(typeof(ILog), (c, o) => LogManager.GetLogger(typeof(Bootstrapper)));

            // db
            var constr = Startup.Configuration.GetConnectionString("mysql");
            var dbFactory = new OrmLiteConnectionFactory(
                constr, //Connection String
                MySqlDialect.Provider);
            container.Register(dbFactory, "dbFactory");
        }

        //protected override IEnumerable<Type> ViewEngines {
        //    get {
        //        return new[] { typeof(Nancy.ViewEngines.Razor.RazorViewEngine) };
        //    }
        //}

        // 每次请求都会触发,一个页面会触发多次
        protected override void ConfigureRequestContainer(TinyIoCContainer container, NancyContext context) {
            base.ConfigureRequestContainer(container, context);

            // db
            var dbFactory = container.Resolve<OrmLiteConnectionFactory>("dbFactory");
            var db = dbFactory.OpenDbConnection();
            container.Register<IDbConnection>(db);


            // Here we register our user mapper as a per-request singleton.
            // As this is now per-request we could inject a request scoped
            // database "context" or other request scoped services.
            container.Register<IUserMapper, UserMapper>();
        }

        // 只在启动时触发1次
        protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines) {
            base.ApplicationStartup(container, pipelines);      
        }

        // 每次请求都会触发,一个页面会触发多次
        protected override void RequestStartup(TinyIoCContainer container, Nancy.Bootstrapper.IPipelines pipelines, NancyContext context) {
            base.RequestStartup(container, pipelines, context);

            var formsAuthConfiguration = new FormsAuthenticationConfiguration() {
                RedirectUrl = "~/account/logon",
                UserMapper = container.Resolve<IUserMapper>(),
            };
            FormsAuthentication.Enable(pipelines, formsAuthConfiguration);

            // Enabling sessions in Nancy
            CookieBasedSessions.Enable(pipelines);

            //放RequestStartup这里是每次请求时判断session，为了避免session过期，所以不放在ApplicationStartup
            pipelines.BeforeRequest += (ctx) => {
                var uid = ctx.Request.Session["TempUserId"];
                var user = ctx.CurrentUser;
                if (user == null && uid == null) {
                    ctx.Request.Session["TempUserId"] = "temp-" + Guid.NewGuid().ToString();
                }
                return null;
            };

        }
    }

    public class vNextRootPathProvider : IRootPathProvider {
        public string GetRootPath() {
            //return System.IO.Directory.GetCurrentDirectory();
            return Startup.Environment.ContentRootPath;
        }
    }

}