using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace FlyHigh.PhoneController
{
    [DisallowMultipleComponent]
    public sealed class WifiReceiver : MonoBehaviour
    {
        private static UdpClient activeSocket;
        private static WifiReceiver activeReceiver;
        private UdpClient socket;
        private IPEndPoint owner;
        private uint sequence;
        private float lastInput = -100;
        private float nextDiscoveryReply;
        private string discoveryId, discoveryName;
        private int received, rejected;
        private string lastResult = "No phone messages received";
        public string Diagnostics => "Messages: " + received + " | Rejected: " + rejected + " | " + lastResult;
        public string PairingCode { get; private set; }
        public string Status { get; private set; } = "Waiting for phone";
        public float Steering { get; private set; }
        public float Throttle { get; private set; }
        public float Brake { get; private set; } = 1;
        public bool Connected => socket != null && Time.realtimeSinceStartup - lastInput < 0.5f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession()
        {
            CloseActiveSocket();
            Application.quitting -= CloseActiveSocket;
            Application.quitting += CloseActiveSocket;
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void RegisterEditorCleanup()
        {
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload -= CloseActiveSocket;
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += CloseActiveSocket;
            UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }
        private static void OnPlayModeChanged(UnityEditor.PlayModeStateChange state)
        {
            if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode ||
                state == UnityEditor.PlayModeStateChange.EnteredEditMode) CloseActiveSocket();
        }
#endif

        private void OnEnable()
        {
            if (activeReceiver == null && activeSocket != null) CloseActiveSocket();
            if (activeReceiver != null && activeReceiver != this && activeSocket != null)
            {
                Status = "Another controller receiver is already listening";
                Debug.LogWarning("[Phone controller] Disabled a duplicate receiver; the existing receiver keeps its connection.");
                enabled = false;
                return;
            }
            CloseSocket();
            PairingCode = UnityEngine.Random.Range(100000, 1000000).ToString();
            discoveryId = Guid.NewGuid().ToString("N");
            discoveryName = Environment.MachineName;
            nextDiscoveryReply = 0;
            received = rejected = 0;
            lastResult = "No phone messages received";
            UdpClient candidate = null;
            try
            {
                // Keep a reference before binding so even a failed bind is disposed.
                candidate = new UdpClient(AddressFamily.InterNetwork);
                candidate.Client.ExclusiveAddressUse = true;
                candidate.Client.Blocking = false;
                candidate.Client.Bind(new IPEndPoint(IPAddress.Any, ControllerPacket.Port));
                socket = candidate; candidate = null;
                activeSocket = socket; activeReceiver = this;
                Status = "Waiting for phone";
                Debug.Log("[Phone controller] Listening on UDP " + ControllerPacket.Port);
            }
            catch (SocketException e)
            {
                CloseSocket();
                Status = e.SocketErrorCode == SocketError.AddressAlreadyInUse
                    ? "Controller port " + ControllerPacket.Port + " is busy. Close another PC game or Unity Play session, then restart Play."
                    : "Cannot listen: " + e.SocketErrorCode;
                Debug.LogError("[Phone controller] " + Status);
            }
            finally { candidate?.Close(); }
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
                    if (ControllerDiscovery.TryReadRequest(data, out string nonce))
                    {
                        if (Time.realtimeSinceStartup >= nextDiscoveryReply)
                        {
                            nextDiscoveryReply = Time.realtimeSinceStartup + 0.2f;
                            byte[] reply = ControllerDiscovery.Reply(nonce, discoveryId, PairingCode, discoveryName, Connected);
                            socket.Send(reply, reply.Length, peer);
                        }
                        continue;
                    }
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

        private static void CloseActiveSocket()
        {
            // Retain the raw socket independently of Unity's destroyed-object null semantics.
            UdpClient active = activeSocket;
            WifiReceiver receiver = activeReceiver;
            activeSocket = null; activeReceiver = null;
            if (receiver != null) { receiver.socket = null; receiver.ClearInput(); }
            active?.Close();
        }
        private void ClearInput()
        {
            owner = null; lastInput = -100; sequence = 0;
            Steering = Throttle = 0; Brake = 1;
        }
        private void CloseSocket()
        {
            UdpClient owned = socket; socket = null;
            if (ReferenceEquals(activeSocket, owned) && owned != null) activeSocket = null;
            if (activeReceiver == this) activeReceiver = null;
            ClearInput();
            owned?.Close();
        }
        private void OnDisable() => CloseSocket();
        private void OnDestroy() => CloseSocket();
        private void OnApplicationQuit() => CloseSocket();
    }
}
