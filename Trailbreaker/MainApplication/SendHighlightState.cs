using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace Trailbreaker.MainApplication
{
    public class SendHighlightState
    {

        public SendHighlightState(Socket client, string selectedcell)
        {
         
            byte[] msg = Encoding.UTF8.GetBytes(selectedcell);
            byte[] bytes = new byte[256];


            try
            {
                int byteCount = client.Send(msg, SocketFlags.None);
            }
            catch (SocketException e)
            {
                Debug.WriteLine(e.Message);

            }

        }




    }
}
