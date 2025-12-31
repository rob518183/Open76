using System.Collections;
using Assets.Scripts.Camera;
using Assets.Scripts.CarSystems;
using Assets.Scripts.Entities; // Assuming Vdf/Vcf are here or related
using Assets.Scripts.System;
using Assets.Scripts.System.Fileparsers;
using UnityEngine;

namespace Assets.Scripts
{
    [RequireComponent(typeof(AudioSource))]
    public class SceneRoot : MonoBehaviour
    {
        [Header("Configuration")]
        public string GamePath;
        public string MissionFile;
        public string VcfToLoad;

        [Header("Music Settings")]
        [Tooltip("Minimum index for random music track selection")]
        public int MusicMinIndex = 2;
        [Tooltip("Maximum index for random music track selection (exclusive)")]
        public int MusicMaxIndex = 18;

        private IEnumerator Start()
        {
#if UNITY_EDITOR
            // Helper for hearing audio when not maximizing the game view
            if (GetComponent<SceneViewAudioHelper>() == null)
            {
                gameObject.AddComponent<SceneViewAudioHelper>();
            }
#endif

            // 1. Setup Global Paths (Fallback if starting directly from scene in Editor)
            if (string.IsNullOrEmpty(Game.Instance.GamePath))
            {
                Game.Instance.GamePath = GamePath;
            }

            // 2. Override Mission File (If coming from Main Menu)
            if (!string.IsNullOrEmpty(Game.Instance.LevelName))
            {
                MissionFile = Game.Instance.LevelName;
            }

            // 3. Load the Level Geometry/Data
            yield return LevelLoader.Instance.LoadLevel(MissionFile);

            // 4. Initialize Player (Only for Missions starting with 'm')
            // Using ToLowerInvariant for safer string comparison across cultures
            if (!string.IsNullOrEmpty(MissionFile) && MissionFile.ToLowerInvariant().StartsWith("m"))
            {
                InitializePlayer();
            }

            // 5. Start Music
            PlayLevelMusic();
        }

        private void InitializePlayer()
        {
            // Import the Vehicle
            CacheManager cacheManager = CacheManager.Instance;
            
            // Note: We use 'out _' to discard the Vdf result if we don't need it locally
            GameObject importedVcf = cacheManager.ImportVcf(VcfToLoad, true, out _);
            
            if (importedVcf != null)
            {
                // Make it playable
                importedVcf.AddComponent<CarInput>();
                // Tag it as Player so AI knows who to target
                var car = importedVcf.GetComponent<Car>();
                if (car != null) car.IsPlayer = true;

                // Find Spawn Point safely
                GameObject spawnPoint = GameObject.FindGameObjectWithTag("Spawn");
                
                if (spawnPoint != null)
                {
                    importedVcf.transform.position = spawnPoint.transform.position;
                    importedVcf.transform.rotation = spawnPoint.transform.rotation;
                }
                else
                {
                    Debug.LogError("SceneRoot: No object with tag 'Spawn' found in scene! Player spawned at (0,0,0).");
                }

                // Attach Camera
                if (CameraManager.Instance != null && CameraManager.Instance.MainCamera != null)
                {
                    var smoothFollow = CameraManager.Instance.MainCamera.GetComponent<SmoothFollow>();
                    if (smoothFollow != null)
                    {
                        smoothFollow.Target = importedVcf.transform;
                    }
                }
            }
            else
            {
                Debug.LogError($"SceneRoot: Failed to import VCF '{VcfToLoad}'");
            }
        }

        private void PlayLevelMusic()
        {
            var musicAudioSource = GetComponent<AudioSource>();
            if (VirtualFilesystem.Instance != null)
            {
                // Clamped random range to prevent errors if indices are invalid
                int trackIndex = Random.Range(MusicMinIndex, MusicMaxIndex);
                var clip = VirtualFilesystem.Instance.GetMusicClip(trackIndex);
                
                if (clip != null)
                {
                    musicAudioSource.clip = clip;
                    musicAudioSource.Play();
                }
                else
                {
                    Debug.LogWarning($"SceneRoot: Music clip at index {trackIndex} not found.");
                }
            }
        }
    }
}