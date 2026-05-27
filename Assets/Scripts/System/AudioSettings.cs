using UnityEngine;

namespace Assets.Scripts.System
{
    public enum AudioCategory
    {
        Music,
        Sfx,
        Voice
    }

    public class AudioCategorySource : MonoBehaviour
    {
        public AudioCategory Category = AudioCategory.Sfx;
        public float BaseVolume = 1.0f;

        public float GetEffectiveVolume()
        {
            return BaseVolume * AudioSettings.GetMultiplier(Category);
        }

        public void SetBaseVolume(float volume)
        {
            BaseVolume = Mathf.Clamp01(volume);
            UpdateVolume();
        }

        public void UpdateVolume()
        {
            AudioSource audioSource = GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.volume = GetEffectiveVolume();
            }
        }
    }

    public static class AudioSettings
    {
        private const string MusicPrefKey = "Open76.Audio.MusicLevel";
        private const string SfxPrefKey = "Open76.Audio.SfxLevel";
        private const string VoicePrefKey = "Open76.Audio.VoiceLevel";

        public static float MusicLevel { get; private set; }
        public static float SfxLevel { get; private set; }
        public static float VoiceLevel { get; private set; }

        static AudioSettings()
        {
            MusicLevel = PlayerPrefs.GetFloat(MusicPrefKey, 1.0f);
            SfxLevel = PlayerPrefs.GetFloat(SfxPrefKey, 1.0f);
            VoiceLevel = PlayerPrefs.GetFloat(VoicePrefKey, 1.0f);
        }

        public static string FormatDisplay(float volume)
        {
            return (volume * 10f).ToString("0.00");
        }

        public static float NextLevel(float current)
        {
            int value = Mathf.RoundToInt(current * 10f);
            value = (value + 1) % 11;
            return value / 10f;
        }

        public static void SetMusicLevel(float level)
        {
            MusicLevel = Mathf.Clamp01(level);
            Save();
            ApplyAudioLevels();
        }

        public static void SetSfxLevel(float level)
        {
            SfxLevel = Mathf.Clamp01(level);
            Save();
            ApplyAudioLevels();
        }

        public static void SetVoiceLevel(float level)
        {
            VoiceLevel = Mathf.Clamp01(level);
            Save();
            ApplyAudioLevels();
        }

        public static float GetMultiplier(AudioCategory category)
        {
            return category switch
            {
                AudioCategory.Music => MusicLevel,
                AudioCategory.Sfx => SfxLevel,
                AudioCategory.Voice => VoiceLevel,
                _ => 1f,
            };
        }

        public static void ApplyAudioLevels()
        {
            foreach (AudioCategorySource categorySource in Object.FindObjectsByType<AudioCategorySource>())
            {
                categorySource.UpdateVolume();
            }
        }

        private static void Save()
        {
            PlayerPrefs.SetFloat(MusicPrefKey, MusicLevel);
            PlayerPrefs.SetFloat(SfxPrefKey, SfxLevel);
            PlayerPrefs.SetFloat(VoicePrefKey, VoiceLevel);
            PlayerPrefs.Save();
        }
    }
}
