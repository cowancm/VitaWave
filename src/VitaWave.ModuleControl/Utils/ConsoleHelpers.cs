using System.IO.Ports;
using VitaWave.ModuleControl.Parsing;

namespace VitaWave.ModuleControl.Utils
{
    public static class ConsoleHelpers
    {
        public static void OutputFancyLabel()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;

            Console.WriteLine(@"

 __      __ _  __       ___         ___       
 \ \    / /|_| | |      \ \        / /                
  \ \  / /  _  | |______ \ \  /\  / /_ ___   _____   
   \ \/ /  | |/____/ _` | \ \/  \/ / _` \ \ / / _ \  
    \  /   | | | || (_| |  \  /\  / (_| |\ V /  __/  
     \/    |_| |_|\__,__|   \/  \/ \__,_| \_/ \___|  
                                                       
");

            Console.ForegroundColor = ConsoleColor.White;

            Console.ResetColor();
        }

        public static(string, string) GetComPorts()
        {
            string dataPort;
            string cliPort;

            var availPorts = SerialPort.GetPortNames().ToList();
                           //= new List<string>() { "COM6", "COM31", "COM9" }; //testing purposes

            var portNum_PortName = new Dictionary<string, string>();

            Console.WriteLine("Current Available Ports:");

            foreach (string availPort in availPorts)
            {
                string portNum = new string(availPort.Where(char.IsDigit).ToArray());
                portNum_PortName.Add(portNum, availPort);
                Console.WriteLine($"{availPort} [{portNum}]");
            }
            Console.WriteLine();
            Console.WriteLine("Please select the data port.");

            while (true)
            {
                var selectionPortNum = Console.ReadLine()?.Trim() ?? string.Empty;

                if (portNum_PortName.TryGetValue(selectionPortNum, out var port))
                {
                    dataPort = port;
                    portNum_PortName.Remove(selectionPortNum);
                    break;
                }

                Console.WriteLine("Invalid selection.");
            }

            Console.WriteLine();
            Console.WriteLine("Please select the CLI port: ");

            while (true)
            {
                var selectionPortNum = Console.ReadLine()?.Trim() ?? string.Empty;

                if (portNum_PortName.TryGetValue(selectionPortNum, out var port))
                {
                    cliPort = port;
                    portNum_PortName.Remove(selectionPortNum);
                    break;
                }

                Console.WriteLine("Invalid selection.");
            }

            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Selected Data: " + dataPort);
            Console.WriteLine("Selected CLI : " + cliPort);
            Console.ForegroundColor = ConsoleColor.White;

            Console.ResetColor();

            return (dataPort, cliPort);
        }

        public static bool AskAboutDataLogging()
        {
            Console.WriteLine("Would you like to save data? (y/n)");
            while (true)
            {
                var result = Console.ReadLine()?.Trim()?.ToLower() ?? string.Empty;

                if (result == "y")
                {
                    Console.WriteLine();
                    return true;
                }
                else if (result == "n")
                {
                    Console.WriteLine();
                    return false;
                }

                Console.WriteLine("Invalid response. Press (y/n)");
            }
        }

        public static void PrintTargetIndication(Event? e)
        {
            if (e?.Targets?.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("PRESENCE DETECTED: " + $"{e.Targets.Count} TARGETS");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("NO PRESENCE DETECTED");
                Console.ResetColor();
            }
        }

        
    }
}
