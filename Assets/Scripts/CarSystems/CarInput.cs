using Assets.Scripts.Camera;
using Assets.Scripts.Entities;
using UnityEngine;
// 1. Add the New Input System Namespace
using UnityEngine.InputSystem; 

namespace Assets.Scripts.CarSystems
{
    internal class CarInput : MonoBehaviour
    {
        private Car _car;
        private CarPhysics _carPhysics;

        private void Start()
        {
            _car = GetComponent<Car>();
            _carPhysics = GetComponent<CarPhysics>();
        }

        private void Update()
        {
            // Safety Checks
            if (CameraManager.Instance == null || !CameraManager.Instance.IsMainCameraActive || !_car.Alive)
            {
                return;
            }
            
            // 2. Poll Input Devices safely
            var kb = Keyboard.current;
            var gp = Gamepad.current;

            // If no keyboard connected (e.g. console/mobile), skip keyboard blocks to avoid null errors
            if (kb != null)
            {
                // --- System Commands ---
                if (kb.kKey.wasPressedThisFrame) _car.Kill();
                if (kb.zKey.wasPressedThisFrame) Car.FireWeapons = !Car.FireWeapons;
                if (kb.sKey.wasPressedThisFrame) _car.ToggleEngine();

                // --- Radar Controls ---
                if (kb.eKey.wasPressedThisFrame) _car.RadarPanel.CycleTarget();
                if (kb.rKey.wasPressedThisFrame) _car.RadarPanel.ToggleRange();
                if (kb.tKey.wasPressedThisFrame) _car.RadarPanel.TargetNearest();
                if (kb.yKey.wasPressedThisFrame) _car.RadarPanel.ClearTarget();

                // --- Weapon Cycling ---
                if (kb.enterKey.wasPressedThisFrame) _car.WeaponsController.CycleWeapon();
            }

            // --- Firing Logic (Keyboard) ---
            bool isFiringAnything = false;

            if (kb != null)
            {
                if (kb.spaceKey.isPressed) 
                {
                    _car.WeaponsController.Fire(-1);
                    isFiringAnything = true;
                }
                else if (kb.digit1Key.isPressed) { _car.WeaponsController.Fire(0); isFiringAnything = true; }
                else if (kb.digit2Key.isPressed) { _car.WeaponsController.Fire(1); isFiringAnything = true; }
                else if (kb.digit3Key.isPressed) { _car.WeaponsController.Fire(2); isFiringAnything = true; }
                else if (kb.digit4Key.isPressed) { _car.WeaponsController.Fire(3); isFiringAnything = true; }
                else if (kb.digit5Key.isPressed) { _car.WeaponsController.Fire(4); isFiringAnything = true; }
                else if (kb.digit6Key.isPressed) { _car.SpecialsController.Fire(0); isFiringAnything = true; }

                if (kb.digit7Key.isPressed) _car.SpecialsController.Fire(1);
                if (kb.digit8Key.isPressed) _car.SpecialsController.Fire(2);
            }

            // Gamepad Firing (Right Trigger)
            if (gp != null && gp.rightTrigger.isPressed)
            {
                 _car.WeaponsController.Fire(-1);
                 isFiringAnything = true;
            }

            if (!isFiringAnything)
            {
                _car.WeaponsController.StopFiring();
            }

            // --- Driving Physics ---
            float throttleInput = 0f;
            float steeringInput = 0f;
            bool eBrakeInput = false;

            // Keyboard Axis Calculation (Simulating GetAxis("Vertical"))
            if (kb != null)
            {
                // Throttle (W / Up Arrow)
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) throttleInput += 1f;
                // Brake/Reverse (S / Down Arrow)
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) throttleInput -= 1f;

                // Steering (A / D / Arrows)
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) steeringInput += 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) steeringInput -= 1f;

                // E-Brake (Shift)
                if (kb.leftShiftKey.isPressed) eBrakeInput = true;
            }

            // Gamepad Axis Calculation
            if (gp != null)
            {
                throttleInput += gp.leftStick.y.ReadValue();
                steeringInput += gp.leftStick.x.ReadValue();
                if (gp.buttonSouth.isPressed) eBrakeInput = true; // A/Cross button
            }

            // Clamp values
            throttleInput = Mathf.Clamp(throttleInput, -1f, 1f);
            steeringInput = Mathf.Clamp(steeringInput, -1f, 1f);

            // Physics Application
            float throttle = Mathf.Max(0, throttleInput);
            float brake = -Mathf.Min(0, throttleInput);

            if (!_car.EngineRunning)
            {
                throttle = 0f;
            }

            _carPhysics.Throttle = throttle;
            _carPhysics.Brake = brake;
            _carPhysics.Steer = steeringInput;
            _carPhysics.EBrake = eBrakeInput;
        }
    }
}