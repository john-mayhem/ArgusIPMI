using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;  // Ensure this namespace is included
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Timers; // Add this to your using directives


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

    public static async Task StartWebServer()
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
                    });
                }).UseUrls("http://*:5000");
            }).Build();

        await host.RunAsync();
    }

    private static async Task ProcessSensorData()
    {
        string dataFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
        string dataFilePath = Path.Combine(dataFolderPath, "data.txt");

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

            string htmlFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "html");
            string xmlFilePath = Path.Combine(htmlFolderPath, "sensors.xml");
            await File.WriteAllTextAsync(xmlFilePath, xmlData.ToString());
        }
        catch (Exception ex)
        {
            // Log or handle exceptions
            Console.Error.WriteLine($"Error processing sensor data: {ex.Message}");
        }
    }
}