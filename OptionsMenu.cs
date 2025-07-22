using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class OptionsMenu : MonoBehaviour
{
    [Header("Referências do UI")]
    public Slider musicSlider;
    public Slider sfxSlider;
    public Button backButton;

    [Header("Audio Mixer")]
    public AudioMixer audioMixer;

    private const string MUSIC_PARAM    = "MusicVolume";
    private const string SFX_PARAM      = "SFXVolume";
    private const string PREF_MUSIC_KEY = "Pref_MusicVolume";
    private const string PREF_SFX_KEY   = "Pref_SFXVolume";

    void Start()
    {
        // 1) Inicializa sliders a partir de PlayerPrefs ou do mixer
        if (PlayerPrefs.HasKey(PREF_MUSIC_KEY))
        {
            float saved = PlayerPrefs.GetFloat(PREF_MUSIC_KEY);
            musicSlider.value = saved;
        }
        else if (audioMixer.GetFloat(MUSIC_PARAM, out float mVol))
        {
            musicSlider.value = Mathf.InverseLerp(-80f, 0f, mVol);
        }
        // Aplica valor inicial ao mixer
        SetMusicVolume(musicSlider.value);

        if (PlayerPrefs.HasKey(PREF_SFX_KEY))
        {
            float saved = PlayerPrefs.GetFloat(PREF_SFX_KEY);
            sfxSlider.value = saved;
        }
        else if (audioMixer.GetFloat(SFX_PARAM, out float sVol))
        {
            sfxSlider.value = Mathf.InverseLerp(-80f, 0f, sVol);
        }
        SetSFXVolume(sfxSlider.value);

        // 2) Setup de callbacks
        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        backButton.onClick.AddListener(() => SceneManager.LoadScene("Menu"));
    }

    /// <summary>
    /// Converte slider 0→1 para -80dB→0dB e grava preferência.
    /// </summary>
    public void SetMusicVolume(float sliderValue)
    {
        float dB = Mathf.Lerp(-80f, 0f, sliderValue);
        audioMixer.SetFloat(MUSIC_PARAM, dB);
        PlayerPrefs.SetFloat(PREF_MUSIC_KEY, sliderValue);
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float sliderValue)
    {
        float dB = Mathf.Lerp(-80f, 0f, sliderValue);
        audioMixer.SetFloat(SFX_PARAM, dB);
        PlayerPrefs.SetFloat(PREF_SFX_KEY, sliderValue);
        PlayerPrefs.Save();
    }
}
