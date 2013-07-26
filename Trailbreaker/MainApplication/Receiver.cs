using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Trailbreaker.MainApplication
{
    public class Receiver
    {
        private readonly IPAddress ipAddress = Dns.GetHostEntry("localhost").AddressList[0];
        private readonly TcpListener listener;
        private readonly Socket socket;

        private TrailbreakerReceiverForm gui;

        private byte[] bytes = new byte[65535];
        private int numBytes;

        public Receiver(TrailbreakerReceiverForm gui, int port)
        {
            this.gui = gui;

            string read;

            listener = new TcpListener(ipAddress, port);
            listener.Start();

            Debug.WriteLine("Waiting for actions.");

            while (true)
            {
                Thread.Sleep(100);

                socket = listener.AcceptSocket();

                numBytes = socket.Receive(bytes);

                read = "";

                for (int i = 0; i < numBytes; i++)
                {
                    char chr = Convert.ToChar(bytes[i]);
                    read += chr;
                    //Newline (\n) is 10
                    if (bytes[i] == 10)
                    {
                        if (read.StartsWith("Accept:"))
                        {
                            if (read.Contains("application/json"))
                            {
                                FindJson(i);
                                break;
                            }else if (read.Contains("text/plain"))
                            {
                                gui.Invoke(new MethodInvoker(() => gui.AddCharacter(Convert.ToChar(bytes[numBytes - 1]))));
                                break;
                            }
                        }
                        read = "";
                    }
                }

                socket.Close();
            }
        }

        private void FindJson(int startFrom)
        {
            int open = 0;
            bool json = false;
            string jstring = "";

            for (int i = startFrom; i < numBytes; i++)
            {
                char chr = Convert.ToChar(bytes[i]);
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
                        var jsonSerializer = new DataContractJsonSerializer(typeof(UserAction));
                        var stream = new MemoryStream(Encoding.UTF8.GetBytes(jstring));
                        try
                        {
                            var act = jsonSerializer.ReadObject(stream) as UserAction;
                            stream.Close();
                            gui.Invoke(new MethodInvoker(() => gui.AddAction(act)));
                        }
                        catch (SerializationException e)
                        {
                            MessageBox.Show(
                                "You need to reload your Trailbreaker Extension AND/OR refresh the current page in Chrome in order to record elements!",
                                "Serialization Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                            Debug.WriteLine(e.Message);
                        }
                    }
                    open--;
                }
            }
        }
    }
}