using UnityEngine;
using UnityEngine.UI;

public class VolumeController : MonoBehaviour
{
    public Slider volumeSlider;

    void Start()
    {
#if UNITY_EDITOR
        // En el Editor siempre arrancar al 100%
        float savedVolume = 1f;
#else
        // En build respetar PlayerPrefs (incluye 0)
        float savedVolume = PlayerPrefs.GetFloat("volume", 1f);
#endif

        savedVolume = Mathf.Clamp01(savedVolume);

        volumeSlider.value = savedVolume;
        AudioListener.volume = savedVolume;

        volumeSlider.onValueChanged.AddListener(ChangeVolume);
    }

    void ChangeVolume(float value)
    {
        value = Mathf.Clamp01(value);
        AudioListener.volume = value;

#if !UNITY_EDITOR
        // En build guardamos la preferencia del usuario
        PlayerPrefs.SetFloat("volume", value);
#endif
    }
}
