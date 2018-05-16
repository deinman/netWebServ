using System;

using WebServLib;

namespace WebServConsole
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Starting server:");
            Server.Start();

            Console.WriteLine("Server successfully started!");
            Console.ReadLine();
        }
    }
}
