using UnityEngine;
using UnityEngine.InputSystem;

namespace FlyHigh.PhoneController
{
    // Both phone modes use the same landscape angle, dead centre and smoothing.
    internal sealed class TiltSteering
    {
        private Accelerometer sensor;
        private float centre;
        private bool hasCentre;

        public float Value { get; private set; }

        public void Calibrate()
        {
            hasCentre = false;
            Value = 0;
            if (TryReadAngle(out float angle))
            {
                centre = angle;
                hasCentre = true;
            }
        }

        public bool Sample(float deltaTime)
        {
            if (!TryReadAngle(out float angle))
            {
                Value = 0;
                return false;
            }

            if (!hasCentre)
            {
                centre = angle;
                hasCentre = true;
            }

            float target = Mathf.Clamp(Mathf.DeltaAngle(centre, angle) / 35f, -1f, 1f);
            Value = Mathf.Lerp(Value, target, 1f - Mathf.Exp(-12f * deltaTime));
            return true;
        }

        private bool TryReadAngle(out float angle)
        {
            angle = 0;
            if (sensor != Accelerometer.current)
            {
                sensor = Accelerometer.current;
                hasCentre = false;
                if (sensor != null) InputSystem.EnableDevice(sensor);
            }
            if (sensor == null || !sensor.enabled) return false;

            // The Input System compensates these axes for landscape orientation.
            Vector3 gravity = sensor.acceleration.ReadValue();
            if (gravity.x * gravity.x + gravity.y * gravity.y < .04f) return false;
            angle = Mathf.Atan2(gravity.x, -gravity.y) * Mathf.Rad2Deg;
            return !float.IsNaN(angle);
        }
    }
}
