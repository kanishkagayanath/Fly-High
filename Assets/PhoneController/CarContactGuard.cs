using System.Collections.Generic;
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
        private readonly List<Mesh> pavementMeshes = new List<Mesh>();
        private readonly List<GameObject> pavementBarriers = new List<GameObject>();
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
        private void OnDestroy()
        {
            foreach (var barrier in pavementBarriers) if (barrier != null) Destroy(barrier);
            foreach (var mesh in pavementMeshes) if (mesh != null) Destroy(mesh);
            if (material != null) Destroy(material);
        }

        public void AddPavementBarriers()
        {
            // Road tiles include their own kerbs/pavements in the non-asphalt submeshes.
            // Extrude top faces instead of stretching Y: flat paving has no height to stretch.
            foreach (var surface in Object.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None))
            {
                if (!surface.enabled || surface.isTrigger || surface.sharedMesh == null || surface.attachedRigidbody != null || surface.name == "Pavement driving barrier") continue;
                if (surface.transform.Find("Pavement driving barrier") != null) continue;
                bool pavement = false;
                for (Transform node = surface.transform; node != null; node = node.parent)
                {
                    string name = node.name.ToLowerInvariant();
                    if (name.Contains("sidewalk") || name.Contains("set_b_tiles") || name.Contains("pavement")) { pavement = true; break; }
                }
                var renderer = surface.GetComponent<MeshRenderer>();
                var materials = renderer != null ? renderer.sharedMaterials : new Material[0];
                bool road = false;
                if (!pavement && (surface.name + surface.sharedMesh.name).ToLowerInvariant().Contains("road"))
                    foreach (var roadMaterial in materials)
                        if (roadMaterial != null && RoadTriangleMask.AsphaltMaterial(roadMaterial.name)) { road = true; break; }
                if ((!pavement && !road) || Vector3.Dot(surface.transform.up, Vector3.up) < 0.99f) continue;
                Mesh source = surface.sharedMesh;
                if (!source.isReadable)
                {
                    Debug.LogWarning("[Pavement guard] Read/Write is required for pavement mesh: " + surface.name);
                    continue;
                }
                var vertices = new List<Vector3>();
                var triangles = new List<int>();
                Vector3[] sourceVertices = source.vertices;
                for (int sub = 0; sub < source.subMeshCount; sub++)
                {
                    if (source.GetTopology(sub) != MeshTopology.Triangles) continue;
                    if (road && (sub >= materials.Length || materials[sub] == null || RoadTriangleMask.AsphaltMaterial(materials[sub].name))) continue;
                    int[] indices = source.GetTriangles(sub);
                    for (int i = 0; i < indices.Length; i += 3)
                    {
                        Vector3 a = surface.transform.TransformPoint(sourceVertices[indices[i]]);
                        Vector3 b = surface.transform.TransformPoint(sourceVertices[indices[i+1]]);
                        Vector3 c = surface.transform.TransformPoint(sourceVertices[indices[i+2]]);
                        if (Vector3.Cross(b-a,c-a).y <= 0.00001f) continue;
                        // Closed triangular prisms preserve concave and curved pavement
                        // outlines without a bounding box that would obstruct the road.
                        int start = vertices.Count;
                        vertices.Add(surface.transform.InverseTransformPoint(a - Vector3.up * 0.5f));
                        vertices.Add(surface.transform.InverseTransformPoint(b - Vector3.up * 0.5f));
                        vertices.Add(surface.transform.InverseTransformPoint(c - Vector3.up * 0.5f));
                        vertices.Add(surface.transform.InverseTransformPoint(a + Vector3.up * 1.6f));
                        vertices.Add(surface.transform.InverseTransformPoint(b + Vector3.up * 1.6f));
                        vertices.Add(surface.transform.InverseTransformPoint(c + Vector3.up * 1.6f));
                        int[] prism = {2,1,0, 3,4,5, 0,1,4, 0,4,3, 1,2,5, 1,5,4, 2,0,3, 2,3,5};
                        foreach (int index in prism) triangles.Add(start + index);
                    }
                }
                if (triangles.Count == 0) continue;
                var mesh = new Mesh {name = surface.name + " pavement exclusion", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32};
                mesh.SetVertices(vertices); mesh.SetTriangles(triangles,0); mesh.RecalculateBounds();
                var barrier = new GameObject("Pavement driving barrier");
                barrier.layer = surface.gameObject.layer;
                barrier.transform.SetParent(surface.transform, false);
                var collider = barrier.AddComponent<MeshCollider>();
                collider.sharedMesh = mesh;
                collider.sharedMaterial = material;
                pavementMeshes.Add(mesh); pavementBarriers.Add(barrier);
            }
            Physics.SyncTransforms();
        }
    }
}
