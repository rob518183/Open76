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
				Vector3 finalPosition = Vector3.zero;
                Quaternion finalRotation = Quaternion.identity;
                
                if (spawnPoint != null)
                {
					finalPosition = spawnPoint.transform.position;
                    finalRotation = spawnPoint.transform.rotation;
                }
                else
                {
                    Debug.LogError("SceneRoot: No object with tag 'Spawn' found in scene! Player spawned at (0,0,0).");
					finalPosition = new Vector3(0, 100, 0); // High default to avoid falling immediately
                }
				
				// 2. FIX: Snap to Ground Logic
                // We cast a ray from high up (Y=1000) straight down to find the terrain/floor.
                RaycastHit hit;
                // Ensure your Terrain GameObject is on the "Terrain" or "Default" layer!
                int layerMask = LayerMask.GetMask("Terrain", "Default"); 
                
                if (Physics.Raycast(new Vector3(finalPosition.x, 1000f, finalPosition.z), Vector3.down, out hit, 2000f, layerMask))
                {
                    // Found ground! Move spawn point to hit point + 1.5 meters buffer
                    finalPosition.y = hit.point.y + 1.5f;
                }
                else
                {
                    // Fallback: If raycast misses (e.g. terrain hole), just assume safe height
                    finalPosition.y = Mathf.Max(finalPosition.y, 50f);
                }

                // 3. Apply Position
                importedVcf.transform.position = finalPosition;
                importedVcf.transform.rotation = finalRotation;
				
                // Attach Camera
                if (CameraManager.Instance != null && CameraManager.Instance.MainCamera != null)
                {
                    var smoothFollow = CameraManager.Instance.MainCamera.GetComponent<SmoothFollow>();
                    if (smoothFollow != null)
                    {
                        smoothFollow.Target = importedVcf.transform;
						// Force camera to snap immediately so it doesn't "swoop" from 0,0,0
                        // (Assuming SmoothFollow has a method/property for instant snap, or just disabling/enabling it)
                        smoothFollow.transform.position = finalPosition + new Vector3(0, 5, -10);
                        smoothFollow.transform.LookAt(finalPosition);
                    }
                }
            }
            else
            {
                Debug.LogError($"SceneRoot: Failed to import VCF '{VcfToLoad}'");
            }
        }

    }
}