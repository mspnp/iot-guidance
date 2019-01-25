namespace Fabrikam.DroneDelivery.IoT.ConsoleHost
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;

    public static class ConsoleHost
    {
        public static async Task WithOptions(Dictionary<string, Func<CancellationToken, Task>> actions)
        {
            while (true)
            {
                var tokenSource = new CancellationTokenSource();

                using (Color(ConsoleColor.Yellow))
                {
                    Console.WriteLine();
                    Console.WriteLine(Assembly.GetEntryAssembly().GetName().Name);
                    Console.WriteLine();
                }

                actions.Keys.Select((title, index) => new { title, index })
                    .ToList()
                    .ForEach(t => Console.WriteLine("[{0}] {1}", t.index + 1, t.title));

                Console.WriteLine();
                Console.Write("Select an option: ('q' to quit)");

                var key = Console.ReadKey().KeyChar.ToString(CultureInfo.InvariantCulture);
                Console.WriteLine();

                if (!int.TryParse(key, out int option))
                {
                    Environment.Exit(0);
                }

                option--;

                var selection = actions.ToList()[option];

                Console.Write("executing ");
                using (Color(ConsoleColor.Green))
                {
                    Console.WriteLine(selection.Key);
                }

                await selection
                    .Value(tokenSource.Token)
                    .ContinueWith(ReportTaskStatus);

                using (Color(ConsoleColor.DarkGreen))
                {
                    Console.WriteLine("press `q` to signal termination");
                }

                var input = Console.ReadKey();
                if (input.KeyChar == 'q')
                {
                    using (Color(ConsoleColor.DarkGreen))
                    {
                        Console.WriteLine();
                        Console.WriteLine("termination signal sent...press any key to go back to previous menu");
                    }
                    tokenSource.Cancel();
                }

                Console.ReadKey();
                Console.Clear();
            }
        }

        private static void ReportTaskStatus(Task task)
        {
            if (task.IsFaulted)
            {
                using (Color(ConsoleColor.Red))
                {
                    Console.WriteLine("an exception occurred");
                }
                Console.WriteLine(task.Exception);
            }
            else if (task.IsCanceled)
            {
                using (Color(ConsoleColor.DarkYellow))
                {
                    Console.WriteLine("cancelled");
                }
            }
            else
            {
                using (Color(ConsoleColor.Blue))
                {
                    Console.WriteLine("completed successfully");
                }
            }

            using (Color(ConsoleColor.DarkGreen))
            {
                Console.WriteLine("press any key to return to the menu");
            }
        }

        public static IDisposable Color(ConsoleColor color)
        {
            var original = Console.ForegroundColor;
            Console.ForegroundColor = color;

            return Disposable.Create(
                () => Console.ForegroundColor = original
                );
        }

        public static class Disposable
        {
            //TODO: better to use the Rx implementation
            public static IDisposable Create(Action whenDisposingAction)
            {
                return new ActionOnDispose(whenDisposingAction);
            }

            private class ActionOnDispose : IDisposable
            {
                private readonly Action _action;

                internal ActionOnDispose(Action action)
                {
                    _action = action;
                }

                public void Dispose()
                {
                    _action();
                }
            }
        }
    }
}
