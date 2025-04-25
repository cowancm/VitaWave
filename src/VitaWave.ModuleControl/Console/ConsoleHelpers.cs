using System.IO.Ports;
using VitaWave.ModuleControl.Parsing;

namespace VitaWave.ModuleControl.Console
{
    public static class ConsoleHelpers
    {
        public static void OutputFancyLabel()
        {
            System.Console.ForegroundColor = ConsoleColor.Cyan;

            System.Console.WriteLine(@"

 __      __ _  __       ___         ___       
 \ \    / /|_| | |      \ \        / /                
  \ \  / /  _  | |______ \ \  /\  / /_ ___   _____   
   \ \/ /  | |/____/ _` | \ \/  \/ / _` \ \ / / _ \  
    \  /   | | | || (_| |  \  /\  / (_| |\ V /  __/  
     \/    |_| |_|\__,__|   \/  \/ \__,_| \_/ \___|  
                                                       
");

            System.Console.ForegroundColor = ConsoleColor.White;

            System.Console.ResetColor();
        }

        public static (string, string) GetComPorts()
        {
            string dataPort;
            string cliPort;

            var availPorts = SerialPort.GetPortNames().ToList();
            //= new List<string>() { "COM6", "COM31", "COM9" }; //testing purposes

            var portNum_PortName = new Dictionary<string, string>();

            System.Console.WriteLine("Current Available Ports:");

            foreach (string availPort in availPorts)
            {
                string portNum = new string(availPort.Where(char.IsDigit).ToArray());
                portNum_PortName.Add(portNum, availPort);
                System.Console.WriteLine($"{availPort} [{portNum}]");
            }
            System.Console.WriteLine();
            System.Console.WriteLine("Please select the data port.");

            while (true)
            {
                var selectionPortNum = System.Console.ReadLine()?.Trim() ?? string.Empty;

                if (portNum_PortName.TryGetValue(selectionPortNum, out var port))
                {
                    dataPort = port;
                    portNum_PortName.Remove(selectionPortNum);
                    break;
                }

                System.Console.WriteLine("Invalid selection.");
            }

            System.Console.WriteLine();
            System.Console.WriteLine("Please select the CLI port: ");

            while (true)
            {
                var selectionPortNum = System.Console.ReadLine()?.Trim() ?? string.Empty;

                if (portNum_PortName.TryGetValue(selectionPortNum, out var port))
                {
                    cliPort = port;
                    portNum_PortName.Remove(selectionPortNum);
                    break;
                }

                System.Console.WriteLine("Invalid selection.");
            }

            System.Console.WriteLine();

            System.Console.ForegroundColor = ConsoleColor.Cyan;
            System.Console.WriteLine("Selected Data: " + dataPort);
            System.Console.WriteLine("Selected CLI : " + cliPort);
            System.Console.ForegroundColor = ConsoleColor.White;

            System.Console.ResetColor();

            return (dataPort, cliPort);
        }

        public static bool AskAboutDataLogging()
        {
            System.Console.WriteLine("Would you like to save data? (y/n)");
            while (true)
            {
                var result = System.Console.ReadLine()?.Trim()?.ToLower() ?? string.Empty;

                if (result == "y")
                {
                    System.Console.WriteLine();
                    return true;
                }
                else if (result == "n")
                {
                    System.Console.WriteLine();
                    return false;
                }

                System.Console.WriteLine("Invalid response. Press (y/n)");
            }
        }

        public static void PrintTargetIndication(Event? e)
        {
            if (e?.Targets?.Count > 0)
            {
                System.Console.ForegroundColor = ConsoleColor.Green;
                System.Console.WriteLine("PRESENCE DETECTED: " + $"{e.Targets.Count} TARGETS");
                System.Console.ResetColor();
            }
            else
            {
                System.Console.ForegroundColor = ConsoleColor.Red;
                System.Console.WriteLine("NO PRESENCE DETECTED");
                System.Console.ResetColor();
            }
        }


    }
}
