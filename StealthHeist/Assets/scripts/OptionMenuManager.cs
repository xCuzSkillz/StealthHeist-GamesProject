using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;

public class OptionMenuManager : MonoBehaviour
{
    public Slider masterVol, musicVol, sfxVol;
    public AudioMixer mainAudioMixer;

    public void ChangeMasterVolume()
    {
        mainAudioMixer.SetFloat("MasterVol", masterVol.value);
    }

    public void ChangeMusicVolume()
    {
        mainAudioMixer.SetFloat("MusicVol", musicVol.value);
    }

    public void ChangeSfxVolume()
    {
        mainAudioMixer.SetFloat("SfxVol", sfxVol.value);
    }
}
