using UnityEngine;

public class SceneFadeIn : MonoBehaviour
{
    public CanvasGroup blackScreen;
    public float fadeDuration = 1f;

    void Start()
    {
        StartCoroutine(FadeIn());
    }

    private System.Collections.IEnumerator FadeIn()
    {
        float t = 0f;
        blackScreen.alpha = 1f; // comenzamos en negro

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            blackScreen.alpha = 1f - (t / fadeDuration);
            yield return null;
        }

        blackScreen.alpha = 0f;
        blackScreen.gameObject.SetActive(false); // lo apagamos si ya no se necesita
    }
}
