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
        [SerializeField, Range(0, 0.25f)] private float phoneSteeringDeadZone = 0.06f;
        [SerializeField, Range(1, 3)] private float phoneSteeringExponent = 1.6f;
        [SerializeField, Min(0.01f)] private float phoneSteeringSmoothTime = 0.14f;
        [SerializeField, Min(0.01f)] private float phoneRecentreSmoothTime = 0.10f;
        [SerializeField, Min(0.05f)] private float releaseStopSmoothing = 0.25f;
        [SerializeField, Min(0.01f)] private float cameraPositionSmoothTime = 0.12f;
        [SerializeField, Min(0.01f)] private float cameraAimSmoothTime = 0.06f;
        [SerializeField, Min(0.01f)] private float cameraHeadingSmoothTime = 0.14f;
        private Vector3 cameraFollowVelocity, cameraAimVelocity, cameraAimPoint;
        private float cameraHeading, cameraHeadingVelocity;
        private bool cameraSnapPending;
        private Vector3 releaseSlowdownVelocity;
        private float smoothedSteering;
        private float phoneSteeringVelocity;
        private CarWheelVisuals wheelVisuals;
        private VehicleGarage garage;
        private GameObject activeVisual;
        private CarContactGuard contactGuard;
        private WifiReceiver input;
        private MobileDriveControls mobileControls;
        private Rigidbody car;
        private Camera view;
        private bool cityMode;
        private Vector3 startPosition;
        private Quaternion startRotation = Quaternion.identity;
        public float RawSteering { get; private set; }
        public float AppliedThrottle { get; private set; }
        public float AppliedBrake { get; private set; }
        public float MaximumSpeed => maximumSpeed;
        public Transform PlayerCar => car != null ? car.transform : null;
        public string CollisionDiagnostics => contactGuard != null ? contactGuard.Diagnostics : "Contact guard initializing";
        public void Initialize(WifiReceiver receiver, bool useCity = false, VehicleGarage progress = null, MobileDriveControls localControls = null)
        {
            input = receiver; garage = progress;
            mobileControls = localControls;
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
            if (cityMode) contactGuard.AddPavementBarriers();
            view = Camera.main;
            if (view == null) view = new GameObject("Prototype camera", typeof(Camera)).GetComponent<Camera>();
            SnapCamera(startPosition, startRotation);
            if(garage!=null)garage.SelectionChanged+=ChangeVehicle;
        }

        private void ChangeVehicle(ShopVehicle entry)
        {
            if(car==null || entry==null)return;
            CreateCarVisual(car.transform,entry.prefab);
            var environment=GetComponent<CityDayNight>();if(environment!=null)environment.RefreshPlayerVehicleLights();
        }
        private void CreateCarVisual(Transform parent,GameObject prefab=null)
        {
            if(prefab==null)prefab=garage?.Selected?.prefab;
            if(prefab==null)prefab=Resources.Load<CarVisualSettings>("FlyHighCarVisual")?.prefab;
            if(prefab==null){Debug.LogWarning("[Car visual] Player vehicle model unavailable.");return;}
            // Normalise at the origin, then attach in car-local space. World bounds
            // cannot be subtracted from a car that has moved or turned in the city.
            var root=new GameObject("Player vehicle visual");var visual=Instantiate(prefab,root.transform,false);visual.name=prefab.name;
            foreach(var collider in visual.GetComponentsInChildren<Collider>(true))collider.enabled=false;
            foreach(var physics in visual.GetComponentsInChildren<Rigidbody>(true)){physics.isKinematic=true;physics.detectCollisions=false;Destroy(physics);}
            var renderers=visual.GetComponentsInChildren<Renderer>();
            if(renderers.Length==0){Destroy(root);return;}
            Bounds bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
            if(bounds.size.z<.001f || bounds.size.x<.001f){Destroy(root);return;}
            visual.transform.localScale*=Mathf.Min(3.6f/bounds.size.z,1.8f/bounds.size.x);
            bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
            visual.transform.localPosition-=new Vector3(bounds.center.x,bounds.min.y+.4f,bounds.center.z);
            root.transform.SetParent(parent,false);
            if(activeVisual!=null){activeVisual.SetActive(false);Destroy(activeVisual);}activeVisual=root;
            if(wheelVisuals==null)wheelVisuals=gameObject.AddComponent<CarWheelVisuals>();wheelVisuals.Initialize(root.transform,parent);
        }
        private void OnDestroy(){if(garage!=null)garage.SelectionChanged-=ChangeVehicle;}

        private void FixedUpdate()
        {
            if (car == null) return;
            float steer = mobileControls != null ? mobileControls.Steering : input.Steering;
            float throttle = mobileControls != null ? mobileControls.Throttle : input.Throttle;
            float brake = mobileControls != null ? mobileControls.Brake : input.Brake;
            var keys = Keyboard.current;
            bool keyboardSteering = mobileControls == null && !input.Connected;
            // Keyboard fallback is for testing before installing the phone app.
            if (keyboardSteering && keys != null && (keys.wKey.isPressed || keys.sKey.isPressed || keys.aKey.isPressed || keys.dKey.isPressed || keys.downArrowKey.isPressed))
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
            UpdateSteering(steer, keyboardSteering, Time.fixedDeltaTime);
            float speedFraction = Mathf.Clamp01(Mathf.Abs(speed) / Mathf.Max(1, maximumSpeed));
            // Hold the opposite pedal to brake first, then change direction near standstill.
            if (throttle * speed < -0.5f) { brake = 1; throttle = 0; }
            AppliedThrottle=throttle;AppliedBrake=brake;
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
        private void UpdateSteering(float raw, bool keyboard, float deltaTime)
        {
            if (deltaTime <= 0) return;
            if (keyboard)
            {
                smoothedSteering = Mathf.MoveTowards(smoothedSteering, raw, steeringResponse * deltaTime);
                phoneSteeringVelocity = 0;
                return;
            }
            // Apply the same steering curve to local and remote phone input.
            // Remove hand tremor around neutral, then build steering progressively.
            float magnitude = Mathf.Clamp01((Mathf.Abs(raw) - phoneSteeringDeadZone)
                / Mathf.Max(0.01f, 1 - phoneSteeringDeadZone));
            float target = Mathf.Sign(raw) * Mathf.Pow(magnitude, phoneSteeringExponent);
            float smoothing = target == 0 ? phoneRecentreSmoothTime : phoneSteeringSmoothTime;
            smoothedSteering = Mathf.SmoothDamp(smoothedSteering, target, ref phoneSteeringVelocity,
                smoothing, Mathf.Infinity, deltaTime);
            if (target == 0 && Mathf.Abs(smoothedSteering) < 0.0005f)
            {
                smoothedSteering = 0;
                phoneSteeringVelocity = 0;
            }
        }
        private void ResetCar()
        {
            car.position = startPosition; car.rotation = startRotation;
            car.linearVelocity = Vector3.zero; car.angularVelocity = Vector3.zero;
            smoothedSteering = 0;
            phoneSteeringVelocity = 0;
            releaseSlowdownVelocity = Vector3.zero;
            contactGuard.Clear();
            if (wheelVisuals != null) wheelVisuals.ResetPose();
            cameraSnapPending = true;
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
            // The visible Transform is interpolated between physics steps. Rigidbody.position
            // is the fixed-step pose: aiming at that pose makes the rendered car jitter.
            Transform renderedCar = car.transform;
            Vector3 renderedPosition = renderedCar.position;
            if (cameraSnapPending || (renderedPosition - cameraAimPoint).sqrMagnitude > 1600f)
                SnapCamera(renderedPosition, renderedCar.rotation);
            float dt = Time.deltaTime;
            if (dt <= 0) return;
            cameraHeading = Mathf.SmoothDampAngle(cameraHeading, renderedCar.eulerAngles.y,
                ref cameraHeadingVelocity, cameraHeadingSmoothTime, Mathf.Infinity, dt);
            Vector3 target = renderedPosition + Quaternion.Euler(0, cameraHeading, 0) * new Vector3(0, 7, -12);
            view.transform.position = Vector3.SmoothDamp(view.transform.position, target,
                ref cameraFollowVelocity, cameraPositionSmoothTime, Mathf.Infinity, dt);
            cameraAimPoint = Vector3.SmoothDamp(cameraAimPoint, renderedPosition + Vector3.up,
                ref cameraAimVelocity, cameraAimSmoothTime, Mathf.Infinity, dt);
            Vector3 direction = cameraAimPoint - view.transform.position;
            if (direction.sqrMagnitude > 0.0001f)
                view.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }
        private void SnapCamera(Vector3 position, Quaternion rotation)
        {
            cameraHeading = rotation.eulerAngles.y;
            cameraHeadingVelocity = 0;
            cameraFollowVelocity = cameraAimVelocity = Vector3.zero;
            cameraAimPoint = position + Vector3.up;
            view.transform.position = position + Quaternion.Euler(0, cameraHeading, 0) * new Vector3(0, 7, -12);
            view.transform.rotation = Quaternion.LookRotation(cameraAimPoint - view.transform.position, Vector3.up);
            cameraSnapPending = false;
        }
    }
}

