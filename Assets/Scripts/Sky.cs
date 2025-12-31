using Assets.Scripts.Camera;
using Assets.Scripts.System;
using Assets.Scripts.System.Fileparsers;
using UnityEngine;

namespace Assets.Scripts
{
    [RequireComponent(typeof(MeshRenderer))]
    public class Sky : MonoBehaviour
    {
        public Vector2 Speed;
        public float Height;

        // Added SerializeField so you can set the default texture in the Inspector
        [SerializeField]
        private string _textureFileName;

        private Material _material;
        private MeshRenderer _meshRenderer;

        public string TextureFilename
        {
            get { return _textureFileName; }
            set
            {
                if (_textureFileName == value) return;
                _textureFileName = value;
                UpdateSkyTexture();
            }
        }

        private void Awake()
        {
            _meshRenderer = GetComponent<MeshRenderer>();
            
            // IMPORTANT: Accessing .material creates a new instance. 
            // We store reference to destroy it later.
            _material = _meshRenderer.material;

            UpdateSkyTexture();
        }

        private void Update()
        {
            // Safety Check: Ensure the camera manager and active camera exist
            if (CameraManager.Instance == null || CameraManager.Instance.ActiveCamera == null)
            {
                return;
            }

            // 1. Scroll Texture
            if (_material != null)
            {
                _material.mainTextureOffset += Speed * Time.deltaTime;
            }

            // 2. Follow Camera (Skybox effect)
            Vector3 camPos = CameraManager.Instance.ActiveCamera.transform.position;
            transform.position = camPos + (Vector3.up * Height);
        }

        private void UpdateSkyTexture()
        {
            // Guard clause: Ensure we have a material and a filename
            if (_material == null || string.IsNullOrEmpty(_textureFileName))
            {
                return;
            }

            // Guard clause: Ensure the CacheManager is ready
            if (CacheManager.Instance != null && CacheManager.Instance.Palette != null)
            {
                _material.mainTexture = TextureParser.ReadMapTexture(_textureFileName, CacheManager.Instance.Palette);
            }
        }

        private void OnDestroy()
        {
            // FIXED: Clean up the instantiated material to prevent memory leaks
            if (_material != null)
            {
                Destroy(_material);
            }
        }
    }
}