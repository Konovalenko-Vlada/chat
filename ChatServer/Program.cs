using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace ChatServer
{
    class Program
    {
        public class StateObject
        {
            public Socket WorkSocket = null;
            public const int BufferSize = 1024;
            public byte[] Buffer = new byte[BufferSize];
            public StringBuilder StringBuilder = new StringBuilder();
        }

        public class AsyncChatServer
        {
            /*
             * Сервак - асинхронный, т.е. может обрабатывать сразу-же сотни клиентов
             * без задержки. Это все возможно благодаря callback-ам
             * функциям, которые ожидают получения чего-либо и ManualResetEvent,
             * который делает всю оставщуюся магию
             */
            Char[] SPLIT_SEPARATOR = new Char[] { ' ' };

            Encoding Encoder = Encoding.UTF8;
            static List<String> ChatText = new List<String>() { "[server] Hello!\r\n" };
            public static ManualResetEvent AllDone = new ManualResetEvent(false);

            public void Start(IPEndPoint IpPoint)
            {
                Socket Listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                try
                {
                    Listener.Bind(IpPoint);
                    Listener.Listen(30);
                    Console.WriteLine("[INFO] Server started");

                    while (true)
                    {
                        AllDone.Reset();

                        Console.WriteLine("[INFO] Waiting for connection...");
                        Listener.BeginAccept(
                            new AsyncCallback(AcceptCallback),
                            Listener);

                        AllDone.WaitOne();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[EXCEPTION] " + ex.Message);
                }

                Console.WriteLine("[INFO] Press ENTER to exit");
                Console.ReadLine();
            }

            private void AcceptCallback(IAsyncResult ar)
            {
                AllDone.Set();

                Socket Listener = ar.AsyncState as Socket;
                Socket Handler = Listener.EndAccept(ar);

                StateObject State = new StateObject();
                State.WorkSocket = Handler;
                Handler.BeginReceive(
                    State.Buffer, 0,
                    StateObject.BufferSize, 0,
                    new AsyncCallback(ReadCallback),
                    State);
            }

            private void ReadCallback(IAsyncResult ar)
            {
                String Content = String.Empty;

                StateObject State = ar.AsyncState as StateObject;
                Socket Handler = State.WorkSocket;

                int BytesRead = Handler.EndReceive(ar);
                if (BytesRead > 0)
                {
                    State.StringBuilder.Append(Encoder.GetString(
                        State.Buffer, 0, BytesRead));
                    Content = State.StringBuilder.ToString();
                    if (Content.IndexOf("<EOF>") > 1)
                    {
                        Send(Handler, Content.Replace("<EOF>", ""));
                    }
                    else
                    {
                        Handler.BeginReceive(
                            State.Buffer, 0,
                            StateObject.BufferSize, 0,
                            new AsyncCallback(ReadCallback),
                            State);
                    }
                }
            }

            private void Send(Socket Handler, string Content)
            {
                String SendData = String.Empty;

                String[] Command = Content.Split(SPLIT_SEPARATOR, 2);
                Console.WriteLine("[INFO] Name: {0}; Params: {1};", Command);

                if (Command.Length < 2)
                {
                    SendData = "ERROR: you canot call command without args";
                }
                else
                {
                    /*
                     * Здесь сервер "понимает" команды самописного протокола.
                     * Результат записывается в переменную SendData, 
                     * которая потом уходить на send
                     */
                    switch (Command[0])
                    {
                        case "HISTORY":
                            if(Command[1] != "")
                            {
                                int Count = Convert.ToInt32(Command[1]);
                                Count--;
                                SendData = String.Join("\r\n", ChatText.Skip(Count));
                            }
                            else
                            {
                                SendData = String.Join("\r\n", ChatText);
                            }
                            break;
                        case "STORE":
                            ChatText.Add(Command[1]);
                            SendData = "OK";
                            break;
                        case "COUNT":
                            SendData = ChatText.Count.ToString();
                            break;
                        default:
                            SendData = "ERROR:no command recived";
                            break;
                    }
                }

                byte[] ByteData = Encoder.GetBytes(SendData);

                Handler.BeginSend(
                    ByteData, 0,
                    ByteData.Length, 0,
                    new AsyncCallback(SendCallback),
                    Handler);
            }

            private void SendCallback(IAsyncResult ar)
            {
                try
                {
                    Socket Handler = ar.AsyncState as Socket;
                    int BytesSent = Handler.EndSend(ar);

                    Handler.Shutdown(SocketShutdown.Both);
                    Handler.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[EXCEPTION] " + ex.Message);
                }
            }
        }

        static void Main(string[] args)
        {
            IPEndPoint IpPoint = new IPEndPoint(IPAddress.Parse("192.168.1.2"), 8805);

            AsyncChatServer Server = new AsyncChatServer();
            Server.Start(IpPoint);
        }
    }
}
