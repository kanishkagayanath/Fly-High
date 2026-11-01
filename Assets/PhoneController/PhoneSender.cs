using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
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
        private float lastMissionState = -100, startUntil, nextStartSend;
        private string requestedToken;
        public MissionPhoneSnapshot MissionState { get; private set; }
        public bool HasMissionState => Connected && Time.realtimeSinceStartup - lastMissionState < 1;
        public bool MissionStartPending => requestedToken != null && Time.realtimeSinceStartup < startUntil;
        public void StartMission()
        {
            if (!HasMissionState || MissionState.phase != CoinMissionPhase.Available || MissionStartPending) return;
            requestedToken = MissionState.token; startUntil = Time.realtimeSinceStartup + 1.5f; nextStartSend = 0;
        }
        private float lastMenuState = -100, menuUntil, nextMenuSend;
        private string requestedMenuToken;
        private DriveMenuAction requestedMenuAction;
        public MenuPhoneSnapshot MenuState { get; private set; }
        public bool HasMenuState => Connected && Time.realtimeSinceStartup - lastMenuState < 1;
        public bool MenuPending => requestedMenuToken != null && Time.realtimeSinceStartup < menuUntil;
        public bool DrivingControlsAvailable => Connected && !MenuPending &&
            (lastMenuState < 0 || (HasMenuState && MenuState.view == DriveMenuView.Driving));
        public void RequestMenu(DriveMenuAction action)
        {
            if (!HasMenuState || MenuPending) return;
            requestedMenuToken = MenuState.token; requestedMenuAction = action;
            menuUntil = Time.realtimeSinceStartup + 1.5f; nextMenuSend = 0;
            Throttle = Reverse = Brake = 0;
        }
        private UdpClient discoverySocket;
        private readonly List<DiscoveredPC> games = new List<DiscoveredPC>();
        private readonly List<IPEndPoint> broadcastTargets = new List<IPEndPoint>();
        private string discoveryNonce;
        private byte[] discoveryRequest;
        private float discoveryEnd, nextDiscoverySend;
        public IReadOnlyList<DiscoveredPC> Games => games;
        public int DiscoveryVersion { get; private set; }
        public bool Searching => discoverySocket != null;
        public float Throttle { get; set; }
        public float Reverse { get; set; }
        public float Brake { get; set; }
        public float Steering { get; private set; }
        public string Status { get; private set; } = "Tap Find PC, or enter an address and code";
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

        public void FindPCs()
        {
            Disconnect(); games.Clear(); DiscoveryVersion++;
            try
            {
                discoverySocket = new UdpClient(AddressFamily.InterNetwork);
                discoverySocket.EnableBroadcast = true;
                discoverySocket.Client.Blocking = false;
                discoverySocket.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
                discoveryNonce = Guid.NewGuid().ToString("N");
                discoveryRequest = ControllerDiscovery.Request(discoveryNonce);
                BuildBroadcastTargets();
                discoveryEnd = Time.realtimeSinceStartup + 6;
                nextDiscoverySend = 0;
                Status = "Searching for PCs on your Wi-Fi...";
            }
            catch (SocketException e) { CancelDiscovery(); Status = "Cannot search: " + e.SocketErrorCode + ". Try manual Connect."; }
        }

        public void CancelDiscovery()
        {
            discoverySocket?.Close(); discoverySocket = null;
            discoveryNonce = null; discoveryRequest = null;
            if (socket == null) Status = "Tap Find PC, or enter an address and code";
        }

        private void BuildBroadcastTargets()
        {
            broadcastTargets.Clear();
            broadcastTargets.Add(new IPEndPoint(IPAddress.Broadcast, ControllerPacket.Port));
            // Directed broadcasts help PCs with several adapters. Limited broadcast remains the fallback.
            try
            {
                foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (adapter.OperationalStatus != OperationalStatus.Up || adapter.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                    foreach (var address in adapter.GetIPProperties().UnicastAddresses)
                    {
                        if (address.Address.AddressFamily != AddressFamily.InterNetwork || address.IPv4Mask == null) continue;
                        byte[] ip = address.Address.GetAddressBytes(), mask = address.IPv4Mask.GetAddressBytes();
                        for (int i = 0; i < 4; i++) ip[i] = (byte)(ip[i] | (mask[i] ^ 255));
                        var target = new IPEndPoint(new IPAddress(ip), ControllerPacket.Port);
                        if (!broadcastTargets.Contains(target) && broadcastTargets.Count < 8) broadcastTargets.Add(target);
                    }
                }
            }
            catch (NetworkInformationException) { }
            catch (NotSupportedException) { }
        }

        private void UpdateDiscovery()
        {
            float now = Time.realtimeSinceStartup;
            if (now >= discoveryEnd)
            {
                CancelDiscovery();
                Status = games.Count > 0 ? "Choose your PC to connect" : "No PC found. Check same Wi-Fi and firewall, or use manual Connect.";
                return;
            }
            try
            {
                for (int i = 0; i < 32 && discoverySocket.Available > 0; i++)
                {
                    IPEndPoint peer = new IPEndPoint(IPAddress.Any, 0);
                    byte[] data = discoverySocket.Receive(ref peer);
                    if (peer.Port != ControllerPacket.Port || !ControllerDiscovery.TryReadReply(data, discoveryNonce, out var found)) continue;
                    found.Address = peer.Address.ToString();
                    int existing = games.FindIndex(pc => pc.Id == found.Id);
                    if (existing >= 0)
                    {
                        var old = games[existing];
                        if (old.Busy == found.Busy && old.PairingCode == found.PairingCode) continue;
                        games[existing] = found;
                    }
                    else if (games.Count < ControllerDiscovery.MaximumGames) games.Add(found);
                    else continue;
                    DiscoveryVersion++;
                }
                if (now >= nextDiscoverySend)
                {
                    nextDiscoverySend = now + 0.75f;
                    // One unavailable adapter must not prevent searches on the others.
                    SocketError? failure = null; bool sent = false;
                    foreach (var target in broadcastTargets)
                    {
                        try { discoverySocket.Send(discoveryRequest, discoveryRequest.Length, target); sent = true; }
                        catch (SocketException e) { failure = e.SocketErrorCode; }
                    }
                    if (!sent) { CancelDiscovery(); Status = "Cannot search: " + failure + ". Try manual Connect."; }
                }
            }
            // An adapter/PC without a listener can return ICMP errors. Keep searching the other targets.
            catch (SocketException e) when (e.SocketErrorCode == SocketError.ConnectionReset || e.SocketErrorCode == SocketError.ConnectionRefused) { }
            catch (SocketException e) { CancelDiscovery(); Status = "Search interrupted: " + e.SocketErrorCode + ". Try again or use manual Connect."; }
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
            if (!active) return;
            if (discoverySocket != null) UpdateDiscovery();
            if (socket == null) return;
            Steering = Mathf.Lerp(Steering, Mathf.Clamp(Mathf.DeltaAngle(centre, Tilt()) / 35f, -1, 1),
                1 - Mathf.Exp(-12 * Time.unscaledDeltaTime));
            try
            {
                for (int i = 0; i < 64 && socket.Available > 0; i++)
                {
                    IPEndPoint peer = server;
                    byte[] reply = socket.Receive(ref peer);
                    if (Encoding.ASCII.GetString(reply) == "FH1|OK|" + code) lastAck = Time.realtimeSinceStartup;
                    else if (MenuPhoneProtocol.TryStatus(reply, code, out var menu) &&
                        (lastMenuState < 0 || menu.revision == MenuState.revision || ControllerPacket.IsNewer(menu.revision, MenuState.revision)))
                    {
                        if (lastMenuState < 0 || menu.view != MenuState.view) Throttle = Reverse = Brake = 0;
                        MenuState = menu; lastMenuState = Time.realtimeSinceStartup;
                        if (menu.token != requestedMenuToken) requestedMenuToken = null;
                    }
                    else if (MissionPhoneProtocol.TryStatus(reply, code, out var state) &&
                        (lastMissionState < 0 || state.revision == MissionState.revision || ControllerPacket.IsNewer(state.revision, MissionState.revision)))
                    {
                        MissionState = state; lastMissionState = Time.realtimeSinceStartup;
                        if (state.phase != CoinMissionPhase.Available || state.token != requestedToken) requestedToken = null;
                    }
                }
                if (!DrivingControlsAvailable) Throttle = Reverse = Brake = 0;
                if (MenuPending && HasMenuState && Time.realtimeSinceStartup >= nextMenuSend)
                {
                    nextMenuSend = Time.realtimeSinceStartup + .15f;
                    byte[] request = MenuPhoneProtocol.Request(code, requestedMenuToken, requestedMenuAction); socket.Send(request, request.Length);
                }
                if (!HasMenuState || Time.realtimeSinceStartup >= menuUntil) requestedMenuToken = null;
                if (Time.realtimeSinceStartup >= nextSend)
                {
                    nextSend = Time.realtimeSinceStartup + 1f / 30;
                    bool canDrive = DrivingControlsAvailable;
                    byte[] data = ControllerPacket.Encode(code, ++sequence, canDrive ? Steering : 0, canDrive ? Throttle - Reverse : 0, canDrive ? Brake : 1);
                    socket.Send(data, data.Length);
                }
                if (MissionStartPending && HasMissionState && Time.realtimeSinceStartup >= nextStartSend)
                {
                    nextStartSend = Time.realtimeSinceStartup + 0.15f;
                    byte[] request = MissionPhoneProtocol.Start(code, requestedToken); socket.Send(request, request.Length);
                }
                if (!HasMissionState || Time.realtimeSinceStartup >= startUntil) requestedToken = null;
                Status = Connected ? "Connected — tilt to steer" : "Waiting for PC — check Wi-Fi, address and code";
                if (Accelerometer.current == null) Status += " (no accelerometer detected)";
            }
            catch (SocketException e) { Status = "Network: " + e.SocketErrorCode; }
        }

        public void Disconnect()
        {
            CancelDiscovery();
            lastMissionState = -100; requestedToken = null; MissionState = default;
            lastMenuState = -100; requestedMenuToken = null; MenuState = default;
            socket?.Close(); socket = null; lastAck = -100; Throttle = Reverse = Brake = Steering = 0;
            Status = "Disconnected";
        }
        private void OnApplicationFocus(bool focus) { active = focus; Throttle = Reverse = Brake = 0; if (!focus) { CancelDiscovery(); requestedToken = requestedMenuToken = null; lastMissionState = -100; } }
        private void OnApplicationPause(bool pause) { active = !pause; Throttle = Reverse = Brake = 0; if (pause) { CancelDiscovery(); requestedToken = requestedMenuToken = null; lastMissionState = -100; } }
        private void OnDisable() { Disconnect(); Screen.sleepTimeout = SleepTimeout.SystemSetting; }
    }
}
