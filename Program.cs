using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Rest;
using Newtonsoft.Json;
using Message = Microsoft.Azure.Devices.Client.Message;

namespace IOT_DEVICE_CRUD
{
    class Program
    {
        private const string DeviceConnectionString =
            "HostName=iot-crud.azure-devices.net;DeviceId=firstDevice;SharedAccessKey=B82XyN18FCFN+PgzPUXog2xGXCUjWSvGoAIoTM1mg3Y=";
        static string connectionString = "HostName=iot-crud.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=1z58ZmBtdb8dV0aguQNQcOc7pGY5MAUgeAIoTM0PwSk=";
        static string deviceId = "thirdDevice";
        static RegistryManager registryManager;

        private static TwinCollection _reportedProperties;

        static async Task Main(string[] args)
        {
            var serviceClient = ServiceClient.CreateFromConnectionString(connectionString);
            var feedbackTask = ReceiveFeedback(serviceClient);
            #region Get Device

            GetDeviceIdAsync().Wait();

            #endregion Get Device End

            #region Add Device

            registryManager = RegistryManager.CreateFromConnectionString(connectionString);
            AddDeviceAsync().Wait();
            Console.ReadLine();

            #endregion Add Device End

            #region Delete Device

            string devicesToDelete = deviceId;
            
            await registryManager.RemoveDeviceAsync(devicesToDelete);

            #endregion Delete Device

            #region Device reported properties update & send D2C messages
            Console.WriteLine("Initializing...");

            var device = DeviceClient.CreateFromConnectionString(DeviceConnectionString);

            await device.OpenAsync();

            Console.WriteLine("Device is connected!");

            var receiveEventsTask = ReceiveEvents(device);

            await UpdateTwin(device);

            Console.WriteLine("Press a key to perform an action:");
            Console.WriteLine("q: quits");
            Console.WriteLine("h: send happy feedback");
            Console.WriteLine("u: send unhappy feedback");
            Console.WriteLine("e: request emergency help");

            var random = new Random();
            var quitRequested = false;
            while (!quitRequested)
            {
                Console.Write("Action? ");
                var input = Console.ReadKey().KeyChar;
                Console.WriteLine();

                var status = StatusType.NotSpecified;
                var latitude = random.Next(0, 100);
                var longitude = random.Next(0, 100);

                switch (Char.ToLower(input))
                {
                    case 'q':
                        quitRequested = true;
                        break;
                    case 'h':
                        status = StatusType.Happy;
                        break;
                    case 'u':
                        status = StatusType.Unhappy;
                        break;
                    case 'e':
                        status = StatusType.Emergency;
                        break;
                }

                var telemetry = new Telemetry
                {
                    Latitude = latitude,
                    Longitude = longitude,
                    Status = status
                };

                var payload = JsonConvert.SerializeObject(telemetry);

                var message = new Message(Encoding.ASCII.GetBytes(payload));

                await device.SendEventAsync(message);

                Console.WriteLine("Message sent!");

                #endregion Device reported properties update & send D2C messages
            }

            Console.WriteLine("Disconnecting...");

        }

        private static async Task UpdateTwin(DeviceClient device)
        {
            _reportedProperties = new TwinCollection();
            _reportedProperties["firmwareVersion"] = "1.0";
            _reportedProperties["firmwareUpdateStatus"] = "n/a";

            await device.UpdateReportedPropertiesAsync(_reportedProperties);
        }
        private static async Task AddDeviceAsync()
        {
            Device device;
            try
            {
                device = await registryManager.AddDeviceAsync(new Device(deviceId));
            }
            catch (DeviceAlreadyExistsException)
            {
                device = await registryManager.GetDeviceAsync(deviceId);
            }
            Console.WriteLine("Generated device key: {0}", device.Authentication.SymmetricKey.PrimaryKey);
        }

        private static async Task GetDeviceIdAsync()
        {
            RegistryManager registryManager =
              RegistryManager.CreateFromConnectionString(connectionString);
            try
            {

                var query = registryManager.CreateQuery("SELECT * FROM devices", 100);
                while (query.HasMoreResults)
                {
                    var page = await query.GetNextAsTwinAsync();
                    foreach (var twin in page)
                    {
                        Console.WriteLine(JsonConvert.SerializeObject(twin.DeviceId));
                    }
                }
            }
            catch (DeviceAlreadyExistsException dvcEx)
            {
                Console.WriteLine("Error : {0}", dvcEx);
            }

        }
        private static async Task ReceiveFeedback(ServiceClient serviceClient)
        {
            var feedbackReceiver = serviceClient.GetFeedbackReceiver();
           
            CancellationToken cancellationToken = CancellationToken.None;
            while (true)
            {
                var feedbackBatch = await feedbackReceiver.ReceiveAsync(cancellationToken);

                if (feedbackBatch == null)
                {
                    continue;
                }

                foreach (var record in feedbackBatch.Records)
                {
                    var messageId = record.OriginalMessageId;
                    var statusCode = record.StatusCode;

                    Console.WriteLine($"Feedback for message '{messageId}', status code: {statusCode}.");

                }

                await feedbackReceiver.CompleteAsync(feedbackBatch);
            }
        }

        private static async Task ReceiveEvents(DeviceClient device)
        {
            while (true)
            {
                var message = await device.ReceiveAsync();

                if (message == null)
                {
                    continue;
                }

                var messageBody = message.GetBytes();

                var payload = Encoding.ASCII.GetString(messageBody);

                Console.WriteLine($"Received message from cloud: '{payload}'");

                await device.CompleteAsync(message);
            }
        }

    }
}

