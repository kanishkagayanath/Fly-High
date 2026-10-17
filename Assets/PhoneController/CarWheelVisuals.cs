using System.Collections.Generic;
using UnityEngine;

namespace FlyHigh.PhoneController
{
    /// <summary>Visual wheel motion; the existing car rigidbody owns all physics.</summary>
    public sealed class CarWheelVisuals : MonoBehaviour
    {
        private sealed class Wheel
        {
            public Transform transform;
            public Quaternion restRotation;
            public Vector3 rollAxis, steerAxis;
            public float radius, phase;
            public bool front;
        }

        private readonly List<Wheel> wheels = new List<Wheel>(4);

        public void Initialize(Transform model, Transform car)
        {
            wheels.Clear();
            foreach (var part in model.GetComponentsInChildren<Transform>(true))
            {
                bool front = part.name == "Car_13_Wheel_Front_Left" || part.name == "Car_13_Wheel_Front_Right";
                bool rear = part.name == "Car_13_Wheel_Rear_Left" || part.name == "Car_13_Wheel_Rear_Right";
                if (!front && !rear) continue;
                var renderers = part.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0) continue;
                Bounds bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                // Cache car-space axes in each wheel parent's space, preserving mirrored/import rotations.
                wheels.Add(new Wheel
                {
                    transform = part, restRotation = part.localRotation, front = front,
                    rollAxis = part.parent.InverseTransformDirection(car.right).normalized,
                    steerAxis = part.parent.InverseTransformDirection(car.up).normalized,
                    radius = Mathf.Max(0.05f, bounds.size.y * 0.5f)
                });
            }
            if (wheels.Count != 4)
                Debug.LogWarning("[Car wheels] Expected four named Car_13 wheels; found " + wheels.Count);
        }

        public void Animate(float forwardSpeed, float steeringDegrees, float deltaTime)
        {
            foreach (var wheel in wheels)
            {
                if (wheel.transform == null) continue;
                wheel.phase = Mathf.Repeat(wheel.phase + forwardSpeed / wheel.radius * Mathf.Rad2Deg * deltaTime, 360);
                wheel.transform.localRotation = Quaternion.AngleAxis(wheel.front ? steeringDegrees : 0, wheel.steerAxis)
                    * Quaternion.AngleAxis(wheel.phase, wheel.rollAxis) * wheel.restRotation;
            }
        }

        public void ResetPose()
        {
            foreach (var wheel in wheels)
            {
                wheel.phase = 0;
                if (wheel.transform != null) wheel.transform.localRotation = wheel.restRotation;
            }
        }
    }
}
