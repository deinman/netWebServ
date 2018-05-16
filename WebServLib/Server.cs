using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Extensions;

namespace WebServLib
{
    public static class Server
    {
        //private static HttpListener _listener;
        private static Router _router = new Router();

        /// <summary>
        /// Returns list of IP addresses assigned to localhost network devices,
        /// such as hardwired ethernet, wireless, etc.
        /// </summary>
        /// <returns>List of IP addresses</returns>
        private static List<IPAddress> GetLocalHostIPs()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            var ret = host.AddressList.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork).ToList();

            return ret;
        }

        private static HttpListener InitializeListener(List<IPAddress> localhostIPs)
        {
            var listener = new HttpListener();
            listener.Prefixes.Add("http://localhost/");

            // Listen to IP address as well.
            localhostIPs.ForEach(ip =>
            {
                Console.WriteLine($"Listening on IP http://{ip.ToString()}/");
                listener.Prefixes.Add($"http://{ip.ToString()}/");
            });

            return listener;
        }

        public static int MaxSimultaneousConnections = 20;
        private static Semaphore sem = new Semaphore(MaxSimultaneousConnections, MaxSimultaneousConnections);

        /// <summary>
        /// Begin listening to connections on a separate worker thread.
        /// </summary>
        /// <param name="listener"></param>
        private static void Start(HttpListener listener)
        {
            listener.Start();
            Task.Run(() => RunServer(listener));
        }

        /// <summary>
        /// Start awaiting connections, up to max value.
        /// This code runs in a separate thread.
        /// </summary>
        /// <param name="listener"></param>
        private static void RunServer(HttpListener listener)
        {
            while (true)
            {
                sem.WaitOne();
                StartConnectionListener(listener);
            }
            // ReSharper disable once FunctionNeverReturns
        }

        /// <summary>
        /// Await connections.
        /// </summary>
        /// <param name="listener"></param>
        private static async void StartConnectionListener(HttpListener listener)
        {
            // Wait for a conneciton. Return to caller while waiting.
            var context = await listener.GetContextAsync();

            // Release the semaphore so another listener can start.
            sem.Release();

            // We have a connection!
            Log(context.Request);

            var request = context.Request;
            var path = request.RawUrl.LeftOf("?");
            var verb = request.HttpMethod;
            var parms = request.RawUrl.RightOf("?");
            var kvParams = GetKeyValues(parms);

            var resp = _router.Route(verb, path, kvParams);
            Respond(context.Response, resp);
            /* // This is the old way.
            var response = "Hello Browser!";
            var encoded = Encoding.UTF8.GetBytes(response);

            context.Response.ContentLength64 = encoded.Length;
            context.Response.OutputStream.Write(encoded, 0, encoded.Length);
            context.Response.OutputStream.Close();
            */
        }

        private static void Respond(HttpListenerResponse response, ResponsePacket resp)
        {
            response.ContentType = resp.ContentType;
            response.ContentLength64 = resp.Data.Length;
            response.OutputStream.Write(resp.Data, 0, resp.Data.Length);
            response.ContentEncoding = resp.Encoding;
            response.StatusCode = (int) HttpStatusCode.OK;
            response.OutputStream.Close();
        }

        public static void Start(string websitePath)
        {
            _router.WebsitePath = websitePath;
            var localHostIPs = GetLocalHostIPs();
            var listener = InitializeListener(localHostIPs);
            Start(listener);
        }

        public static void Log(HttpListenerRequest request)
        {
            var s = new StringBuilder();

            s.Append(request.RemoteEndPoint + " ");
            s.Append(request.HttpMethod + " /");
            s.Append(request.Url.AbsoluteUri.RightOf('/', 3));

            Console.WriteLine(s.ToString());
        }

        private static Dictionary<string, string> GetKeyValues(string data, Dictionary<string, string> kv = null)
        {
            kv.IfNull(() => kv = new Dictionary<string, string>());
            data.If(d => d.Length > 0,
                d => d.Split('&').ForEach(keyValue => kv[keyValue.LeftOf('=')] = keyValue.RightOf('=')));
            return kv;
        }
    }
}
