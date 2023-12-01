using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;  // Ensure this namespace is included
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

    public class WebServerHost
    {
        public static async Task StartWebServer()
        {
            var host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.Configure(app =>
                    {
                        app.UseRouting();

                        // Set up static file serving
                        var htmlFolder = Path.Combine(Directory.GetCurrentDirectory(), "html");
                        if (!Directory.Exists(htmlFolder))
                        {
                            Directory.CreateDirectory(htmlFolder);
                        }

                        app.UseDefaultFiles(new DefaultFilesOptions
                        {
                            FileProvider = new PhysicalFileProvider(htmlFolder),
                            DefaultFileNames = new List<string> { "index.html" }
                        });

                        app.UseStaticFiles(new StaticFileOptions
                        {
                            FileProvider = new PhysicalFileProvider(htmlFolder),
                            RequestPath = ""
                        });

                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapGet("/", async context =>
                            {
                                await context.Response.WriteAsync("Hello, World!");
                            });
                        });
                    }).UseUrls("http://*:5000"); // Listen on all network interfaces on port 5000
                }).Build();

            await host.RunAsync();
        }
    }
