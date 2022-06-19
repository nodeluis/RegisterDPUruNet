using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WatsonWebsocket;

namespace RegistroTickeo
{

    public class MyWebSocket
    {
        private int PORT = 3000;
        private static Dictionary<string, CancellationTokenSource> clients = new Dictionary<string, CancellationTokenSource>();

        public MyWebSocket(int port)
        {
            PORT = port;
            //WatsonWsServer server = new WatsonWsServer();
        }

        public WatsonWsServer StartServer()
        {
            Console.WriteLine("iniciando desde MyWebSocket: " + PORT.ToString());
            WatsonWsServer server = new WatsonWsServer("localhost", PORT);
            server.ClientConnected += ClientConnected;
            server.ClientDisconnected += ClientDisconnected;
            server.MessageReceived += MessageReceived;
            server.Start();
            return server;
        }

        public static void ClientConnected(object sender, ClientConnectedEventArgs args)
        {
            Console.WriteLine("Client connected: " + args.IpPort);
            if (!clients.Keys.Contains(args.IpPort))
            {
                CancellationTokenSource _tokenSource = new CancellationTokenSource();
                clients.Add(args.IpPort, _tokenSource);
            }

        }

        public static void ClientDisconnected(object sender, ClientDisconnectedEventArgs args)
        {
            Console.WriteLine("Client disconnected: " + args.IpPort);
            if (clients.Keys.Contains(args.IpPort))
            {
                clients[args.IpPort].Cancel();
                clients.Remove(args.IpPort);
            }
        }

        public static void MessageReceived(object sender, MessageReceivedEventArgs args)
        {
            Console.WriteLine("Message Received: " + Encoding.UTF8.GetString(args.Data));
        }

        public static void SendMessage(WatsonWsServer server, string message)
        {
            foreach (var client in clients)
            {
                try
                {
                    if (clients.ContainsKey(client.Key))
                    {
                        Console.WriteLine("Sending: " + message + " To: " + client.Key);
                        server.SendAsync(client.Key, Encoding.UTF8.GetBytes(message), client.Value.Token);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("envio incorrecto");
                }
            }
        }

    }
}
