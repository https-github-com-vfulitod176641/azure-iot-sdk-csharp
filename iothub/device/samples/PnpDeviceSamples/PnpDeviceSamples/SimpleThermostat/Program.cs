using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;

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
            // -> Initialize device client instance.
            // -> Set handler to receive "target temperature" updates.
            // -> Set handler to receive "reboot" command.
            // -> Retrieve current "target temperature".
            // -> Send "current temperature" over both telemetry and property channels.

            PrintLog($"Initialize the device client.");
            await InitializeDeviceClientAsync().ConfigureAwait(false);

            PrintLog($"Set handler to receive \"target temperature\" updates.");
            await s_deviceClient.SetDesiredPropertyUpdateCallbackAsync(TargetTemperatureUpdateCallbackAsync, s_deviceClient).ConfigureAwait(false);

            PrintLog($"Set handler for \"reboot\" command");
            await s_deviceClient.SetMethodHandlerAsync("reboot", HandleRebootCommandAsync, s_deviceClient);

            PrintLog($"Send current temperature reading.");
            await SendCurrentTemperatureAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Initialize the device client instance over Mqtt protocol, setting the ModelId into ClientOptions, and open the connection.
        /// This method also sets a connection status change callback.
        /// </summary>
        private static async Task InitializeDeviceClientAsync()
        {
            var options = new ClientOptions
            {
                ModelId = ModelId,
            };

            // Initialize the device client instance using the device connection string, transport of Mqtt over TCP (with fallback to Websocket),
            // and the device ModelId set in ClientOptions.
            s_deviceClient = DeviceClient.CreateFromConnectionString(DeviceConnectionString, TransportType.Mqtt, options);

            // Register a connection status change callback, that will get triggered any time the device's connection status changes.
            s_deviceClient.SetConnectionStatusChangesHandler((status, reason) =>
            {
                PrintLog($"Connection status change registered - status={status}, reason={reason}");
            });

            // This will open the device client connection over Mqtt.
            await s_deviceClient.OpenAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// The desired property update callback, which receives the target temperture as a desired property update,
        /// and updates the current temperature value over telemetry and reported property update.
        /// </summary>
        /// <param name="desiredProperties">The target temperature update patch.</param>
        /// <param name="userContext">The user context supplied to the callback.</param>
        private static async Task TargetTemperatureUpdateCallbackAsync(TwinCollection desiredProperties, object userContext)
        {
            PrintLog($"Received an update for target temperature");
            PrintLog(desiredProperties.ToJson());

            double targetTemperature = GetPropertyFromTwin<double>(desiredProperties, "targettemperature");
            await UpdateCurrentTemperatureAsync(targetTemperature).ConfigureAwait(false);
        }

        // Send the current temperature over telemetry and reported property.
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
            PrintLog($"Sent current temperature {currentTemperature} over telemetry.");
        }

        // Update the temperature over telemetry and reported property, based on the target temperature update received.
        private static async Task UpdateCurrentTemperatureAsync(double targetTemperature)
        {
            // Send temperature update over telemetry.
            string telemetryName = "temperature";
            string telemetryPayload = $"{{ \"{telemetryName}\": {targetTemperature} }}";
            var message = new Message(Encoding.UTF8.GetBytes(telemetryPayload))
            {
                ContentEncoding = "utf-8",
                ContentType = "application/json",
            };

            await s_deviceClient.SendEventAsync(message).ConfigureAwait(false);
            PrintLog($"Sent current temperature {targetTemperature} over telemetry.");

            // Send temperature update over reported property.
            var reportedProperty = new TwinCollection();
            reportedProperty["currenttemperature"] = targetTemperature;
            await s_deviceClient.UpdateReportedPropertiesAsync(reportedProperty).ConfigureAwait(false);
            PrintLog($"Sent current temperature {targetTemperature} over reoprted property update.");
        }

        /// <summary>
        /// The callback to handle "reboot" command. This method will send a temeprature update (of 0) over telemetry, 
        /// and also reset the temperature property to 0.
        /// </summary>
        /// <param name="request">The command request. The can have an optional payload in it.</param>
        /// <param name="userContext">The user context supplied to the callback.</param>
        /// <returns></returns>
        private static async Task<MethodResponse> HandleRebootCommandAsync(MethodRequest request, object userContext)
        {
            PrintLog("Rebooting thermostat: resetting current temperature reading to 0.0");
            await UpdateCurrentTemperatureAsync(0).ConfigureAwait(false);

            return new MethodResponse(200);
        }

        private static T GetPropertyFromTwin<T>(TwinCollection collection, string propertyName)
        {
            return collection.Contains(propertyName) ? (T)collection[propertyName] : default;
        }

        private static void PrintLog(string message)
        {
            Console.WriteLine($">> {DateTime.Now}: {message}");
        }
    }
}
