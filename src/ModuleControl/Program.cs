using ModuleControl.Communication;
using System.IO.Ports;

namespace ModuleControl
{
    class Program
    {
        static void Main(string[] args)
        {
            using CancellationTokenSource cts = new CancellationTokenSource();
            var coms = GetDataCliComsFromUser();

            using ModuleIO moduleIO = new ModuleIO(coms.Item1, coms.Item2, cts.Token);
           
            Task processingTask = Task.Run(() => moduleIO.PollSerial());

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();

            cts.Cancel();
            try
            {
                processingTask.Wait();
            }
            catch (AggregateException ae)
            {
                foreach (var ex in ae.InnerExceptions)
                {
                    Console.WriteLine($"Exception: {ex.Message}");
                }
            }
        }

        public static (string, string) GetDataCliComsFromUser()
        {
            var serialPortNames = SerialPort.GetPortNames().ToList();

            if (serialPortNames.Count < 2)
            {
                Console.WriteLine("Check that the device is plugged in and at least two COM ports are available.");
                return (string.Empty, string.Empty);
            }

            Console.WriteLine("Select your DATA COM port:");
            for (int i = 0; i < serialPortNames.Count; i++)
            {
                Console.WriteLine($"[{i}]: {serialPortNames[i]}");
            }

            int dataChoiceAsInt = GetUserChoice(serialPortNames.Count);

            Console.WriteLine("Select your CLI COM port:");
            for (int i = 0; i < serialPortNames.Count; i++)
            {
                if (i == dataChoiceAsInt) Console.WriteLine($"[{i}]: {serialPortNames[i]} (already chosen as DATA)");
                else Console.WriteLine($"[{i}]: {serialPortNames[i]}");
            }

            int cliChoiceAsInt;
            while (true)
            {
                cliChoiceAsInt = GetUserChoice(serialPortNames.Count);
                if (cliChoiceAsInt == dataChoiceAsInt)
                {
                    Console.WriteLine("CLI COM port cannot be the same as DATA COM port. Please select a different one.");
                }
                else
                {
                    break;
                }
            }

            var dataComName = serialPortNames[dataChoiceAsInt];
            var cliComName = serialPortNames[cliChoiceAsInt];
            return (dataComName, cliComName);
        }

        private static int GetUserChoice(int maxIndex)
        {
            int choice;
            while (true)
            {
                Console.Write("Enter your choice: ");
                string input = Console.ReadLine()?.Trim() ?? "";

                if (int.TryParse(input, out choice) && choice >= 0 && choice < maxIndex)
                    return choice;

                Console.WriteLine("Invalid selection. Please enter a valid number.");
            }
        }
    }
}