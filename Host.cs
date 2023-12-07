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
using System.Xml.Serialization; // Add this for XmlSerializer



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
                        });
                    }).UseUrls("http://*:5000");
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
                // Asynchronously read the data file
                string fileContent = await File.ReadAllTextAsync(dataFilePath);

                // Parse the fileContent using regular expressions to extract the sensor values
                var fanSpeeds = Regex.Matches(fileContent, @"Fan\d\s+\|\s+(\d+\.\d+)\s+\|")
                                     .Cast<Match>()
                                     .Select(m => m.Groups[1].Value)
                                     .Select(value => float.Parse(value, System.Globalization.CultureInfo.InvariantCulture))
                                     .Select(number => ((int)Math.Round(number)).ToString())
                                     .ToArray();

                var cpuTemps = Regex.Matches(fileContent, @"Temp\s+\|\s+(\d+\.\d+)\s+\|")
                                        .Cast<Match>()
                                    .Select(m => m.Groups[1].Value)
                                    .Select(value => float.Parse(value, System.Globalization.CultureInfo.InvariantCulture))
                                    .Select(number => ((int)Math.Round(number)).ToString())
                                    .ToArray();

                var powerConsumption = Regex.Match(fileContent, @"Pwr Consumption\s+\|\s+(\d+\.\d+)\s+\|")
                                                .Groups[1].Value;
                powerConsumption = ((int)Math.Round(float.Parse(powerConsumption, System.Globalization.CultureInfo.InvariantCulture))).ToString();

                // Create an XML representation of the data
                var xmlData = new XElement("SensorData",
                    new XElement("Fans",
                        fanSpeeds.Select((speed, index) => new XElement($"Fan{index + 1}", speed))
                    ),
                    new XElement("Temperatures",
                        cpuTemps.Select((temp, index) => new XElement($"CPU{index + 1}", temp))
                    ),
                    new XElement("PowerConsumption", powerConsumption)
                );
                await File.WriteAllTextAsync(xmlFilePath, xmlData.ToString());
            }
            catch (Exception ex)
            {
                // Log the error
                Console.Error.WriteLine($"Error processing sensor data: {ex.Message}");

                // Clear the contents of sensors.xml
                var emptyData = new XElement("SensorData",
                    new XElement("Fans"),
                    new XElement("Temperatures"),
                    new XElement("PowerConsumption", "0")
                );
                await File.WriteAllTextAsync(xmlFilePath, emptyData.ToString());
                Logger.Instance.Log("Sleeping for 10 seconds");
                Console.WriteLine("Sleeping for 10 seconds" );
                await Task.Delay(10000);
            }
        }
    }
}