using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SteamRcon
{
    public class RconClient
    {
        private TcpClient tcpClient = new TcpClient();
        public List<String> Log = new List<String>();
        public bool Connected
        {
            get { return tcpClient.Connected; }
            set { if (!value) { if (tcpClient.Connected) tcpClient.Close(); } }
        }

        public bool Authorized
        {
            get;
            private set;
        }
        public RconClient()
        {
        }

        public void Connect(String address, int port)
        {
            IPAddress IPAddr;
            if (IPAddress.TryParse(address, out IPAddr))
            {
                try
                {
                    tcpClient.Connect(IPAddr, port);
                }
                catch (Exception e) { WriteToLog(e.Message); }
            }
        }

        public void Disconnect()
        {
            Connected = false;
        }

        public void Authorize(String rconPasswd)
        {
            if (tcpClient.Connected)
            {
                try
                {
                    MemoryStream packetStream = new MemoryStream();
                    int id = 0; //UniqueID
                    byte[] asciiCommand = Encoding.ASCII.GetBytes(rconPasswd + "\0");

                    int packetSize = asciiCommand.Length + 9;

                    // packetStream.Write()
                    packetStream.Write(BitConverter.GetBytes(packetSize), 0, 4);
                    packetStream.Write(BitConverter.GetBytes(id), 4, 4);
                    packetStream.Write(BitConverter.GetBytes((int)PacketType.SERVERDATA_AUTH), 8, 4);
                    packetStream.Write(asciiCommand, 12, asciiCommand.Length);

                    tcpClient.GetStream().Write(packetStream.ToArray(), 0, (int)packetStream.Length);
                }
                catch (Exception e) { WriteToLog(e.Message); }
            }
        }

        public bool SendCommand(String command)
        {
            if (tcpClient.Connected)
            {
                try
                {
                    MemoryStream packetStream = new MemoryStream();
                    int id = 0; //UniqueID
                    byte[] asciiCommand = Encoding.ASCII.GetBytes(command + "\0");

                    int packetSize = asciiCommand.Length + 9;

                    // packetStream.Write()
                    packetStream.Write(BitConverter.GetBytes(packetSize), 0, 4);
                    packetStream.Write(BitConverter.GetBytes(id), 4, 4);
                    packetStream.Write(BitConverter.GetBytes((int)PacketType.SERVERDATA_EXECCOMMAND), 8, 4);
                    packetStream.Write(asciiCommand, 12, asciiCommand.Length);

                    tcpClient.GetStream().Write(packetStream.ToArray(), 0, (int)packetStream.Length);

                    return true;
                }
                catch (Exception e) { WriteToLog(e.Message); }
            }

            return false;
        }
        private void WriteToLog(String message)
        {
            Log.Add(message);
        }

        private enum PacketType : int
        {
            SERVERDATA_AUTH = 3, SERVERDATA_AUTH_RESPONSE = 2, SERVERDATA_EXECCOMMAND = 2, SERVERDATA_RESPONSE_VALUE = 0
        };
    }
}
