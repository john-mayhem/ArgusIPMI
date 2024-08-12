using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using System.Xml.Linq;
using System.Xml.Serialization; 
using Microsoft.Extensions.Logging;



namespace ArgusIPMI
{
    public class WebServerHost
    {
        private static System.Timers.Timer? sensorDataTimer; // Timer is nullable

        private static async void OnTimedEvent(object? source, ElapsedEventArgs e) // Make source nullable
        {
            if (sensorDataTimer != null) // Null check for sensorDataTimer
            {
                sensorDataTimer.Enabled = false;
                try
                {
                    await ProcessSensorData();
                }
                finally
                {
                    sensorDataTimer.Enabled = true;
                }
            }
        }
        private static Executor? _executor;
        public static async Task StartWebServer(Executor executor)
        {
            _executor = executor;
            // Set up the timer for 1000 milliseconds (1 second)
            sensorDataTimer = new System.Timers.Timer(1000)
            {
                AutoReset = false, // Prevent the Timer from calling the elapsed event repeatedly
                Enabled = true
            };
            sensorDataTimer.Elapsed += OnTimedEvent;

            var host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.Configure(app =>
                    {
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

                        app.UseRouting();


                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapGet("/", async context =>
                            {
                                await context.Response.WriteAsync("Hello, World!");
                            });

                            endpoints.MapPost("/setAutomatic", async context =>
                            {
                                await _executor.SetIPMIMode(true);
                                await context.Response.WriteAsync("Automatic mode set");
                            });

                            endpoints.MapPost("/setManual", async context =>
                            {
                                await _executor.SetIPMIMode(false);
                                await context.Response.WriteAsync("Manual mode set");
                            });

                            endpoints.MapPost("/setFanSpeed", async context =>
                            {
                                var speed = context.Request.Query["speed"].ToString();
                                if (string.IsNullOrEmpty(speed))
                                {
                                    context.Response.StatusCode = 400; // Bad Request
                                    await context.Response.WriteAsync("Speed parameter is missing or invalid.");
                                    return;
                                }

                                await _executor.SetFanSpeed(speed);
                                await context.Response.WriteAsync($"Fan speed set to {speed}");
                            });

                            endpoints.MapPost("/setFanSpeed10", async context =>
                            {
                                await _executor.SetFanSpeed10();
                                await context.Response.WriteAsync("Fan speed set to 10%");
                            });

                            endpoints.MapPost("/setFanSpeed20", async context =>
                            {
                                await _executor.SetFanSpeed20();
                                await context.Response.WriteAsync("Fan speed set to 20%");
                            });

                            endpoints.MapPost("/setFanSpeed30", async context =>
                            {
                                await _executor.SetFanSpeed30();
                                await context.Response.WriteAsync("Fan speed set to 30%");
                            });

                            endpoints.MapPost("/setFanSpeed40", async context =>
                            {
                                await _executor.SetFanSpeed40();
                                await context.Response.WriteAsync("Fan speed set to 40%");
                            });

                            endpoints.MapPost("/setFanSpeed50", async context =>
                            {
                                await _executor.SetFanSpeed50();
                                await context.Response.WriteAsync("Fan speed set to 50%");
                            });

                            endpoints.MapPost("/setFanSpeed60", async context =>
                            {
                                await _executor.SetFanSpeed60();
                                await context.Response.WriteAsync("Fan speed set to 60%");
                            });

                            endpoints.MapPost("/setFanSpeed70", async context =>
                            {
                                await _executor.SetFanSpeed70();
                                await context.Response.WriteAsync("Fan speed set to 70%");
                            });

                            endpoints.MapPost("/setFanSpeed80", async context =>
                            {
                                await _executor.SetFanSpeed80();
                                await context.Response.WriteAsync("Fan speed set to 80%");
                            });

                            endpoints.MapPost("/setFanSpeed90", async context =>
                            {
                                await _executor.SetFanSpeed90();
                                await context.Response.WriteAsync("Fan speed set to 90%");
                            });

                            endpoints.MapPost("/setFanSpeed100", async context =>
                            {
                                await _executor.SetFanSpeed100();
                                await context.Response.WriteAsync("Fan speed set to 100%");
                            });
                        });
                    }).UseUrls("http://*:5000");
                    webBuilder.ConfigureLogging(logging =>
                    {
                        logging.ClearProviders(); // Clear existing logging providers
                        logging.AddProvider(new CustomLoggerProvider()); // Add custom logger provider
                    });
                }).Build();

            await host.RunAsync();
        }

        private static async Task ProcessSensorData()
        {
            string dataFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
            string dataFilePath = Path.Combine(dataFolderPath, "data.txt");
            string htmlFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "html");
            string xmlFilePath = Path.Combine(htmlFolderPath, "sensors.xml");

            try
            {
                string fileContent = await File.ReadAllTextAsync(dataFilePath);

                // Adjusted regex for Fan RPM
                var fanSpeeds = Regex.Matches(fileContent, @"Fan\d RPM\s+\|\s+(\d+\.\d+)")
                                     .Cast<Match>()
                                     .Select(m => m.Groups[1].Value)
                                     .ToArray();

                // Adjusted regex for Temperatures
                var cpuTemps = Regex.Matches(fileContent, @"Temp\s+\|\s+(\d+\.\d+)")
                                    .Cast<Match>()
                                    .Select(m => m.Groups[1].Value)
                                    .ToArray();

                // Adjusted regex for Power Consumption
                var powerConsumptionMatch = Regex.Match(fileContent, @"Pwr Consumption\s+\|\s+(\d+\.\d+)");
                var powerConsumption = powerConsumptionMatch.Success ? powerConsumptionMatch.Groups[1].Value : "0";

                var xmlData = new XElement("SensorData",
                    new XElement("Fans", fanSpeeds.Select((speed, index) => new XElement($"Fan{index + 1}", speed))),
                    new XElement("Temperatures", cpuTemps.Select((temp, index) => new XElement($"CPU{index + 1}", temp))),
                    new XElement("PowerConsumption", powerConsumption)
                );
                await File.WriteAllTextAsync(xmlFilePath, xmlData.ToString());
            }
            catch (Exception ex)
            {
                // Error handling
                Console.Error.WriteLine($"Error processing sensor data: {ex.Message}");
                var emptyData = new XElement("SensorData", new XElement("Fans"), new XElement("Temperatures"), new XElement("PowerConsumption", "0"));
                await File.WriteAllTextAsync(xmlFilePath, emptyData.ToString());
                Logger.Instance.Log("Sleeping for 10 seconds");
                Console.WriteLine("Sleeping for 10 seconds");
                await Task.Delay(10000);
            }
        }

    }
}