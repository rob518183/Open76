using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Camera
{
    public class CameraManager
    {
        private readonly Stack<FSMCamera> _cameraStack;
        private GameObject _mainCameraObject; // Changed to simple GameObject reference
        private bool _audioEnabled;

        // Singleton Instance
        private static CameraManager _instance;
        public static CameraManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new CameraManager();
                return _instance;
            }
        }

        // Helper to safely get the Unity Camera component
        public UnityEngine.Camera MainCamera
        {
            get 
            { 
                // Safety check: If scene reloaded, this might be null or "missing"
                if (_mainCameraObject == null) return null;
                return _mainCameraObject.GetComponent<UnityEngine.Camera>(); 
            }
        }

        public FSMCamera ActiveCamera
        {
            get
            {
                // Clean up stack if objects were destroyed externally
                while (_cameraStack.Count > 0 && _cameraStack.Peek() == null)
                {
                    _cameraStack.Pop();
                }

                if (_cameraStack.Count > 0)
                {
                    return _cameraStack.Peek();
                }

                return null;
            }
        }

        public bool AudioEnabled
        {
            get { return _audioEnabled; }
            set
            {
                _audioEnabled = value;
                UpdateAudioListenerState();
            }
        }

        public bool IsMainCameraActive
        {
            get 
            { 
                if (MainCamera == null || ActiveCamera == null) return false;
                // Compare GameObjects, not Component types
                return MainCamera.gameObject == ActiveCamera.gameObject; 
            }
        }

private CameraManager()
        {
            _cameraStack = new Stack<FSMCamera>();
            _audioEnabled = true;

            // 1. Try to find an existing FSMCamera
            var targetCamera = Object.FindFirstObjectByType<FSMCamera>();

            // 2. If missing, find the standard Unity Camera and auto-add the script
            if (targetCamera == null)
            {
                var vanillaCamera = Object.FindFirstObjectByType<UnityEngine.Camera>();
                if (vanillaCamera != null)
                {
                    Debug.Log("CameraManager: FSMCamera component missing. Auto-adding it to " + vanillaCamera.name);
                    targetCamera = vanillaCamera.gameObject.AddComponent<FSMCamera>();
                }
            }

            // 3. Initialize Stack
            if (targetCamera != null)
            {
                _mainCameraObject = targetCamera.gameObject;
                _cameraStack.Push(targetCamera);
            }
            else
            {
                Debug.LogError("CameraManager: CRITICAL - No Camera found in scene at all!");
            }

            _audioEnabled = true;
        }
        
        public void PushCamera()
        {
            // 1. Disable current top camera
            if (_cameraStack.Count > 0 && _cameraStack.Peek() != null)
            {
                var currentCamera = _cameraStack.Peek();
                currentCamera.gameObject.SetActive(false);
            }

            // 2. Create new Camera
            GameObject newCameraObject = new GameObject("Stack Camera " + (_cameraStack.Count + 1));
            newCameraObject.AddComponent<UnityEngine.Camera>();
            // Copy clear flags/depth from main if needed? usually desirable.
            
            var newCamera = newCameraObject.AddComponent<FSMCamera>();
            newCameraObject.AddComponent<AudioListener>();
            
            // 3. Push and Update Audio
            _cameraStack.Push(newCamera);
            UpdateAudioListenerState();
        }

        public void PopCamera()
        {
            if (_cameraStack.Count == 0) return;

            // 1. Remove and Destroy current
            var stackCamera = _cameraStack.Pop();
            if (stackCamera != null)
            {
                Object.Destroy(stackCamera.gameObject);
            }

            // 2. Re-enable the previous camera
            if (_cameraStack.Count > 0)
            {
                var previousCamera = _cameraStack.Peek();
                if (previousCamera != null)
                {
                    // FIXED: Was SetActive(false), must be true to restore it
                    previousCamera.gameObject.SetActive(true); 
                    
                    // Ensure audio state is correct for the restored camera
                    UpdateAudioListenerState(); 
                }
            }
        }

        // Helper to apply audio settings to the CURRENTLY active camera
        private void UpdateAudioListenerState()
        {
            var activeCam = ActiveCamera;
            if (activeCam != null)
            {
                var listener = activeCam.GetComponent<AudioListener>();
                if (listener != null)
                {
                    listener.enabled = _audioEnabled;
                }
            }
        }

        public void Destroy()
        {
            while (_cameraStack.Count > 0)
            {
                var stackCamera = _cameraStack.Pop();
                if (stackCamera != null)
                    Object.Destroy(stackCamera.gameObject);
            }

            _instance = null;
        }
    }
}