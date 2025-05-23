﻿using System.IO.Ports;

namespace VitaWave.ModuleControl.Utils
{
    public static class SerialPortsExtensions
    {

        //dotnets implementation of serial.read really sucks
        public static int ReadExact(this SerialPort serial, byte[] buffer, int offset, int count)
        {
            int bytesRead = 0;
            int totalBytesRead = 0;

            while (totalBytesRead < count)
            {
                bytesRead = serial.Read(buffer, offset + totalBytesRead, count - totalBytesRead);
                if (bytesRead == 0)
                    throw new IOException("Serial port read returned 0 bytes unexpectedly");
                totalBytesRead += bytesRead;
            }

            return totalBytesRead;
        }
    }
}
