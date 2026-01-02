using System.Collections.Generic;
using Assets.Scripts.CarSystems;
using Assets.Scripts.Entities;
using UnityEngine;
using UnityEngine.XR;
// 1. Add New Input System Namespace
using UnityEngine.InputSystem; 

namespace Assets.Scripts.Camera
{
    [RequireComponent(typeof(SmoothFollow))]
    public class CameraController : MonoBehaviour
    {
        private SmoothFollow _smoothFollow;
        private Car _player;
        
        // Cached references
        private Transform _thirdPersonChassis;
        private Transform _firstPersonChassis;
        private RaySusp[] _suspensions;
        private List<Transform> _vlocPoints = new List<Transform>();
        private bool _referencesInitialized = false;

        public bool FirstPerson { get; private set; }

        private enum ChassisView
        {
            FirstPerson,
            ThirdPerson,
            AllHidden
        }

        private void Start()
        {
            _smoothFollow = GetComponent<SmoothFollow>();
        }

        private void Update()
        {
            if (CameraManager.Instance != null && !CameraManager.Instance.IsMainCameraActive)
            {
                return;
            }

            if (_player == null)
            {
                AttemptFindPlayer();
                return; 
            }

            if (!_player.Alive)
            {
                if (FirstPerson)
                {
                    SetCameraThirdPerson();
                    FirstPerson = false;
                }
                return;
            }
            
            HandleViewSwitching();
            HandleXRInput();

            if (FirstPerson)
            {
                HandleFirstPersonRotation();
            }
        }

        private void AttemptFindPlayer()
        {
            Transform target = _smoothFollow.Target;
            if (target != null)
            {
                _player = target.GetComponent<Car>();
                if (_player != null && !_referencesInitialized)
                {
                    InitializePlayerReferences();
                }
            }
        }

        private void InitializePlayerReferences()
        {
            _thirdPersonChassis = _player.transform.Find("Chassis/ThirdPerson");
            _firstPersonChassis = _player.transform.Find("Chassis/FirstPerson");
            _suspensions = _player.GetComponentsInChildren<RaySusp>();

            _vlocPoints.Clear();
            foreach (Transform child in _player.transform)
            {
                if (child.name == "VLOC")
                {
                    _vlocPoints.Add(child);
                }
            }
            _referencesInitialized = true;
        }

        private void HandleViewSwitching()
        {
            // 2. Poll Keyboard safely
            var kb = Keyboard.current;
            if (kb == null) return;

            // F1 - F3: Main Views
            if (kb.f1Key.wasPressedThisFrame)
            {
                SetCameraFirstPersonAtVLOCIndex(0);
                SetVisibleChassisModel(ChassisView.FirstPerson);
                FirstPerson = true;
            }
            else if (kb.f2Key.wasPressedThisFrame)
            {
                SetCameraThirdPerson();
                FirstPerson = false;
            }
            else if (kb.f3Key.wasPressedThisFrame)
            {
                SetCameraFirstPersonAtVLOCIndex(1);
                SetVisibleChassisModel(ChassisView.AllHidden);
                FirstPerson = false;
            }
            // F4 - F7: Wheel Views
            else if (kb.f4Key.wasPressedThisFrame) SwitchToWheel(0);
            else if (kb.f5Key.wasPressedThisFrame) SwitchToWheel(1);
            else if (kb.f6Key.wasPressedThisFrame) SwitchToWheel(2);
            else if (kb.f7Key.wasPressedThisFrame) SwitchToWheel(3);
        }

        private void SwitchToWheel(int index)
        {
            SetCameraAtWheelIndex(index);
            SetVisibleChassisModel(ChassisView.ThirdPerson);
            FirstPerson = false;
        }

        private void HandleXRInput()
        {
            // Reset VR position
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                var subsystems = new List<XRInputSubsystem>();
                SubsystemManager.GetSubsystems(subsystems);
                if (subsystems.Count > 0)
                {
                    subsystems[0].TryRecenter();
                }
            }
        }

        private void HandleFirstPersonRotation()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            Quaternion targetRotation = Quaternion.Euler(-14, 0, 0);

            // Numpad Look logic using 'isPressed'
            if (kb.numpad6Key.isPressed)      targetRotation = Quaternion.Euler(-14, 90, 0);  // Right
            else if (kb.numpad2Key.isPressed) targetRotation = Quaternion.Euler(-14, 180, 0); // Back
            else if (kb.numpad4Key.isPressed) targetRotation = Quaternion.Euler(-14, -90, 0); // Left
            
            if (kb.numpad8Key.isPressed)      targetRotation = Quaternion.Euler(7, 0, 0);     // Up

            transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime * 6);
        }

        private void SetCameraAtWheelIndex(int wheelIndex)
        {
            if (_suspensions == null || wheelIndex >= _suspensions.Length) return;

            var wheel = _suspensions[wheelIndex].transform;
            var meshTransform = wheel.Find("Mesh");
            
            if (meshTransform != null && meshTransform.childCount > 0)
            {
                var target = meshTransform.GetChild(0);
                transform.parent = wheel;
                transform.localPosition = target.localPosition;
                transform.localRotation = Quaternion.Euler(-14, 0, 0);
                _smoothFollow.enabled = false;
            }
        }

        private void SetCameraThirdPerson()
        {
            _smoothFollow.Target = _player.transform;
            _smoothFollow.enabled = true;
            transform.parent = null;

            SetVisibleChassisModel(ChassisView.ThirdPerson);
        }

        private void SetCameraFirstPersonAtVLOCIndex(int vlocIndex)
        {
            if (vlocIndex >= _vlocPoints.Count)
            {
                Debug.LogWarning($"Cannot find VLOC with index {vlocIndex}");
                return;
            }

            Transform vloc = _vlocPoints[vlocIndex];
            transform.parent = vloc;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.Euler(-14, 0, 0);
            _smoothFollow.enabled = false;
        }

        private void SetVisibleChassisModel(ChassisView chassisView)
        {
            if (_thirdPersonChassis != null)
                _thirdPersonChassis.gameObject.SetActive(chassisView == ChassisView.ThirdPerson);

            if (_firstPersonChassis != null)
                _firstPersonChassis.gameObject.SetActive(chassisView == ChassisView.FirstPerson);

            if (_suspensions != null)
            {
                bool showWheels = (chassisView == ChassisView.ThirdPerson);
                foreach (var suspension in _suspensions)
                {
                    suspension.SetWheelVisibile(showWheels);
                }
            }
        }

        public void SetCameraPositionAndLookAt(Vector3 position, Vector3 lookat)
        {
            transform.position = position;
            transform.LookAt(lookat);
        }
    }
}