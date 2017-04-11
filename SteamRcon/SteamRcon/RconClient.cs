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
        private Action<string> dataReceived;
        private static int readBufferSize = 1000;
        private byte[] readBuffer = new byte[readBufferSize];

        private List<byte> receivedBytes = new List<byte>();

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
        public RconClient(Action<string> receiveDataCallback)
        {
            dataReceived = receiveDataCallback;
        }

        public void Connect(IPAddress address, int port)
        {
            if (port < 1 || port > 65535)
                throw new InvalidOperationException("Must be a valid port.");
            try
            {
                tcpClient.Connect(address, port);

                StateObject so = new StateObject();
                tcpClient.GetStream().BeginRead(so.ReadBuffer, 0, so.ReadBuffer.Length, OnSocketRead, so);
            }
            catch (Exception e) { throw e; }
        }

        public void Disconnect()
        {
            Connected = false;
        }

        private void OnSocketRead(IAsyncResult ar)
        {
            var readStream = tcpClient.GetStream();
            StateObject so = (StateObject)ar.AsyncState;

            var bytesRead = readStream.EndRead(ar);
            so.BytesRead.Write(so.ReadBuffer, 0, bytesRead);

            if (so.BytesRead.Length > 4)
            {
                byte[] len = new byte[4];
                so.BytesRead.Read(len, 0, 4);
                var packetSize = BitConverter.ToInt32(len, 0);
                if (so.BytesRead.Length - 4 >= packetSize)
                {
                    byte[] packet = new byte[packetSize + 4];
                    so.BytesRead.Read(packet, 0, packetSize + 4);
                    ParseReceivedData(packet);
                }
            }

            readStream.BeginRead(readBuffer, 0, 1000, OnSocketRead, null);
        }

        private void ParseReceivedData(byte[] data)
        {

        }

        private void SendPacket(PacketType t, string body)
        {

            if (!Connected)
                throw new InvalidOperationException("Not Connected.");

            var netStream = tcpClient.GetStream();

            try
            {
                List<byte> byteStream = new List<byte>();

                int id = 0;
                byte[] asciiBody = Encoding.ASCII.GetBytes(body + @"\0");
                int packetSize = asciiBody.Length + 9;

                byteStream.AddRange(BitConverter.GetBytes(packetSize));
                byteStream.AddRange(BitConverter.GetBytes(id));
                byteStream.AddRange(BitConverter.GetBytes((int)t));
                byteStream.AddRange(asciiBody);

                netStream.Write(byteStream.ToArray(), 0, byteStream.Count);
            }
            catch (Exception e) { throw e; }
        }

        public void Authorize(string rconPassword)
        {
            SendPacket(PacketType.SERVERDATA_AUTH, rconPassword);
        }
        public void SendCommand(string command)
        {
            SendPacket(PacketType.SERVERDATA_EXECCOMMAND, command);
        }
        private void WriteToLog(String message)
        {
            Log.Add(message);
        }

        private enum PacketType : int
        {
            SERVERDATA_AUTH = 3, SERVERDATA_AUTH_RESPONSE = 2, SERVERDATA_EXECCOMMAND = 2, SERVERDATA_RESPONSE_VALUE = 0
        };

        private class StateObject
        {
            public static int ReadBufferSize = 1000;
            public byte[] ReadBuffer = new byte[readBufferSize];
            public MemoryStream BytesRead = new MemoryStream();
        }
    }
}
