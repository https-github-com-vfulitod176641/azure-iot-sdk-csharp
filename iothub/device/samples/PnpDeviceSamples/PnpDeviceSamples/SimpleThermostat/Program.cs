using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;

namespace SimpleThermostat
{
    public class Program
    {
        private const string DeviceConnectionString = "device_connection_string_here";
        private const string ModelId = "dtmi:com:example:simplethermostat;1";

        private static DeviceClient s_deviceClient;

        public static async Task Main(string[] _)
        {
            await RunSampleAsync().ConfigureAwait(false);
        }

        private static async Task RunSampleAsync()
        {
            // This sample follows the following workflow:
            // -> Initialize device client.
            // -> Set handler to receive "target temperature" updates.
            // -> Set handler to receive "reboot" command.
            // -> Retrieve current "target temperature".
            // -> Send "current temerature" update over both telemetry and property update.

            Console.WriteLine($">> {DateTime.Now}: Initialize the device client");
            await InitializeDeviceClient().ConfigureAwait(false);

            Console.WriteLine($">> {DateTime.Now}: Send current temperature reading...");
            await SendCurrentTemperatureAsync().ConfigureAwait(false);
        }

        private static async Task InitializeDeviceClient()
        {
            var options = new ClientOptions
            {
                ModelId = ModelId,
            };

            // Initialize the device client instance using the device connection string, transport of Mqtt over TCP (with fallback to Websocket),
            // and the device ModelId set in ClientOptions.
            s_deviceClient = DeviceClient.CreateFromConnectionString(DeviceConnectionString, TransportType.Mqtt, options);

            // Register a connection status change callback, that will get triggerred any time the device's connection status changes.
            s_deviceClient.SetConnectionStatusChangesHandler((status, reason) =>
            {
                Console.WriteLine($">> {DateTime.Now}: Connection status change registered - status={status}, reason={reason}");
            });

            // This will open the device client connection over Mqtt.
            await s_deviceClient.OpenAsync().ConfigureAwait(false);
        }

        private static async Task SendCurrentTemperatureAsync()
        {
            string telemetryName = "temperature";

            // Generate a random value between 40F and 90F for the current temperature reading.
            double currentTemperature = new Random().NextDouble() * 50 + 40;

            string telemetryPayload = $"{{ \"{telemetryName}\": {currentTemperature} }}";
            var message = new Message(Encoding.UTF8.GetBytes(telemetryPayload))
            {
                ContentEncoding = "utf-8",
                ContentType = "application/json",
            };

            await s_deviceClient.SendEventAsync(message).ConfigureAwait(false);
            Console.WriteLine($">> {DateTime.Now}: Sent current temperature {currentTemperature} over telemetry.");
        }

        private static Task SetTargetTemperature()
        {
            return Task.CompletedTask;
        }

        private static Task ReceiveTargetTemperatureUpdates()
        {
            return Task.CompletedTask;
        }

        private static Task HandleRebootCommand()
        {
            return Task.CompletedTask;
        }
    }
}
