using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace FlyHigh.PhoneController
{
    /// <summary>Feeds the local car from touch and tilt while the driving HUD is active.</summary>
    public sealed class MobileDriveControls : MonoBehaviour
    {
        private readonly TiltSteering tilt = new TiltSteering();
        private readonly List<HoldPedal> holds = new List<HoldPedal>(5);
        private DesktopMainMenu menu;
        private RectTransform root;
        private RectTransform touchSteering;
        private TMP_Text hint;
        private bool forward, reverse, brake, left, right, tiltReady;
        private bool focused = true, suspended;
        private float hintUntil;

        private bool CanDrive => isActiveAndEnabled && focused && !suspended &&
            menu != null && menu.View == DriveMenuView.Driving;
        public float Steering => CanDrive ? tiltReady ? tilt.Value : (right ? 1f : 0f) - (left ? 1f : 0f) : 0f;
        public float Throttle => CanDrive ? (forward ? 1f : 0f) - (reverse ? 1f : 0f) : 0f;
        public float Brake => !CanDrive || brake ? 1f : 0f;

        public void Initialize(RectTransform hud, DesktopMainMenu navigation)
        {
            menu = navigation;
            root = PhoneUi.Rect(hud, "Mobile drive controls", 0, 0, 1, 1);
            PhoneUi.Button(root, "Calibrate tilt", "CALIBRATE", .18f, .888f, .34f, .974f,
                Calibrate, PhoneUi.Surface, PhoneUi.Cyan);
            AddHold(root, "Mobile reverse", "REVERSE", .025f, .205f, .17f, .325f,
                new Color(.23f, .18f, .37f, .93f), held => reverse = held);
            AddHold(root, "Mobile brake", "BRAKE", .025f, .035f, .17f, .18f,
                new Color(.61f, .17f, .16f, .94f), held => brake = held);
            AddHold(root, "Mobile accelerate", "ACCELERATE", .825f, .035f, .975f, .222f,
                new Color(.05f, .45f, .48f, .94f), held => forward = held);

            touchSteering = PhoneUi.Rect(root, "Touch steering fallback", 0, 0, 1, 1);
            AddHold(touchSteering, "Mobile steer left", "LEFT", .18f, .035f, .27f, .14f,
                PhoneUi.Surface, held => left = held);
            AddHold(touchSteering, "Mobile steer right", "RIGHT", .29f, .035f, .38f, .14f,
                PhoneUi.Surface, held => right = held);
            touchSteering.gameObject.SetActive(false);

            var banner = PhoneUi.Panel(root, "Tilt instructions", .355f, .915f, .74f, .971f,
                new Color(.04f, .08f, .12f, .92f), PhoneUi.Edge);
            hint = PhoneUi.Text(banner.transform, "Tilt status", "", .02f, .07f, .98f, .93f, 19,
                PhoneUi.White, false, TextAlignmentOptions.Center);
            Calibrate();
        }

        private void AddHold(RectTransform parent, string name, string caption,
            float leftEdge, float bottom, float rightEdge, float top, Color fill, Action<bool> change)
        {
            var button = PhoneUi.Button(parent, name, caption, leftEdge, bottom, rightEdge, top,
                null, fill, PhoneUi.Edge);
            var hold = button.gameObject.AddComponent<HoldPedal>();
            hold.Changed = change;
            holds.Add(hold);
        }

        private void Calibrate()
        {
            tilt.Calibrate();
            hintUntil = Time.unscaledTime + 5f;
        }

        private void Update()
        {
            if (!CanDrive || root == null) return;
            tiltReady = tilt.Sample(Time.unscaledDeltaTime);
            if (touchSteering.gameObject.activeSelf == tiltReady)
                touchSteering.gameObject.SetActive(!tiltReady);

            string message = tiltReady ? "Tilt to steer. Hold level and tap CALIBRATE to centre." :
                "Hold the phone upright to tilt, or use LEFT and RIGHT.";
            if (hint.text != message) hint.text = message;
            bool showHint = !tiltReady || Time.unscaledTime < hintUntil;
            if (hint.transform.parent.gameObject.activeSelf != showHint)
                hint.transform.parent.gameObject.SetActive(showHint);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            focused = hasFocus;
            if (!hasFocus) InterruptDrive();
        }

        private void OnApplicationPause(bool paused)
        {
            suspended = paused;
            if (paused) InterruptDrive();
        }

        private void InterruptDrive()
        {
            ReleasePedals();
            if (menu != null) menu.OpenPause();
        }

        private void ReleasePedals()
        {
            foreach (var hold in holds)
                if (hold != null) hold.Release();
        }

        private void OnDisable() => ReleasePedals();
    }
}
