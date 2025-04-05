using System;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] Sound[] sfxSounds;
    [SerializeField] AudioSource sfxSource;

    private void Awake()
    {
        if (Instance)
        {
            Debug.LogError("AudioManager already exists!");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }


    public void PlaySFX(string name)
    {
        Sound sound = Array.Find(sfxSounds, s => s.name == name);
        if (sound == null)
        {
            Debug.Log("Sound not found!");
        }
        else
        {
            sfxSource.PlayOneShot(sound.clip);
        }
    }

    public void PlayClickSound()
    {
        PlaySFX("Click");
    }
}
