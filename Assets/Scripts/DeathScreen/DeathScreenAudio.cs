using UnityEngine;

public class DeathScreenAudio : MonoBehaviour
{
    [Header("---Audio Source---")]
    [SerializeField] private AudioSource music;

    [Header("---Audio Clips---")]
    [SerializeField] private AudioClip[] musicClips;


    private void Start()
    {
        LoadVolumeSettings();
        PlayMusic();
    }

    private void PlayMusic()
    {
        if (music != null && musicClips != null && musicClips.Length > 0)
        {
            music.clip = musicClips[Random.Range(0, musicClips.Length)];
            music.Play();
        }
    }

    public void StopMusic()
    {
        if (music != null)
        {
            music.Stop();
        }
    }

    private void LoadVolumeSettings()
    {
        float musicVolume = AudioPrefs.GetMusicVolume(1f);

        if (music != null)
        {
            music.volume = musicVolume;
        }
    }

    public void SetMusicVolume(float volume)
    {
        if (music != null)
        {
            music.volume = volume;
        }

        AudioPrefs.SetMusicVolume(volume);
    }

    public void SetSFXVolume(float volume)
    {
        AudioPrefs.SetSfxVolume(volume);
    }
}
