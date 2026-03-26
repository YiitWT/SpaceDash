using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("---Audio Source---")]
    [SerializeField] private AudioSource music;
    [SerializeField] private AudioSource sfx;

    [Header("---Audio Clips---")]
    [SerializeField] private AudioClip[] musicClips;
    public AudioClip heal;
    public AudioClip damage;
    public AudioClip gameOver;

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

    public void PlaySFX(AudioClip clip)
    {
        if (sfx != null && clip != null)
        {
            sfx.PlayOneShot(clip);
        }
    }

    private void LoadVolumeSettings()
    {
        float musicVolume = AudioPrefs.GetMusicVolume(1f);
        float sfxVolume = AudioPrefs.GetSfxVolume(1f);

        if (music != null)
        {
            music.volume = musicVolume;
        }

        if (sfx != null)
        {
            sfx.volume = sfxVolume;
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
        if (sfx != null)
        {
            sfx.volume = volume;
        }

        AudioPrefs.SetSfxVolume(volume);
    }
}
