using UnityEngine;

namespace FlyHigh.PhoneController
{
    /// <summary>Keep drive forces out of walls and reject turns that increase penetration.</summary>
    public sealed class CarContactGuard : MonoBehaviour
    {
        private readonly ContactPoint[] contacts = new ContactPoint[24];
        private readonly Vector3[] normals = new Vector3[24];
        private readonly Collider[] nearby = new Collider[48];
        private int count;
        private float contactTime = -100;
        private BoxCollider body;
        private PhysicsMaterial material;
        private Quaternion approvedHeading;
        private float lastObstacleTime = -100;
        public bool HoldingHeading => Time.fixedTime - lastObstacleTime <= 0.12f;
        public string Diagnostics => HoldingHeading ? "Contact guard v2: HEADING HELD — reverse away" : "Contact guard v2: free steering";

        public void Initialize(BoxCollider collider)
        {
            body = collider;
            approvedHeading = body.attachedRigidbody.rotation;
            material = new PhysicsMaterial("Car sliding contact")
            {
                staticFriction = 0, dynamicFriction = 0, bounciness = 0,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounceCombine = PhysicsMaterialCombine.Minimum
            };
            body.sharedMaterial = material;
        }

        private void OnCollisionEnter(Collision collision) => Record(collision);
        private void OnCollisionStay(Collision collision) => Record(collision);
        private void Record(Collision collision)
        {
            if (contactTime != Time.fixedTime) { count = 0; contactTime = Time.fixedTime; }
            int length = collision.GetContacts(contacts);
            for (int i = 0; i < length && count < normals.Length; i++)
            {
                Vector3 normal = contacts[i].normal;
                if (Mathf.Abs(normal.y) > 0.8f) continue; // Keep road support out of horizontal sliding.
                normal.y = 0;
                if (normal.sqrMagnitude > 0.001f)
                {
                    normals[count++] = normal.normalized;
                    lastObstacleTime = Time.fixedTime;
                }
            }
            if (HoldingHeading) RestoreHeading();
        }
        public Vector3 Slide(Vector3 vector)
        {
            if (Time.fixedTime - contactTime > Time.fixedDeltaTime * 1.5f) return vector;
            // Two passes handle corners with more than one contact plane.
            for (int pass = 0; pass < 2; pass++)
                for (int i = 0; i < count; i++)
                {
                    float inward = Vector3.Dot(vector, normals[i]);
                    if (inward < 0) vector -= normals[i] * inward;
                }
            return vector;
        }
        public bool CanTurn(Quaternion rotation)
        {
            Vector3 position = body.attachedRigidbody.position;
            int length = Physics.OverlapBoxNonAlloc(position + rotation * body.center,
                body.size * 0.5f + Vector3.one * 0.02f, nearby, rotation, ~0, QueryTriggerInteraction.Ignore);
            if (length == nearby.Length) return false;
            for (int i = 0; i < length; i++)
            {
                var other = nearby[i];
                if (other.attachedRigidbody == body.attachedRigidbody || Physics.GetIgnoreLayerCollision(body.gameObject.layer, other.gameObject.layer)
                    || Physics.GetIgnoreCollision(body, other)) continue;
                if (!Physics.ComputePenetration(body, position, rotation, other, other.transform.position,
                    other.transform.rotation, out _, out float proposed)) continue;
                Physics.ComputePenetration(body, position, body.attachedRigidbody.rotation, other, other.transform.position,
                    other.transform.rotation, out _, out float current);
                if (proposed > Mathf.Max(0.015f, current + 0.005f)) return false;
            }
            return true;
        }
        public void ApplySteering(Quaternion requestedHeading)
        {
            // A rotation constraint cannot stop a scripted rotation assignment.
            // Keep the pre-contact approved heading until the car has left the obstacle.
            if (!HoldingHeading && CanTurn(requestedHeading)) approvedHeading = requestedHeading;
            RestoreHeading();
        }
        private void RestoreHeading()
        {
            body.attachedRigidbody.angularVelocity = Vector3.zero;
            body.attachedRigidbody.rotation = approvedHeading;
        }
        public void Clear()
        {
            count = 0; contactTime = lastObstacleTime = -100;
            if (body != null && body.attachedRigidbody != null) approvedHeading = body.attachedRigidbody.rotation;
        }
        private void OnDisable() => Clear();
        private void OnDestroy() { if (material != null) Destroy(material); }

        public static void AddPavementBarriers()
        {
            // Snapshot once before adding children; keep each imported sidewalk's X/Z footprint.
            foreach (var surface in Object.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None))
            {
                if (!surface.enabled || surface.isTrigger || surface.sharedMesh == null || surface.attachedRigidbody != null) continue;
                bool pavement = false;
                for (Transform node = surface.transform; node != null; node = node.parent)
                {
                    string name = node.name.ToLowerInvariant();
                    if (name.Contains("sidewalk") || name.Contains("set_b_tiles") || name.Contains("pavement")) { pavement = true; break; }
                }
                if (!pavement || Vector3.Dot(surface.transform.up, Vector3.up) < 0.99f) continue;
                Bounds bounds = surface.sharedMesh.bounds;
                float height = bounds.size.y * Mathf.Abs(surface.transform.lossyScale.y);
                if (height < 0.001f || height >= 1.2f) continue;
                float stretch = 1.2f / height;
                var barrier = new GameObject("Pavement driving barrier");
                barrier.layer = surface.gameObject.layer;
                barrier.transform.SetParent(surface.transform, false);
                barrier.transform.localPosition = new Vector3(0, bounds.min.y * (1 - stretch), 0);
                barrier.transform.localScale = new Vector3(1, stretch, 1);
                barrier.AddComponent<MeshCollider>().sharedMesh = surface.sharedMesh;
            }
        }
    }
}
