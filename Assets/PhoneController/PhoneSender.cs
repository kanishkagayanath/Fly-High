using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FlyHigh.PhoneController
{
    public sealed class PhoneSender : MonoBehaviour
    {
        private UdpClient socket;
        private IPEndPoint server;
        private string code;
        private uint sequence;
        private float nextSend, lastAck = -100, centre;
        private bool active = true;
        public float Throttle { get; set; }
        public float Reverse { get; set; }
        public float Brake { get; set; }
        public float Steering { get; private set; }
        public string Status { get; private set; } = "Enter the PC address and pairing code";
        public bool Connected => socket != null && Time.realtimeSinceStartup - lastAck < 1;

        private void OnEnable()
        {
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            if (Accelerometer.current != null) InputSystem.EnableDevice(Accelerometer.current);
        }

        public void Connect(string address, string pairingCode)
        {
            Disconnect();
            if (!IPAddress.TryParse(address.Trim(), out IPAddress ip) || ip.AddressFamily != AddressFamily.InterNetwork ||
                pairingCode.Trim().Length != 6 || !uint.TryParse(pairingCode.Trim(), out _))
            { Status = "Enter an IPv4 address and six-digit code"; return; }
            try
            {
                server = new IPEndPoint(ip, ControllerPacket.Port); code = pairingCode.Trim();
                socket = new UdpClient(); socket.Connect(server); socket.Client.Blocking = false;
                sequence = 0; nextSend = 0; Status = "Connecting..."; Calibrate();
            }
            catch (SocketException e) { Disconnect(); Status = e.Message; }
        }

        private float Tilt()
        {
            if (Accelerometer.current == null) return 0;
            Vector3 a = Accelerometer.current.acceleration.ReadValue();
            // Input System compensates the sensor coordinates for landscape orientation.
            return Mathf.Atan2(a.x, -a.y) * Mathf.Rad2Deg;
        }
        public void Calibrate() { centre = Tilt(); }

        private void Update()
        {
            if (socket == null || !active) return;
            Steering = Mathf.Lerp(Steering, Mathf.Clamp(Mathf.DeltaAngle(centre, Tilt()) / 35f, -1, 1),
                1 - Mathf.Exp(-12 * Time.unscaledDeltaTime));
            try
            {
                for (int i = 0; i < 64 && socket.Available > 0; i++)
                {
                    IPEndPoint peer = server;
                    if (Encoding.ASCII.GetString(socket.Receive(ref peer)) == "FH1|OK|" + code) lastAck = Time.realtimeSinceStartup;
                }
                if (Time.realtimeSinceStartup >= nextSend)
                {
                    nextSend = Time.realtimeSinceStartup + 1f / 30;
                    byte[] data = ControllerPacket.Encode(code, ++sequence, Steering, Throttle - Reverse, Brake);
                    socket.Send(data, data.Length);
                }
                Status = Connected ? "Connected — tilt to steer" : "Waiting for PC — check Wi-Fi, address and code";
                if (Accelerometer.current == null) Status += " (no accelerometer detected)";
            }
            catch (SocketException e) { Status = "Network: " + e.SocketErrorCode; }
        }

        public void Disconnect()
        {
            socket?.Close(); socket = null; lastAck = -100; Throttle = Reverse = Brake = Steering = 0;
            Status = "Disconnected";
        }
        private void OnApplicationFocus(bool focus) { active = focus; Throttle = Reverse = Brake = 0; }
        private void OnApplicationPause(bool pause) { active = !pause; Throttle = Reverse = Brake = 0; }
        private void OnDisable() { Disconnect(); Screen.sleepTimeout = SleepTimeout.SystemSetting; }
    }
}
