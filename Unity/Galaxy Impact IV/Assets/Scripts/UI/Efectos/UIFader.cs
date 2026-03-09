using UnityEngine;

public class UIFader : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public float fadeDuration = 3f;

    private void Start()
    {
        canvasGroup.alpha = 0f; // Empezamos invisible
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    public void FadeIn()
    {
        StartCoroutine(FadeCanvasIn());
    }

    private System.Collections.IEnumerator FadeCanvasIn()
    {
        float t = 0;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0, 1, t / fadeDuration);
            yield return null;
        }
                    
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }
}
