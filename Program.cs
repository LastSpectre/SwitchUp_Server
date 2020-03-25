using System.Net;
using System;

namespace BA_Praxis_Server
{
    class Program
    {
        static void Main(string[] args)
        {
            // Get all IPs
            string HostName = Dns.GetHostName();
            IPAddress[] iPAddresses = Dns.GetHostAddresses(HostName);

            // print out all IPs
            Console.WriteLine("IPs:");

            foreach (var ip in iPAddresses)
            {
                Console.WriteLine(ip.ToString());
            }

            // create new server and start
            Server server = new Server();
            server.Start(IPAddress.Parse("0.0.0.0"), 15151);
        }
    }
}