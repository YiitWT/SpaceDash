using UnityEngine;

public static class AudioPrefs
{
    private const string MusicVolumeKey = "MusicVolume";
    private const string SfxVolumeKey = "SfxVolume";
    private const string LegacyAudioKey = "audioPrefKey";

    public static float GetMusicVolume(float defaultValue = 1f)
    {
        if (PlayerPrefs.HasKey(MusicVolumeKey))
        {
            return PlayerPrefs.GetFloat(MusicVolumeKey, defaultValue);
        }

        if (PlayerPrefs.HasKey(LegacyAudioKey))
        {
            return PlayerPrefs.GetFloat(LegacyAudioKey, defaultValue);
        }

        return defaultValue;
    }

    public static float GetSfxVolume(float defaultValue = 1f)
    {
        if (PlayerPrefs.HasKey(SfxVolumeKey))
        {
            return PlayerPrefs.GetFloat(SfxVolumeKey, defaultValue);
        }

        if (PlayerPrefs.HasKey(LegacyAudioKey))
        {
            return PlayerPrefs.GetFloat(LegacyAudioKey, defaultValue);
        }

        return defaultValue;
    }

    public static void SetMusicVolume(float volume)
    {
        PlayerPrefs.SetFloat(MusicVolumeKey, volume);
        PlayerPrefs.Save();
    }

    public static void SetSfxVolume(float volume)
    {
        PlayerPrefs.SetFloat(SfxVolumeKey, volume);
        PlayerPrefs.Save();
    }
}
