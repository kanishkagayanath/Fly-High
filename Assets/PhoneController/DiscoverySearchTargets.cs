using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using UnityEngine;

namespace FlyHigh.PhoneController
{
    /// <summary>Bounded LAN discovery targets for broadcast and direct UDP requests.</summary>
    internal sealed class DiscoverySearchTargets
    {
        internal const int MaximumHosts = 1024;
        internal readonly List<IPEndPoint> Broadcasts = new List<IPEndPoint>();
        internal readonly List<IPEndPoint> Hosts = new List<IPEndPoint>();
        private readonly HashSet<uint> hostAddresses = new HashSet<uint>();

        internal void Refresh(string lastPcAddress)
        {
            Broadcasts.Clear();
            Hosts.Clear();
            hostAddresses.Clear();
            AddBroadcast(IPAddress.Broadcast);
            if (IPAddress.TryParse(lastPcAddress, out var previous)) AddHost(previous);
            AddAndroidNetwork();
            try
            {
                var adapters = NetworkInterface.GetAllNetworkInterfaces();
                Array.Sort(adapters, (a, b) =>
                    (a.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ? 0 : 1).CompareTo(
                     b.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ? 0 : 1));
                foreach (var adapter in adapters)
                {
                    if (adapter.OperationalStatus != OperationalStatus.Up ||
                        adapter.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                        adapter.NetworkInterfaceType == NetworkInterfaceType.Tunnel) continue;
                    try
                    {
                        foreach (var address in adapter.GetIPProperties().UnicastAddresses)
                            if (IsIPv4(address.Address)) AddNetwork(address.Address, address.IPv4Mask);
                    }
                    catch (NetworkInformationException) { }
                    catch (NotSupportedException) { }
                }
            }
            catch (NetworkInformationException) { }
            catch (NotSupportedException) { }
        }

        internal void AddNetwork(IPAddress address, IPAddress mask)
        {
            if (!IsIPv4(address)) return;
            uint local = Number(address);
            uint networkMask = IsIPv4(mask) ? Number(mask) : 0xffffff00u;
            uint hostMask = ~networkMask;
            // Only contiguous masks with at least two usable hosts describe a discovery subnet.
            if (networkMask == 0 || hostMask < 3 || (hostMask & unchecked(hostMask + 1)) != 0) return;
            uint first = (local & networkMask) + 1;
            uint last = (local | hostMask) - 1;
            AddBroadcast(Address(last + 1));

            // Large LANs are bounded to the phone's local 256-address window.
            if ((ulong)last - first + 1 > MaximumHosts)
            {
                first = Math.Max(first, (local & 0xffffff00u) + 1);
                last = Math.Min(last, (local | 255u) - 1);
            }
            for (uint next = first; next <= last && Hosts.Count < MaximumHosts; next++)
                if (next != local) AddHost(Address(next));
        }

        private void AddHost(IPAddress address)
        {
            if (!IsIPv4(address) || Hosts.Count >= MaximumHosts) return;
            uint value = Number(address);
            if (value == 0 || value >= 0xe0000000u || !hostAddresses.Add(value)) return;
            Hosts.Add(new IPEndPoint(address, ControllerPacket.Port));
        }

        private void AddBroadcast(IPAddress address)
        {
            var target = new IPEndPoint(address, ControllerPacket.Port);
            if (Broadcasts.Count < 8 && !Broadcasts.Contains(target)) Broadcasts.Add(target);
        }

        private static bool IsIPv4(IPAddress address) =>
            address != null && address.AddressFamily == AddressFamily.InterNetwork;

        private static uint Number(IPAddress address)
        {
            byte[] bytes = address.GetAddressBytes();
            return (uint)bytes[0] << 24 | (uint)bytes[1] << 16 | (uint)bytes[2] << 8 | bytes[3];
        }

        private static IPAddress Address(uint number) => new IPAddress(new[]
        {
            (byte)(number >> 24), (byte)(number >> 16), (byte)(number >> 8), (byte)number
        });

        private void AddAndroidNetwork()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unity.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var connectivity = activity.Call<AndroidJavaObject>("getSystemService", "connectivity"))
                using (var network = connectivity.Call<AndroidJavaObject>("getActiveNetwork"))
                {
                    if (network == null) return;
                    using (var properties = connectivity.Call<AndroidJavaObject>("getLinkProperties", network))
                    {
                        if (properties == null) return;
                        using (var links = properties.Call<AndroidJavaObject>("getLinkAddresses"))
                        {
                            int count = Math.Min(links.Call<int>("size"), 16);
                            for (int i = 0; i < count; i++)
                            {
                                using (var link = links.Call<AndroidJavaObject>("get", i))
                                using (var address = link.Call<AndroidJavaObject>("getAddress"))
                                {
                                    int prefix = link.Call<int>("getPrefixLength");
                                    if (prefix < 1 || prefix > 30 ||
                                        !IPAddress.TryParse(address.Call<string>("getHostAddress"), out var ip) ||
                                        !IsIPv4(ip)) continue;
                                    AddNetwork(ip, Address(uint.MaxValue << (32 - prefix)));
                                }
                            }
                        }
                    }
                }
            }
            catch (AndroidJavaException) { }
#endif
        }
    }
}

