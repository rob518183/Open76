using Assets.Scripts.System;
using Assets.Scripts.System.Fileparsers;
using UnityEngine;

namespace Assets.Scripts
{
    [RequireComponent(typeof(AudioSource))]
    public class MusicPlayer : MonoBehaviour
    {
        private static MusicPlayer _instance;
        public static MusicPlayer Instance { get; private set; }

        [Header("Config")]
        public int MinTrackIndex = 2;
        public int MaxTrackIndex = 18;
        public bool PlayOnStart = true;

        private AudioSource _audioSource;

        private void Awake()
        {
            // Singleton Pattern: Ensure only one MusicPlayer exists
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            // Uncomment the next line if you want music to keep playing when scene changes
            // DontDestroyOnLoad(gameObject); 

            _audioSource = GetComponent<AudioSource>();
        }

        private void Start()
        {
            if (PlayOnStart)
            {
                PlayRandomMusic();
            }
        }

        public void PlayRandomMusic()
        {
            if (VirtualFilesystem.Instance == null)
            {
                Debug.LogWarning("MusicPlayer: VirtualFilesystem not initialized.");
                return;
            }

            // Using the logic extracted from your old SceneRoot
            int trackIndex = Random.Range(MinTrackIndex, MaxTrackIndex);
            AudioClip clip = VirtualFilesystem.Instance.GetMusicClip(trackIndex);

            if (clip != null)
            {
                _audioSource.clip = clip;
                _audioSource.Play();
            }
            else
            {
                Debug.LogWarning($"MusicPlayer: Could not load music track {trackIndex}");
            }
        }
    }
}