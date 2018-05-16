using System;
using System.Reflection;
using Extensions;
using WebServLib;

namespace WebServConsole
{
    class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Starting server:");

            var websitePath = GetWebsitePath();
            Server.Start(websitePath);

            Console.WriteLine("Server successfully started!");
            Console.ReadLine();
        }

        public static string GetWebsitePath()
        {
            // Path of our exe.
            var websitePath = Assembly.GetExecutingAssembly().Location;
            websitePath = websitePath.LeftOfRightmostOf("\\").LeftOfRightmostOf("\\").LeftOfRightmostOf("\\") +
                          "\\Website";
            return websitePath;
        }
    }
}
