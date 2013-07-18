using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Trailbreaker.RecorderApplication
{
    internal class Receiver
    {
        private readonly IPAddress ipAddress = Dns.GetHostEntry("localhost").AddressList[0];
        private readonly TcpListener listener;

        public Receiver(GUI gui, int port)
        {
            listener = new TcpListener(ipAddress, port);
            listener.Start();
            Debug.WriteLine("Waiting for actions.");
            while (true)
            {
                Thread.Sleep(100);
                Socket s = listener.AcceptSocket();
                var b = new byte[65535];
                int k = s.Receive(b);
                int open = 0;
                bool json = false;
                String jstring = "";
                for (int i = 0; i < k; i++)
                {
                    char chr = Convert.ToChar(b[i]);
                    if (chr == '{')
                    {
                        if (json)
                        {
                            open++;
                        }
                        json = true;
                    }
                    if (json)
                    {
                        jstring += chr;
                    }
                    if (chr == '}')
                    {
                        if (open == 0)
                        {
                            Debug.WriteLine(jstring);
                            json = false;
                            var jsonSerializer = new DataContractJsonSerializer(typeof (UserAction));
                            var stream = new MemoryStream(Encoding.UTF8.GetBytes(jstring));
                            var act = jsonSerializer.ReadObject(stream) as UserAction;
                            stream.Close();
                            gui.Invoke(new MethodInvoker(() => gui.AddAction(act)));
                        }
                        open--;
                    }
                }
                s.Close();
            }
        }
    }
}