﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : MonoSingleton<SoundManager>
{
    public AudioMixer audioMixer;

    public AudioSource musicAudioSource;

    public AudioSource soundAudioSource;

    private const string MusicPath = "Music/";
    private const string SoundPath = "Sound/";

    private bool musicOn;
    public bool MusicOn
    {
        get { return musicOn; }
        set 
        {
            musicOn = value;
            this.MusicMute(!musicOn);
        }
    }

    private bool soundOn;
    public bool SoundOn
    {
        get { return soundOn; }
        set
        {
            soundOn = value;
            this.SoundMute(!soundOn);
        }
    }

    private int musicVolume;
    public int MusicVolume
    {
        get { return musicVolume; }
        set 
        {
            if (musicVolume != value)
            {
                musicVolume = value;
                if (musicOn)
                {
                    this.SetVolume("MusicVolume", musicVolume);
                }
            }        
        }
    }
    private int soundVolume;
    public int SoundVolume
    {
        get { return soundVolume; }
        set
        {
            if (soundVolume != value)
            {
                soundVolume = value;
                if (soundOn)
                {
                    this.SetVolume("SoundVolume", soundVolume);
                }
            }
        }
    }

    private void Start()
    {
        this.MusicVolume = Config.MusicVolume;
        this.SoundVolume = Config.SoundVolume;
        this.MusicOn = Config.MusicOn;
        this.SoundOn = Config.SoundOn;
    }

    public void MusicMute(bool mute)
    {
        this.SetVolume("MusicVolume", mute ? 0 : musicVolume);    
    }    
    
    public void SoundMute(bool mute)
    {
        this.SetVolume("SoundVolume", mute ? 0 : musicVolume);    
    }

    private void SetVolume(string name, int value)
    {
        float volume = value * 0.5f - 50f;
        this.audioMixer.SetFloat(name, volume);
    }

    public void PlayMusic(string name)
    {
        AudioClip clip = Resloader.Load<AudioClip>(MusicPath + name);
        if (clip == null)
        {
            Debug.LogErrorFormat("PlayMusic:{0} is not existed", name);
            return;
        }
        if (musicAudioSource.isPlaying)
        {
            musicAudioSource.Stop();
        }
        musicAudioSource.clip = clip;
        musicAudioSource.Play();
        musicAudioSource.loop = true;
    }

    public void PlaySound(string name)
    {
        AudioClip clip = Resloader.Load<AudioClip>(SoundPath + name);
        if (clip == null)
        {
            Debug.LogErrorFormat("PlaySound:{0} is not existed", name);
            return;
        }
        soundAudioSource.PlayOneShot(clip);
    }

    protected void PlayClipOnAudioSource(AudioSource source, AudioClip clip, bool isLoop)
    { 
            
    }
}
