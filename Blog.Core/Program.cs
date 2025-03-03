﻿using System;
using System.IO;
using System.Reflection;
using System.Xml;
using Autofac.Extensions.DependencyInjection;
using Blog.Core.Model.Models;
using log4net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Blog.Core
{
    public class Program
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Program));
        public static void Main(string[] args)
        {
            // 生成承载 web 应用程序的 Microsoft.AspNetCore.Hosting.IWebHost。Build是WebHostBuilder最终的目的，将返回一个构造的WebHost，最终生成宿主。
            var host = CreateHostBuilder(args).Build();

            // 创建可用于解析作用域服务的新 Microsoft.Extensions.DependencyInjection.IServiceScope。
            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var loggerFactory = services.GetRequiredService<ILoggerFactory>();

                try
                {
                    // 从 system.IServicec提供程序获取 T 类型的服务。
                    // 数据库连接字符串是在 Model 层的 Seed 文件夹下的 MyContext.cs 中
                    var configuration = services.GetRequiredService<IConfiguration>();
                    if (configuration.GetSection("AppSettings")["SeedDBEnabled"].ObjToBool() || configuration.GetSection("AppSettings")["SeedDBDataEnabled"].ObjToBool())
                    {
                        var myContext = services.GetRequiredService<MyContext>();
                        var Env = services.GetRequiredService<IWebHostEnvironment>();
                        DBSeed.SeedAsync(myContext, Env.WebRootPath).Wait();
                    }
                }
                catch (Exception e)
                {
                    log.Error($"Error occured seeding the Database.\n{e.Message}");
                    throw;
                }
            }

            // 运行 web 应用程序并阻止调用线程, 直到主机关闭。
            // 创建完 WebHost 之后，便调用它的 Run 方法，而 Run 方法会去调用 WebHost 的 StartAsync 方法
            // 将Initialize方法创建的Application管道传入以供处理消息
            // 执行HostedServiceExecutor.StartAsync方法
            // ※※※※ 有异常，查看 Log 文件夹下的异常日志 ※※※※  
            host.Run();
        }


        public static IHostBuilder CreateHostBuilder(string[] args) =>
          Host.CreateDefaultBuilder(args)
            .UseServiceProviderFactory(new AutofacServiceProviderFactory()) //<--NOTE THIS
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder
                .UseStartup<Startup>()
                .ConfigureLogging((hostingContext, builder) =>
                {
                    //该方法需要引入Microsoft.Extensions.Logging名称空间
                    builder.AddFilter("System", LogLevel.Error); //过滤掉系统默认的一些日志
                    builder.AddFilter("Microsoft", LogLevel.Error);//过滤掉系统默认的一些日志

                    //添加Log4Net
                    var path = Path.Combine(Directory.GetCurrentDirectory(), "Log4net.config");
                    //不带参数：表示log4net.config的配置文件就在应用程序根目录下，也可以指定配置文件的路径
                    //需要添加nuget包：Microsoft.Extensions.Logging.Log4Net.AspNetCore
                    builder.AddLog4Net(path);
                });
            });
    }
}
