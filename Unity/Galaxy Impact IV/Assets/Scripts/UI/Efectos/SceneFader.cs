using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneFader : MonoBehaviour
{
    public Image fadeImage;
    public float fadeDuration = 1.2f;
    public string nextScene = "LoginScene";
    public AudioSource audioSource; // sonido al pulsar

    private bool isTransitioning = false;

    private void Start()
    {
        // Fade-In al iniciar
        StartCoroutine(FadeIn());
    }

    private void Update()
    {
        if (isTransitioning) return;

        if (Input.anyKeyDown || Input.GetMouseButtonDown(0))
        {
            StartCoroutine(PlaySoundAndFadeOut());
        }
    }

    private IEnumerator FadeIn()
    {
        Color c = fadeImage.color;
        for (float t = fadeDuration; t >= 0; t -= Time.deltaTime)
        {
            c.a = t / fadeDuration;
            fadeImage.color = c;
            yield return null;
        }
        c.a = 0;
        fadeImage.color = c;
    }

    private IEnumerator PlaySoundAndFadeOut()
    {
        isTransitioning = true;

        if (audioSource != null)
            //audioSource.Play();

        yield return new WaitForSeconds(0.15f); // leve delay para escuchar el sonido

        Color c = fadeImage.color;
        for (float t = 0; t <= fadeDuration; t += Time.deltaTime)
        {
            c.a = t / fadeDuration;
            fadeImage.color = c;
            yield return null;
        }

        SceneManager.LoadScene(nextScene);
    }
}
