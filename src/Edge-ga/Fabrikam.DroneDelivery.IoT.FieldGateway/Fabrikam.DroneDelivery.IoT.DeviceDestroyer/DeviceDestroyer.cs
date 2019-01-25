namespace Fabrikam.DroneDelivery.IoT.DeviceDestroyer
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices;
    
    public class DeviceDestroyer
    {
        private const int PAGING_SIZE = 500;
        public static async Task DeleteAllDevicesAsync(string connectionString, int pagingSize=PAGING_SIZE)
        {
            var registryManager = RegistryManager.CreateFromConnectionString(connectionString);
            var stopWatch = Stopwatch.StartNew();
            var counter = 0;
            var cancellationToken = new CancellationTokenSource().Token;
            while (true)
            {
                var query = registryManager.CreateQuery("SELECT * FROM devices", pagingSize);
                if (!query.HasMoreResults)
                {
                    stopWatch.Stop();
                    break;
                }
                while (query.HasMoreResults)
                {
                    var page = await query.GetNextAsTwinAsync();
                    Parallel.ForEach<Microsoft.Azure.Devices.Shared.Twin>(page, async twin =>
                    {
                        string deviceId = twin.DeviceId;
                        try
                        {
                            await registryManager.RemoveDeviceAsync(twin.DeviceId, cancellationToken).ConfigureAwait(false);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"Exception of type {e.GetType().ToString()} occured. Message is {e.Message}.");
                        }

                        Interlocked.Increment(ref counter);
                        Console.WriteLine(deviceId + " deleted!");
                    });
                }
            }

            var timeElapsed = stopWatch.Elapsed;
            Console.WriteLine($"Total time spent in deleting {counter} devices - {timeElapsed.Hours}:{timeElapsed.Minutes}:{timeElapsed.Seconds}");
            stopWatch.Stop();
            Console.Read();
        }
    }
}
