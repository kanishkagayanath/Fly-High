using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FlyHigh.PhoneController
{
    public sealed class CityTrafficSystem : MonoBehaviour
    {
        private Transform player;
        public int VehicleCount { get; private set; }
        public string Status { get; private set; } = "Preparing city traffic";
        public void Initialize(Transform playerCar) { player = playerCar; StartCoroutine(Prepare()); }
        private IEnumerator Prepare()
        {
            yield return null;
            var settings = Resources.Load<TrafficVehicleSettings>("FlyHighTraffic");
            if (settings == null || settings.vehicles == null || settings.vehicles.Length == 0)
            { Status = "Traffic assets unavailable — allow Unity import and restart Play"; Debug.LogWarning(Status); yield break; }
            // One startup synchronization: road queries must see the newly created pavement barriers.
            Physics.SyncTransforms();
            var graph = new CityRoadGraph(); graph.Build();
            var candidates = new List<int>();
            for (int i = 0; i < graph.nodes.Count; i++) if (graph.nodes[i].neighbours.Count > 0) candidates.Add(i);
            for (int i = candidates.Count - 1; i > 0; i--)
            { int j = Random.Range(0, i + 1); int swap = candidates[i]; candidates[i] = candidates[j]; candidates[j] = swap; }
            var spawnPoints = new List<Vector3>();
            // Farthest-point placement spreads traffic over the complete road network.
            // The old player-distance sort filled the nearby streets first.
            while (candidates.Count > 0 && VehicleCount < settings.vehicleCount)
            {
                int selected=-1;float best=float.NegativeInfinity;
                foreach(int candidate in candidates)
                {
                    Vector3 p=graph.nodes[candidate].position;
                    if(player!=null && Vector3.Distance(player.position,p)<14)continue;
                    float separation=spawnPoints.Count==0?0:float.PositiveInfinity;
                    foreach(var other in spawnPoints)separation=Mathf.Min(separation,(p-other).sqrMagnitude);
                    if(separation>best){best=separation;selected=candidate;}
                }
                if(selected<0)break;
                candidates.Remove(selected);
                int index=selected;
                Vector3 position = graph.nodes[index].position;
                if (player != null && Vector3.Distance(player.position, position) < 14) continue;
                bool spaced = true;
                foreach (Vector3 other in spawnPoints) if (Vector3.Distance(position, other) < 16) { spaced = false; break; }
                if (!spaced || !graph.RoadFootprint(position, Quaternion.identity) || Physics.CheckBox(position, new Vector3(1, 0.33f, 2), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore)) continue;
                var prefab = settings.vehicles[VehicleCount % settings.vehicles.Length];
                if (prefab == null) continue;
                var root = new GameObject("NPC traffic / " + prefab.name);
                root.transform.SetParent(transform);
                var driver = root.AddComponent<NpcTrafficCar>();
                if (!graph.Reservations.TryReserve(index, driver)) { Destroy(root); continue; }
                if (!driver.Initialize(graph, index, prefab, settings.cruisingSpeed * Random.Range(0.8f, 1.1f), player))
                { graph.Reservations.ReleaseAll(driver); root.SetActive(false); Destroy(root); continue; }
                GetComponent<GameAudio>()?.AttachTraffic(root.transform);
                spawnPoints.Add(position); VehicleCount++;
            }
            Status = "NPC traffic: " + VehicleCount + " vehicles (asphalt only)";
            Debug.Log("[City traffic] " + VehicleCount + " vehicles; " + graph.nodes.Count + " sampled road points.");
            if (VehicleCount == 0) Debug.LogWarning("[City traffic] No clear connected road space found; traffic was not spawned.");
        }
    }
}

