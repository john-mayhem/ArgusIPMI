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
        private static System.Timers.Timer? sensorDataTimer; // Fully qualify the Timer here

        private static async void OnTimedEvent(Object source, ElapsedEventArgs e)
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

        public static async Task StartWebServer(IPMIToolWrapper ipmiWrapper)
        {
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

                            // Endpoint to get the current configuration settings
                            endpoints.MapGet("/api/config", async context =>
                            {
                                var configManager = new ConfigManager();
                                var settings = configManager.LoadSettings();

                                // Serialize settings to XML
                                var serializer = new XmlSerializer(typeof(Settings));
                                using var stringWriter = new StringWriter();
                                serializer.Serialize(stringWriter, settings);

                                context.Response.ContentType = "application/xml";
                                await context.Response.WriteAsync(stringWriter.ToString());
                            });

                            // Endpoint to save the configuration settings
                            endpoints.MapPost("/api/config", async context =>
                            {
                                var configManager = new ConfigManager();

                                try
                                {
                                    // Deserialize settings from request body
                                    var serializer = new XmlSerializer(typeof(Settings));
                                    using var reader = new StreamReader(context.Request.Body);
                                    var requestBody = await reader.ReadToEndAsync();
                                    Console.WriteLine(requestBody); // Log the request body for debugging

                                    using var stringReader = new StringReader(requestBody);
                                    var settings = (Settings)serializer.Deserialize(stringReader);

                                    // Save the settings
                                    configManager.SaveSettings(settings);

                                    context.Response.StatusCode = StatusCodes.Status200OK;
                                    await context.Response.WriteAsync("Settings updated successfully.");
                                }
                                catch (Exception ex)
                                {
                                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                                    await context.Response.WriteAsync($"An error occurred while updating settings: {ex.Message}");
                                }
                            });

                            endpoints.MapPost("/api/retry-config", async context =>
                            {
                                try
                                {
                                    var configManager = new ConfigManager();
                                    var settings = configManager.LoadSettings();

                                    // Attempt to connect to IPMI with the new settings
                                    var sensorData = await ipmiWrapper.GetSensorListAsync(settings.IpAddress, settings.Username, settings.Password);

                                    // Handle errors as above, and return appropriate messages to the client
                                }
                                catch (Exception ex)
                                {
                                    // Error handling
                                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                                    await context.Response.WriteAsync("Failed to retry with new settings.");
                                }
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
            }
        }
    }
}