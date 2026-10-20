using System;

namespace FlyHigh.PhoneController
{
    /// <summary>Reserve both ends of a move so two cars cannot swap through each other.</summary>
    public sealed class TrafficReservations
    {
        private readonly object[] owners;
        public TrafficReservations(int capacity) { owners = new object[capacity]; }
        public bool Available(int node, object owner) => node >= 0 && node < owners.Length &&
            (owners[node] == null || ReferenceEquals(owners[node], owner));
        public bool TryReserve(int node, object owner)
        {
            if (owner == null || !Available(node, owner)) return false;
            owners[node] = owner; return true;
        }
        public void Release(int node, object owner)
        {
            if (node >= 0 && node < owners.Length && ReferenceEquals(owners[node], owner)) owners[node] = null;
        }
        public void ReleaseAll(object owner)
        {
            for (int i = 0; i < owners.Length; i++) if (ReferenceEquals(owners[i], owner)) owners[i] = null;
        }
    }
}
