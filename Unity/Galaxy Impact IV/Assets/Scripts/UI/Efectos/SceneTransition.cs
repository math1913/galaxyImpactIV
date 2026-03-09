using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneTransition : MonoBehaviour
{
    public static SceneTransition Instance;

    [Header("Fade Settings")]
    public Image fadeImage;
    public float fadeDuration = 1f;

    private void Awake()
    {
        // Singleton
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        StartCoroutine(FadeIn());
    }

    // ---------- PUBLIC ----------
    public void FadeToScene(string sceneName)
    {
        StartCoroutine(FadeOutAndLoad(sceneName));
    }

    // ---------- FADE IN ----------
    private IEnumerator FadeIn()
    {
        Color c = fadeImage.color;
        c.a = 1;
        fadeImage.color = c;

        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            c.a = 1 - (t / fadeDuration);
            fadeImage.color = c;
            yield return null;
        }

        c.a = 0;
        fadeImage.color = c;
    }

    // ---------- FADE OUT ----------
    private IEnumerator FadeOutAndLoad(string sceneName)
    {
        Color c = fadeImage.color;

        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            c.a = t / fadeDuration;
            fadeImage.color = c;
            yield return null;
        }

        c.a = 1;
        fadeImage.color = c;

        SceneManager.LoadScene(sceneName);
    }
}
