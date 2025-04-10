using System.IO.Ports;

namespace ModuleControl.Utils
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

            var availPorts //= SerialPort.GetPortNames().ToList();
                           = new List<string>() { "COM6", "COM31", "COM9" };

            var portNum_PortName = new Dictionary<string, string>();

            Console.WriteLine("Select the data port: ");

            foreach (string availPort in availPorts)
            {
                string portNum = new string(availPort.Where(char.IsDigit).ToArray());
                portNum_PortName.Add(portNum, availPort);
                Console.WriteLine($"{availPort} [{portNum}]");
            }

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

            Console.WriteLine("Select the CLI port: ");

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
            Console.WriteLine("Would you like to save data?");

            //WIP

            return false;
        }

        
    }
}
