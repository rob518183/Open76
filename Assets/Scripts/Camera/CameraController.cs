using System.Collections.Generic;
using Assets.Scripts.CarSystems;
using Assets.Scripts.Entities;
using UnityEngine;
using UnityEngine.XR;

namespace Assets.Scripts.Camera
{
    [RequireComponent(typeof(SmoothFollow))]
    public class CameraController : MonoBehaviour
    {
        private SmoothFollow _smoothFollow;
        private Car _player;
        
        // Cached references to avoid calling .Find() every frame/click
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
            // Dependency Check
            if (CameraManager.Instance != null && !CameraManager.Instance.IsMainCameraActive)
            {
                return;
            }

            // 1. Player Initialization
            if (_player == null)
            {
                AttemptFindPlayer();
                return; 
            }

            // 2. Health Check
            if (!_player.Alive)
            {
                if (FirstPerson) // Only switch if we haven't already
                {
                    SetCameraThirdPerson();
                    FirstPerson = false;
                }
                return;
            }
            
            // 3. Input Handling
            HandleViewSwitching();
            HandleXRInput();

            // 4. First Person Rotation
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
            // Cache Chassis parts
            _thirdPersonChassis = _player.transform.Find("Chassis/ThirdPerson");
            _firstPersonChassis = _player.transform.Find("Chassis/FirstPerson");

            // Cache Suspensions
            _suspensions = _player.GetComponentsInChildren<RaySusp>();

            // Cache VLOC points (Virtual Location of Camera)
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
            if (Input.GetKeyDown(KeyCode.F1))
            {
                SetCameraFirstPersonAtVLOCIndex(0);
                SetVisibleChassisModel(ChassisView.FirstPerson);
                FirstPerson = true;
            }
            else if (Input.GetKeyDown(KeyCode.F2))
            {
                SetCameraThirdPerson();
                FirstPerson = false;
            }
            else if (Input.GetKeyDown(KeyCode.F3))
            {
                SetCameraFirstPersonAtVLOCIndex(1);
                SetVisibleChassisModel(ChassisView.AllHidden);
                FirstPerson = false;
            }
            // Wheel Cameras (F4 - F7)
            else if (Input.GetKeyDown(KeyCode.F4)) SwitchToWheel(0);
            else if (Input.GetKeyDown(KeyCode.F5)) SwitchToWheel(1);
            else if (Input.GetKeyDown(KeyCode.F6)) SwitchToWheel(2);
            else if (Input.GetKeyDown(KeyCode.F7)) SwitchToWheel(3);
        }

        private void SwitchToWheel(int index)
        {
            SetCameraAtWheelIndex(index);
            SetVisibleChassisModel(ChassisView.ThirdPerson);
            FirstPerson = false;
        }

        private void HandleXRInput()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                // MODERN XR RECENTERING
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
            Quaternion targetRotation = Quaternion.Euler(-14, 0, 0);

            // Using 'else if' ensures specific priority. 
            // Keypad 8 (Look Up/Mirror) overrides others in this logic.
            if (Input.GetKey(KeyCode.Keypad6))      targetRotation = Quaternion.Euler(-14, 90, 0);  // Right
            else if (Input.GetKey(KeyCode.Keypad2)) targetRotation = Quaternion.Euler(-14, 180, 0); // Back
            else if (Input.GetKey(KeyCode.Keypad4)) targetRotation = Quaternion.Euler(-14, -90, 0); // Left
            
            if (Input.GetKey(KeyCode.Keypad8))      targetRotation = Quaternion.Euler(7, 0, 0);     // Up

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