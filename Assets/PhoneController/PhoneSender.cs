using System;
using System.Collections.Generic;
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
        private float nextSend, lastAck = -100;
        private readonly TiltSteering tilt = new TiltSteering();
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
        private string requestedGarageId;
        private bool requestedGarageBuy;
        private float lastHomeState=-100;
        public HomePhoneSnapshot HomeState {get;private set;}
        public bool HasHomeState=>HasMenuState&&HomeState!=null&&Time.realtimeSinceStartup-lastHomeState<1.5f&&HomeState.token==MenuState.token;
        public void RequestVehicle(string id,bool buy)
        {
            if(!HasHomeState||HomeState.page!=PhoneHomePage.Shop||MenuPending||!HomePhoneProtocol.ValidId(id))return;
            requestedGarageId=id;requestedGarageBuy=buy;requestedMenuToken=MenuState.token;
            menuUntil=Time.realtimeSinceStartup+1.5f;nextMenuSend=0;Throttle=Reverse=Brake=0;
        }
        public MenuPhoneSnapshot MenuState { get; private set; }
        public bool HasMenuState => Connected && Time.realtimeSinceStartup - lastMenuState < 1;
        public bool MenuPending => requestedMenuToken != null && Time.realtimeSinceStartup < menuUntil;
        public bool DrivingControlsAvailable => Connected && !MenuPending &&
            (lastMenuState < 0 || (HasMenuState && MenuState.view == DriveMenuView.Driving));
        public void RequestMenu(DriveMenuAction action)
        {
            if (!HasMenuState || MenuPending) return;
            requestedGarageId=null;requestedMenuToken = MenuState.token; requestedMenuAction = action;
            menuUntil = Time.realtimeSinceStartup + 1.5f; nextMenuSend = 0;
            Throttle = Reverse = Brake = 0;
        }
        private const string LastPcAddressKey = "FlyHigh.LastPCAddress";
        private UdpClient discoverySocket;
        private readonly DiscoverySearchTargets discoveryTargets = new DiscoverySearchTargets();
        private int nextProbe, probePass;
        private float nextProbeSend;
        private bool savedPcAddress;
        private readonly List<DiscoveredPC> games = new List<DiscoveredPC>();
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
                sequence = 0; nextSend = 0; savedPcAddress = false; Status = "Connecting..."; Calibrate();
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
                discoveryTargets.Refresh(PlayerPrefs.GetString(LastPcAddressKey, ""));
                discoveryEnd = Time.realtimeSinceStartup + 10;
                nextDiscoverySend = nextProbeSend = 0;
                nextProbe = probePass = 0;
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

        private void SendDiscovery(IPEndPoint target)
        {
            try { discoverySocket.Send(discoveryRequest, discoveryRequest.Length, target); }
            catch (SocketException) { }
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
                    foreach (var target in discoveryTargets.Broadcasts) SendDiscovery(target);
                    if (discoveryTargets.Hosts.Count > 0) SendDiscovery(discoveryTargets.Hosts[0]);
                }
                if (now >= nextProbeSend && discoveryTargets.Hosts.Count > 0 && probePass < 2)
                {
                    nextProbeSend = now + .05f;
                    for (int i = 0; i < 12 && nextProbe < discoveryTargets.Hosts.Count; i++)
                        SendDiscovery(discoveryTargets.Hosts[nextProbe++]);
                    if (nextProbe == discoveryTargets.Hosts.Count)
                    {
                        nextProbe = 0;
                        probePass++;
                        nextProbeSend = now + .75f;
                    }
                }
            }
            // An adapter/PC without a listener can return ICMP errors. Keep searching the other targets.
            catch (SocketException e) when (e.SocketErrorCode == SocketError.ConnectionReset || e.SocketErrorCode == SocketError.ConnectionRefused) { }
            catch (SocketException e) { CancelDiscovery(); Status = "Search interrupted: " + e.SocketErrorCode + ". Try again or use manual Connect."; }
        }

        public void Calibrate() { tilt.Calibrate(); Steering = 0; }

        private void Update()
        {
            if (!active) return;
            if (discoverySocket != null) UpdateDiscovery();
            if (socket == null) return;
            tilt.Sample(Time.unscaledDeltaTime);
            Steering = tilt.Value;
            try
            {
                for (int i = 0; i < 64 && socket.Available > 0; i++)
                {
                    IPEndPoint peer = server;
                    byte[] reply = socket.Receive(ref peer);
                    if (Encoding.ASCII.GetString(reply) == "FH1|OK|" + code)
                    {
                        lastAck = Time.realtimeSinceStartup;
                        if (!savedPcAddress)
                        {
                            savedPcAddress = true;
                            PlayerPrefs.SetString(LastPcAddressKey, server.Address.ToString());
                            PlayerPrefs.Save();
                        }
                    }
                    else if (MenuPhoneProtocol.TryStatus(reply, code, out var menu) &&
                        (lastMenuState < 0 || menu.revision == MenuState.revision || ControllerPacket.IsNewer(menu.revision, MenuState.revision)))
                    {
                        if (lastMenuState < 0 || menu.view != MenuState.view) Throttle = Reverse = Brake = 0;
                        MenuState = menu; lastMenuState = Time.realtimeSinceStartup;
                        if (menu.token != requestedMenuToken){requestedMenuToken = null;requestedGarageId=null;}
                    }
                    else if(HomePhoneProtocol.TryStatus(reply,code,out var home)&&
                        (lastHomeState<0||home.revision==HomeState.revision||ControllerPacket.IsNewer(home.revision,HomeState.revision)))
                    {HomeState=home;lastHomeState=Time.realtimeSinceStartup;}
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
                    byte[] request = requestedGarageId!=null?HomePhoneProtocol.Request(code,requestedMenuToken,requestedGarageId,requestedGarageBuy):MenuPhoneProtocol.Request(code, requestedMenuToken, requestedMenuAction); socket.Send(request, request.Length);
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
            lastMenuState = -100; requestedMenuToken = requestedGarageId = null; MenuState = default;
            lastHomeState=-100;HomeState=null;
            socket?.Close(); socket = null; lastAck = -100; Throttle = Reverse = Brake = Steering = 0;
            Status = "Disconnected";
        }
        private void OnApplicationFocus(bool focus) { active = focus; Throttle = Reverse = Brake = 0; if (!focus) { CancelDiscovery(); requestedToken = requestedMenuToken = null; lastMissionState = -100; } }
        private void OnApplicationPause(bool pause) { active = !pause; Throttle = Reverse = Brake = 0; if (pause) { CancelDiscovery(); requestedToken = requestedMenuToken = null; lastMissionState = -100; } }
        private void OnDisable() { Disconnect(); Screen.sleepTimeout = SleepTimeout.SystemSetting; }
    }
}
