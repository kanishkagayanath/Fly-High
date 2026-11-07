using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FlyHigh.PhoneController
{
    /// <summary>Desktop authority for offers, pickups and completion. The phone only requests a start.</summary>
    public sealed class CoinMissionSystem : MonoBehaviour
    {
        public const string WalletKey = "FlyHigh.Coins";
        private CoinMissionSettings settings;
        private Transform player;
        private Rigidbody body;
        private WifiReceiver receiver;
        private CoinMissionPresentation presentation;
        private bool[] collected;
        private int offered = -1, active = -1, lastCompleted = -1;
        private string offerToken;
        private Vector3 previousPosition;
        private float completionUntil, nextOfferScan;
        private uint revision;
        private bool lastStartable, lastPaused;
        private VehicleGarage garage;

        public event Action<CoinMissionPhase> PhaseChanged;
        public event Action CoinCollected;
        public CoinMissionSettings Settings => settings;
        public CoinMissionPhase Phase { get; private set; }
        public Vector3 PlayerPosition => player != null ? player.position : Vector3.zero;
        public int OfferedIndex => offered;
        public int ActiveIndex => active;
        public int Collected { get; private set; }
        public int Wallet => garage != null ? garage.Coins : 0;
        public int Total => active >= 0 ? settings.routes[active].coins.Length : 0;
        public string OfferToken => offerToken;
        public int Version { get; private set; }
        public bool CanStart => enabled && Phase == CoinMissionPhase.Available && Time.timeScale > 0 && Eligible(offered);
        public bool IsCollected(int index) => collected != null && index >= 0 && index < collected.Length && collected[index];

        public void Initialize(Transform car, WifiReceiver connection, RectTransform hud, CityMinimap minimap, VehicleGarage progress, bool onPhone = false)
        {
            if (settings != null) return;
            settings = Resources.Load<CoinMissionSettings>("FlyHighMissions");
            if (!ValidSettings(settings)) { Debug.LogWarning("[Missions] Bake valid coin routes with Fly High > City > Bake coin missions."); enabled = false; return; }
            player = car; body = car.GetComponent<Rigidbody>(); receiver = connection;
            garage = progress; garage.Changed += ProgressChanged;
            previousPosition = body.position;
            presentation = gameObject.AddComponent<CoinMissionPresentation>();
            presentation.Initialize(this, hud, minimap, onPhone);
            if (receiver != null) receiver.StartMissionRequested += RequestStart;
            Publish();
        }

        private static bool ValidSettings(CoinMissionSettings value)
        {
            if (value == null || value.coinModel == null || value.sackModel == null || value.markerMaterial == null || value.routes == null || value.routes.Length == 0) return false;
            foreach (var route in value.routes)
                if (route == null || route.path == null || route.path.Length < 2 || route.coins == null || route.coins.Length == 0 || route.coins.Length > 256) return false;
            return true;
        }

        private void Update()
        {
            if (player == null) return;
            if (lastStartable != CanStart || lastPaused != (Time.timeScale <= 0)) { lastStartable = CanStart; lastPaused = Time.timeScale <= 0; Publish(); }
            if (Time.timeScale <= 0) return;
            if (Phase == CoinMissionPhase.Complete && Time.time >= completionUntil)
            { Phase = CoinMissionPhase.Exploring; offered = -1; Version++; PhaseChanged?.Invoke(Phase); Publish(); }
            if (Phase == CoinMissionPhase.Exploring || Phase == CoinMissionPhase.Available)
            {
                if (Time.time >= nextOfferScan) { nextOfferScan = Time.time + 0.1f; ScanOffers(); }
                if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) TryStartMission();
            }
        }

        private void FixedUpdate()
        {
            if (body == null) return;
            Vector3 here = body.position;
            if (Phase == CoinMissionPhase.Collecting)
            {
                // Swept pickup tests cannot miss a coin at speed. Large teleports/reset never sweep the map.
                Vector3 from = (here - previousPosition).sqrMagnitude < 225 ? previousPosition : here;
                var route = settings.routes[active];
                for (int i = 0; i < collected.Length; i++)
                {
                    if (collected[i] || SegmentDistance(route.coins[i], from, here) > settings.collectRadius) continue;
                    collected[i] = true; Collected++; Award(1); Version++;
                    presentation.Pickup(i); CoinCollected?.Invoke(); Publish();
                }
                if (Collected == Total && SegmentDistance(route.path[route.path.Length - 1], from, here) <= 3.2f)
                    Complete();
            }
            previousPosition = here;
        }

        private void ScanOffers()
        {
            int next = -1; float distance = settings.startRadius * settings.startRadius;
            for (int i = 0; i < settings.routes.Length; i++)
            {
                // Exit the completed point before replaying it; other missions remain immediately available.
                float d = FlatDistanceSquared(player.position, settings.routes[i].path[0]);
                if (i == lastCompleted) { if (d > 100) lastCompleted = -1; else continue; }
                if (d < distance) { distance = d; next = i; }
            }
            if (next == offered) return;
            offered = next;
            Phase = next >= 0 ? CoinMissionPhase.Available : CoinMissionPhase.Exploring;
            offerToken = next >= 0 ? Guid.NewGuid().ToString("N").Substring(0,16) : null;
            Version++; PhaseChanged?.Invoke(Phase); Publish();
        }

        private bool Eligible(int index) => body != null && index >= 0 && index < settings.routes.Length &&
            FlatDistanceSquared(body.position, settings.routes[index].path[0]) <= settings.startRadius * settings.startRadius &&
            new Vector2(body.linearVelocity.x, body.linearVelocity.z).sqrMagnitude <= 9;

        public bool TryStartMission()
        {
            if (!CanStart) return false;
            active = offered; offered = -1; offerToken = null;
            collected = new bool[settings.routes[active].coins.Length]; Collected = 0;
            previousPosition = body.position;
            Phase = CoinMissionPhase.Collecting; Version++;
            presentation.Begin(settings.routes[active]); PhaseChanged?.Invoke(Phase); Publish(); return true;
        }

        private void RequestStart(string token)
        {
            if (token == offerToken && !string.IsNullOrEmpty(token)) TryStartMission();
        }

        private void Complete()
        {
            Award(settings.finishBonus); SaveWallet(); lastCompleted = active;
            Phase = CoinMissionPhase.Complete; completionUntil = Time.time + 5;
            active = -1; Version++;
            presentation.Complete(); PhaseChanged?.Invoke(Phase); Publish();
        }

        public int NextCoin()
        {
            if (active < 0) return -1;
            int nearest = -1; float distance = float.PositiveInfinity;
            for (int i = 0; i < collected.Length; i++) if (!collected[i])
            { float d = FlatDistanceSquared(player.position,settings.routes[active].coins[i]); if (d < distance) {distance = d; nearest = i;} }
            return nearest;
        }

        private void Award(int value) { garage.Credit(value); }
        private void SaveWallet() { if (garage != null) garage.Flush(); }
        private void ProgressChanged() { Version++; Publish(); }
        private void Publish()
        {
            if (receiver == null) return;
            receiver.SetMissionState(new MissionPhoneSnapshot { revision = ++revision,
                phase = Time.timeScale <= 0 || (Phase == CoinMissionPhase.Available && !CanStart) ? CoinMissionPhase.Exploring : Phase, token = offerToken,
                collected = Phase == CoinMissionPhase.Collecting || Phase == CoinMissionPhase.Complete ? Collected : 0,
                total = active >= 0 ? Total : Phase == CoinMissionPhase.Available ? settings.routes[offered].coins.Length : Phase == CoinMissionPhase.Complete ? Collected : 0,
                wallet = Wallet });
        }
        private static float FlatDistanceSquared(Vector3 a, Vector3 b) { float x=a.x-b.x,z=a.z-b.z; return x*x+z*z; }
        private static float SegmentDistance(Vector3 p, Vector3 a, Vector3 b)
        {
            if (Mathf.Abs(p.y - b.y) > 3) return float.PositiveInfinity;
            p.y = a.y = b.y = 0; Vector3 delta = b-a;
            float t = delta.sqrMagnitude < 0.0001f ? 0 : Mathf.Clamp01(Vector3.Dot(p-a,delta)/delta.sqrMagnitude);
            return Vector3.Distance(p,a+delta*t);
        }
        private void OnApplicationPause(bool paused) { if (paused) SaveWallet(); }
        private void OnDisable()
        {
            SaveWallet();
            if (garage != null) garage.Changed -= ProgressChanged;
            if (receiver != null) { receiver.StartMissionRequested -= RequestStart; receiver.ClearMissionState(); }
            if (presentation != null) presentation.enabled = false;
        }
        private void OnEnable()
        {
            if (settings == null || presentation == null) return;
            if (garage != null) { garage.Changed -= ProgressChanged; garage.Changed += ProgressChanged; }
            if (receiver != null)
            {
                receiver.StartMissionRequested -= RequestStart;
                receiver.StartMissionRequested += RequestStart;
            }
            if (presentation != null) presentation.enabled = true;
            Publish();
        }
        private void OnDestroy() { if (presentation != null) Destroy(presentation); }
    }
}


