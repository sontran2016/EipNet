using EIPNET.EIP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EipNetServer
{
    class Program
    {
        static void Main(string[] args)
        {
            StartListening();
        }

        //private static string _server = "192.168.1.64";//"192.168.1.117";
        private static int _port = 44818;
        private static bool _stop = false;
        private const int _bufferSize = 1024;
        public static void StartListening()
        {
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = ipHostInfo.AddressList.First(x => x.AddressFamily == AddressFamily.InterNetwork);
            string _server = ipAddress.ToString();
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Parse(_server), _port);
            // Create a TCP/IP socket.
            Socket listener = new Socket(AddressFamily.InterNetwork,SocketType.Stream, ProtocolType.Tcp);

            // Bind the socket to the local endpoint and listen for incoming connections.
            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(10);
                Console.WriteLine("Server started");
                Console.WriteLine("Server IP: {0}",_server);
                var tListener = new Thread(() =>
                 {
                     while (!_stop)
                     {
                         Console.WriteLine("Waiting for a new connection...");
                         Socket handler = listener.Accept();
                         Console.WriteLine("A client connected: "+ ((System.Net.IPEndPoint)handler.RemoteEndPoint).Address);

                         var tReceive=new Thread(new ParameterizedThreadStart(BeginReceiveData));
                         tReceive.Start(handler);
                     }                     
                });
                tListener.Start();

                //Console.WriteLine("Press exit");
                //Console.ReadLine();
                //_stop = true;
                //listener.Shutdown( SocketShutdown.Both);
                //listener.Close();
                //tListener.Join();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Console.Read();
            }            
        }

        private static void BeginReceiveData(object handler)
        {
            EncapsPacket request;
            do
            {
                var objHandler = handler as Socket;
                request = ReceiveData(objHandler);
                if(request.Length==0 && request.Command!= (ushort)EncapsCommand.UnRegisterSession) continue;
                string txt;
                var remotePoint = ((System.Net.IPEndPoint)objHandler.RemoteEndPoint).Address;
                switch (request.Command)
                {
                    case (ushort)EncapsCommand.RegisterSession:
                        txt = string.Format("{0} {1}, {2}", remotePoint, EncapsCommand.RegisterSession, "success");
                        break;
                    case (ushort)EncapsCommand.UnRegisterSession:
                        txt = string.Format("{0} {1}", remotePoint, EncapsCommand.UnRegisterSession);
                        break;
                    default:
                        txt = GetData(request.EncapsData);
                        txt = string.Format("{0} {1}, {2}", remotePoint, request.Command, txt);
                        break;
                }
                Console.WriteLine(txt);
                ReplyData(objHandler, txt);
            } while (!_stop && request.Command!= (ushort)EncapsCommand.UnRegisterSession);
        }

        private static EncapsPacket ReceiveData(Socket handler)
        {
            var bytes = new byte[_bufferSize];
            var bytesRec = handler.Receive(bytes);
            var request = new EncapsPacket();
            int tmp;
            if(bytesRec>0)
                request.Expand(bytes, 0, out tmp);
            return request;
        }
        private static void ReplyData(Socket handler, string data)
        {
            byte[] bytes = Encoding.Default.GetBytes(data);
            var reply = EncapsPacketFactory.CreateNOP(bytes);
            //var reply = new EncapsPacket();
            //reply.EncapsData = bytes;
            //reply.Length = (ushort)data.Length;
            //reply.Command = (ushort) EncapsCommand.NOP;
            reply.Status = (uint)EncapsStatusCode.Success;
            var msg = reply.Pack();
            handler.Send(msg);
        }
        private static string GetData(byte[] bytes)
        {
            var txt = Encoding.Default.GetString(bytes, 0, bytes.Length);
            return txt;
        }
    }
}
