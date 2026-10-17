using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace FlyHigh.PhoneController
{
    public sealed class WifiReceiver : MonoBehaviour
    {
        private UdpClient socket;
        private IPEndPoint owner;
        private uint sequence;
        private float lastInput = -100;
        private int received, rejected;
        private string lastResult = "No phone messages received";
        public string Diagnostics => "Messages: " + received + " | Rejected: " + rejected + " | " + lastResult;
        public string PairingCode { get; private set; }
        public string Status { get; private set; } = "Waiting for phone";
        public float Steering { get; private set; }
        public float Throttle { get; private set; }
        public float Brake { get; private set; } = 1;
        public bool Connected => socket != null && Time.realtimeSinceStartup - lastInput < 0.5f;

        private void OnEnable()
        {
            PairingCode = UnityEngine.Random.Range(100000, 1000000).ToString();
            received = rejected = 0;
            lastResult = "No phone messages received";
            try
            {
                socket = new UdpClient(ControllerPacket.Port); socket.Client.Blocking = false;
                Debug.Log("[Phone controller] Listening on UDP " + ControllerPacket.Port);
            }
            catch (SocketException e)
            {
                socket?.Close(); socket = null;
                Status = "Cannot listen: " + e.Message;
                Debug.LogError("[Phone controller] " + Status);
            }
        }

        private void Update()
        {
            if (socket == null) return;
            if (!Connected) { owner = null; Steering = Throttle = 0; Brake = 1; Status = "Waiting for phone"; }
            try
            {
                // Limit work per frame even if a peer floods the socket.
                for (int i = 0; i < 64 && socket.Available > 0; i++)
                {
                    IPEndPoint peer = new IPEndPoint(IPAddress.Any, 0);
                    byte[] data = socket.Receive(ref peer);
                    received++;
                    if (!ControllerPacket.TryDecode(data, PairingCode, out uint next, out float steer, out float gas, out float brake))
                    {
                        rejected++;
                        string[] fields = data.Length <= 128 ? Encoding.ASCII.GetString(data).Split('|') : new string[0];
                        lastResult = fields.Length == 6 && fields[0] == "FH1" && fields[1] != PairingCode
                            ? "Pairing code mismatch — enter the current PC code"
                            : "Invalid controller message";
                        continue;
                    }
                    if (owner != null && (!owner.Equals(peer) || !ControllerPacket.IsNewer(next, sequence))) continue;
                    owner = peer; sequence = next; lastInput = Time.realtimeSinceStartup;
                    Steering = steer; Throttle = gas; Brake = brake; Status = "Phone connected";
                    byte[] ack = Encoding.ASCII.GetBytes("FH1|OK|" + PairingCode);
                    socket.Send(ack, ack.Length, peer);
                    lastResult = "Valid input received; reply sent";
                }
            }
            catch (SocketException e) { Status = "Network: " + e.SocketErrorCode; }
        }

        private void OnDisable()
        {
            socket?.Close(); socket = null; owner = null; lastInput = -100;
            Steering = Throttle = 0; Brake = 1;
        }
    }
}
