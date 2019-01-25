namespace BatchingModule
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.Loader;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Client.Transport.Mqtt;

    class Program
    {
        static long counter;
        private const int DefaultBatchSize = 10;
        private static ICollection<Message> messages;
        private const string IoTHubMessagesBatchSize = "UpstreamRelayBatchSize";

        static void PrintEnv()
        {
            foreach (DictionaryEntry kv in System.Environment.GetEnvironmentVariables())
            {
                Console.WriteLine($"{kv.Key}:{kv.Value}");
            }
        }

        static void Main(string[] args)
        {
            if (!int.TryParse(Environment.GetEnvironmentVariable(IoTHubMessagesBatchSize), out int batchSize))
            {
                batchSize = DefaultBatchSize;
            }

            messages = new List<Message>(batchSize);
            
            // For debug purposes, print env vars
            PrintEnv();

            InitAsync(batchSize).Wait();

            // Wait until the app unloads or is cancelled
            var cts = new CancellationTokenSource();
            AssemblyLoadContext.Default.Unloading += (ctx) => cts.Cancel();
            Console.CancelKeyPress += (sender, cpe) => cts.Cancel();

            WhenCancelled(cts.Token).Wait(cts.Token);
        }

        /// <summary>
        /// Handles cleanup operations when app is cancelled or unloads
        /// </summary>
        public static Task WhenCancelled(CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).SetResult(true), tcs);

            return tcs.Task;
        }

        /// <summary>
        /// Initializes the ModuleClient and sets up the callback to receive
        /// messages containing temperature information
        /// </summary>
        static async Task InitAsync(int batchSize)
        {
            MqttTransportSettings mqttSetting = new MqttTransportSettings(TransportType.Mqtt_Tcp_Only);
            ITransportSettings[] settings = { mqttSetting };

            // Open a connection to the Edge runtime
            ModuleClient ioTHubModuleClient = await ModuleClient.CreateFromEnvironmentAsync(settings).ConfigureAwait(false);
            await ioTHubModuleClient.OpenAsync().ConfigureAwait(false);
            Console.WriteLine("IoT Hub module client initialized.");

            // Note the use of Tuple to pass multiple objects
            var userContext = new Tuple<ModuleClient, int>(ioTHubModuleClient, batchSize);

            // Register callback to be called when a message is received by the module
            await ioTHubModuleClient.SetInputMessageHandlerAsync("input1", BatchMessagesAsync, userContext).ConfigureAwait(false);
        }

        /// <summary>
        /// This method is called whenever the module is sent a message from the EdgeHub. 
        /// It just pipe the messages without any change.
        /// It prints all the incoming messages.
        /// </summary>
        static async Task<MessageResponse> BatchMessagesAsync(Message message, object userContext)
        {
            var counterValue = Interlocked.Increment(ref counter);
            var userContextValues = userContext as Tuple<ModuleClient, int>;
            if (userContextValues == null)
            {
                throw new InvalidOperationException("UserContext doesn't contain expected values");
            }

            try
            {
                var moduleClient = userContextValues.Item1;
                if (counterValue >= userContextValues.Item2)
                {
                    Console.WriteLine($"Sending a batch of {counterValue} messages to Iot Hub...");
                    await moduleClient.SendEventBatchAsync("output1", messages);
                    Interlocked.Exchange(ref counter, 0);
                    messages.Clear();
                }
                else
                {
                    byte[] messageBytes = message.GetBytes();
                    string messageString = Encoding.UTF8.GetString(messageBytes);
                    Console.WriteLine($"Received message: {counterValue}, Body: [{messageString}]");

                    if (!string.IsNullOrEmpty(messageString))
                    {
                        var msg = new Message(messageBytes);
                        foreach (var prop in message.Properties)
                        {
                            msg.Properties[prop.Key] = prop.Value;
                        }

                        messages.Add(msg);
                    }
                }
                return MessageResponse.Completed;
            }
            catch (AggregateException ex)
            {
                foreach (Exception exception in ex.InnerExceptions)
                {
                    Console.WriteLine("Error: {0}", exception);
                }
                // Indicate that the message treatment is not completed.
                return MessageResponse.Abandoned;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);

                // Indicate that the message treatment is not completed.
                return MessageResponse.Abandoned;
            }
        }
    }
}
