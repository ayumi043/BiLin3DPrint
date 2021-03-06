﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Nancy.Owin;
using Nancy;
using Bilin3d;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace BiLin3D {
    public class Startup {

        public static IHostingEnvironment Environment { get; private set; }
        public static IConfiguration Configuration { get; set; }

        public Startup(IHostingEnvironment env) {
            Environment = env;

            // work with with a builder using multiple calls
            // chain calls together as a fluent API
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            Configuration = config;
        }


        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services) {
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory) {
            loggerFactory.AddConsole();

            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            }

            app.UseOwin().UseNancy();
            //app.UseOwin(x => {
            //    x.UseNancy(opts => opts.Bootstrapper = new Bootstrapper());
            //});

        }
    }
}
