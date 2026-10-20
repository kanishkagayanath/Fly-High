using UnityEngine;

namespace FlyHigh.PhoneController
{
    public sealed class NpcTrafficCar : MonoBehaviour
    {
        private CityRoadGraph graph;
        private Rigidbody body;
        private CarWheelVisuals wheels;
        private int current, target = -1, previous = -1;
        private float cruise, speed, nextDecision;
        private readonly RaycastHit[] hits = new RaycastHit[24];
        private readonly Collider[] overlaps = new Collider[24];
        private Vector3 HalfSize = new Vector3(0.85f, 0.32f, 1.8f);

        public bool Initialize(CityRoadGraph roadGraph, int start, GameObject prefab, float cruisingSpeed)
        {
            graph = roadGraph; current = start; cruise = cruisingSpeed;
            transform.position = graph.nodes[start].position;
            var shape = gameObject.AddComponent<BoxCollider>(); shape.size = HalfSize * 2;
            body = gameObject.AddComponent<Rigidbody>(); body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            var model = Instantiate(prefab, transform, false);
            model.name = "Traffic vehicle visual";
            model.transform.localPosition = Vector3.zero; model.transform.localRotation = Quaternion.identity;
            foreach (var collider in model.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            foreach (var physics in model.GetComponentsInChildren<Rigidbody>(true))
            { physics.isKinematic = true; physics.detectCollisions = false; Destroy(physics); }
            var renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                model.transform.localScale *= 3.6f / Mathf.Max(0.01f, bounds.size.z);
                bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                HalfSize.x = Mathf.Max(HalfSize.x, bounds.size.x * 0.5f);
                shape.size = HalfSize * 2;
                model.transform.localPosition -= new Vector3(bounds.center.x - transform.position.x,
                    bounds.min.y - transform.position.y + 0.45f, bounds.center.z - transform.position.z);
            }
            wheels = gameObject.AddComponent<CarWheelVisuals>(); wheels.Initialize(model.transform, transform);
            if (graph.PickNext(current, previous, Vector3.forward, this, out target))
                body.rotation = Quaternion.LookRotation(graph.nodes[target].position - body.position);
            return target >= 0 && graph.RoadFootprint(body.position, body.rotation, HalfSize.x + 0.15f, HalfSize.z + 0.15f);
        }

        private void FixedUpdate()
        {
            if (graph == null) return;
            if (target < 0)
            {
                speed = 0;
                if (Time.time < nextDecision) return;
                nextDecision = Time.time + 0.5f;
                if (!graph.PickNext(current, previous, transform.forward, this, out target)) return;
            }
            Vector3 destination = graph.nodes[target].position;
            Vector3 delta = destination - body.position;
            if (delta.sqrMagnitude < 0.0025f)
            {
                graph.Reservations.Release(current, this);
                previous = current; current = target; target = -1;
                return;
            }
            Vector3 direction = delta.normalized;
            Quaternion facing = Quaternion.RotateTowards(body.rotation, Quaternion.LookRotation(direction), 100 * Time.fixedDeltaTime);
            float alignment = Mathf.Clamp01(Vector3.Dot(transform.forward, direction));
            float desiredSpeed = Mathf.Lerp(2, cruise, alignment * alignment);
            float lookAhead = Mathf.Max(2, speed * speed / 16 + 0.6f);
            float clearance = lookAhead;
            int count = Physics.BoxCastNonAlloc(body.position, HalfSize, direction, hits, facing, lookAhead, ~0, QueryTriggerInteraction.Ignore);
            if (count == hits.Length) clearance = 0;
            for (int i = 0; i < count; i++)
                if (hits[i].collider.attachedRigidbody != body) clearance = Mathf.Min(clearance, hits[i].distance);
            desiredSpeed = Mathf.Min(desiredSpeed, Mathf.Sqrt(16 * Mathf.Max(0, clearance - 0.35f)));
            speed = Mathf.MoveTowards(speed, desiredSpeed, (desiredSpeed < speed ? 8 : 3) * Time.fixedDeltaTime);
            float distance = Mathf.Min(delta.magnitude, speed * Time.fixedDeltaTime, Mathf.Max(0, clearance - 0.08f));
            Vector3 proposed = body.position + direction * distance;
            int blocking = Physics.OverlapBoxNonAlloc(proposed, HalfSize, overlaps, facing, ~0, QueryTriggerInteraction.Ignore);
            bool clear = blocking < overlaps.Length && graph.RoadFootprint(proposed, facing, HalfSize.x + 0.15f, HalfSize.z + 0.15f);
            for (int i = 0; i < blocking; i++) if (overlaps[i].attachedRigidbody != body) { clear = false; break; }
            if (!clear) { speed = 0; return; }
            body.MovePosition(proposed); body.MoveRotation(facing);
            float steer = Mathf.Clamp(Vector3.SignedAngle(transform.forward, direction, Vector3.up), -25, 25);
            wheels.Animate(distance / Time.fixedDeltaTime, steer, Time.fixedDeltaTime);
        }
        private void OnDestroy() { graph?.Reservations.ReleaseAll(this); }
    }
}
