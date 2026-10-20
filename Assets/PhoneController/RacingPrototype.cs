using UnityEngine;
using UnityEngine.InputSystem;

namespace FlyHigh.PhoneController
{
    public sealed class RacingPrototype : MonoBehaviour
    {
        [SerializeField, Min(0)] private float acceleration = 28f;
        [SerializeField, Min(0)] private float maximumSpeed = 55f;
        [SerializeField, Min(0)] private float maximumReverseSpeed = 12f;
        [SerializeField, Min(0.1f)] private float steeringResponse = 3f;
        [SerializeField, Min(0.05f)] private float releaseStopSmoothing = 0.25f;
        private Vector3 releaseSlowdownVelocity;
        private float smoothedSteering;
        private CarWheelVisuals wheelVisuals;
        private CarContactGuard contactGuard;
        private WifiReceiver input;
        private Rigidbody car;
        private Camera view;
        private bool cityMode;
        private Vector3 startPosition;
        private Quaternion startRotation = Quaternion.identity;
        public float RawSteering { get; private set; }
        public Transform PlayerCar => car != null ? car.transform : null;
        public string CollisionDiagnostics => contactGuard != null ? contactGuard.Diagnostics : "Contact guard initializing";
        public void Initialize(WifiReceiver receiver, bool useCity = false)
        {
            input = receiver;
            cityMode = useCity;
            startPosition = Vector3.up;
            if (cityMode)
            {
                var marker = FindFirstObjectByType<CityCarSpawn>();
                if (marker != null)
                {
                    startPosition = marker.transform.position;
                    startRotation = Quaternion.Euler(0, marker.transform.eulerAngles.y, 0);
                }
                else
                {
                    bool found = false;
                    foreach (var surface in FindObjectsByType<MeshCollider>(FindObjectsSortMode.None))
                    {
                        if (!surface.enabled || surface.isTrigger || surface.sharedMesh == null) continue;
                        if (!(surface.name + surface.sharedMesh.name).ToLowerInvariant().Contains("road")) continue;
                        var ray = new Ray(surface.bounds.center + Vector3.up * (surface.bounds.extents.y + 10), Vector3.down);
                        if (!surface.Raycast(ray, out RaycastHit hit, 100) || hit.normal.y < 0.9f) continue;
                        if (Physics.CheckBox(hit.point + Vector3.up * 0.8f, new Vector3(1, 0.35f, 2), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore)) continue;
                        startPosition = hit.point + Vector3.up * 0.8f;
                        found = true; break;
                    }
                    if (!found) Debug.LogWarning("[City drive] No clear road start found. Add a CityCarSpawn component to an empty object above a road.");
                }
            }
            else
            {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Controller test ground";
            ground.transform.position = new Vector3(0, -0.5f, 0);
            ground.transform.localScale = new Vector3(200, 1, 200);
            ground.GetComponent<Renderer>().material.color = new Color(0.12f, 0.2f, 0.22f);
            }
            var body = new GameObject("Player car");
            var box = body.AddComponent<BoxCollider>();
            box.size = new Vector3(1.8f, 0.8f, 3.6f);
            CreateCarVisual(body.transform);
            body.transform.SetPositionAndRotation(startPosition, startRotation);
            car = body.AddComponent<Rigidbody>();
            car.mass = 1000;
            // Arcade control owns heading. Impacts may move/stop the car, but cannot spin it.
            car.constraints = RigidbodyConstraints.FreezeRotation;
            car.interpolation = RigidbodyInterpolation.Interpolate;
            car.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            contactGuard = body.AddComponent<CarContactGuard>();
            contactGuard.Initialize(box);
            if (cityMode) CarContactGuard.AddPavementBarriers();
            view = Camera.main;
            if (view == null) view = new GameObject("Prototype camera", typeof(Camera)).GetComponent<Camera>();
            view.transform.position = startPosition + startRotation * new Vector3(0, 7, -12);
            view.transform.LookAt(startPosition);
        }

        private void CreateCarVisual(Transform parent)
        {
            var settings = Resources.Load<CarVisualSettings>("FlyHighCarVisual");
            if (settings == null || settings.prefab == null)
            {
                var fallback = GameObject.CreatePrimitive(PrimitiveType.Cube);
                fallback.transform.SetParent(parent, false);
                fallback.transform.localScale = new Vector3(1.8f, 0.8f, 3.6f);
                fallback.GetComponent<Collider>().enabled = false;
                fallback.GetComponent<Renderer>().material.color = new Color(1, 0.35f, 0.08f);
                Debug.LogWarning("[Car visual] Car settings missing. Wait for Editor import, then restart Play mode.");
                return;
            }
            var visual = Instantiate(settings.prefab, parent, false);
            visual.name = "Car_13 visual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            foreach (var collider in visual.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            foreach (var rigidbody in visual.GetComponentsInChildren<Rigidbody>(true))
            {
                rigidbody.isKinematic = true;
                rigidbody.detectCollisions = false;
                Destroy(rigidbody);
            }
            var renderers = visual.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            Bounds bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            if (bounds.size.z < 0.001f) return;
            visual.transform.localScale *= 3.6f / bounds.size.z;
            bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            visual.transform.localPosition -= new Vector3(bounds.center.x, bounds.min.y + 0.4f, bounds.center.z);
            wheelVisuals = gameObject.AddComponent<CarWheelVisuals>();
            wheelVisuals.Initialize(visual.transform, parent);
        }

        private void FixedUpdate()
        {
            if (car == null) return;
            float steer = input.Steering, throttle = input.Throttle, brake = input.Brake;
            var keys = Keyboard.current;
            // Keyboard fallback is for testing before installing the phone app.
            if (!input.Connected && keys != null && (keys.wKey.isPressed || keys.sKey.isPressed || keys.aKey.isPressed || keys.dKey.isPressed || keys.downArrowKey.isPressed))
            {
                steer = (keys.dKey.isPressed ? 1 : 0) - (keys.aKey.isPressed ? 1 : 0);
                throttle = (keys.wKey.isPressed ? 1 : 0) - (keys.downArrowKey.isPressed ? 1 : 0);
                brake = keys.sKey.isPressed ? 1 : 0;
            }
            Vector3 velocity = car.linearVelocity;
            RawSteering = steer;
            // Ease road velocity toward rest when a driving pedal is released.
            // Preserve gravity and clear the smoothing state when drive resumes.
            if (Mathf.Abs(throttle) < 0.01f)
            {
                Vector3 horizontal = new Vector3(velocity.x, 0, velocity.z);
                horizontal = Vector3.SmoothDamp(horizontal, Vector3.zero, ref releaseSlowdownVelocity,
                    releaseStopSmoothing, Mathf.Infinity, Time.fixedDeltaTime);
                if (horizontal.sqrMagnitude < 0.0025f)
                {
                    horizontal = Vector3.zero;
                    releaseSlowdownVelocity = Vector3.zero;
                }
                velocity = new Vector3(horizontal.x, velocity.y, horizontal.z);
            }
            else releaseSlowdownVelocity = Vector3.zero;
            velocity = contactGuard.Slide(velocity);
            car.linearVelocity = velocity;
            float speed = Vector3.Dot(velocity, car.transform.forward);
            smoothedSteering = Mathf.MoveTowards(smoothedSteering, steer, steeringResponse * Time.fixedDeltaTime);
            float speedFraction = Mathf.Clamp01(Mathf.Abs(speed) / Mathf.Max(1, maximumSpeed));
            // Hold the opposite pedal to brake first, then change direction near standstill.
            if (throttle * speed < -0.5f) { brake = 1; throttle = 0; }
            Vector3 driveForce = Vector3.zero;
            if (brake <= 0 && ((throttle > 0 && speed < maximumSpeed) || (throttle < 0 && speed > -maximumReverseSpeed)))
                driveForce = car.transform.forward * (throttle * acceleration * (throttle < 0 ? 0.6f : 1));
            Vector3 sideways = Vector3.Project(velocity, car.transform.right);
            driveForce -= sideways * Mathf.Lerp(10f, 14f, speedFraction);
            driveForce -= new Vector3(velocity.x, 0, velocity.z) * (0.25f + brake * 5);
            car.AddForce(contactGuard.Slide(driveForce), ForceMode.Acceleration);
            float turnRate = Mathf.Lerp(70f, 25f, speedFraction);
            float direction = speed >= 0 ? 1 : -1;
            Quaternion turn = car.rotation * Quaternion.Euler(0, smoothedSteering * turnRate * direction
                * Mathf.Clamp01(Mathf.Abs(speed) / 4) * Time.fixedDeltaTime, 0);
            contactGuard.ApplySteering(turn);
            if (car.position.y < startPosition.y - 30 || (!cityMode && (Mathf.Abs(car.position.x) > 98 || Mathf.Abs(car.position.z) > 98)))
                ResetCar();
        }
        private void ResetCar()
        {
            car.position = startPosition; car.rotation = startRotation;
            car.linearVelocity = Vector3.zero; car.angularVelocity = Vector3.zero;
            smoothedSteering = 0;
            releaseSlowdownVelocity = Vector3.zero;
            contactGuard.Clear();
            if (wheelVisuals != null) wheelVisuals.ResetPose();
        }
        private void Update()
        {
            if (car != null && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) ResetCar();
        }
        private void LateUpdate()
        {
            if (car == null || view == null) return;
            if (wheelVisuals != null)
            {
                float speed = Vector3.Dot(car.linearVelocity, car.transform.forward);
                float fraction = Mathf.Clamp01(Mathf.Abs(speed) / Mathf.Max(1, maximumSpeed));
                wheelVisuals.Animate(speed, smoothedSteering * Mathf.Lerp(28f, 8f, fraction), Time.deltaTime);
            }
            Vector3 target = car.position + car.transform.TransformDirection(new Vector3(0, 7, -12));
            view.transform.position = Vector3.Lerp(view.transform.position, target, 1 - Mathf.Exp(-5 * Time.deltaTime));
            view.transform.LookAt(car.position + Vector3.up);
        }
    }
}
