using UnityEngine;
using TMPro;

public class BlinkingText : MonoBehaviour
{
    public TMP_Text text;
    public float fadeSpeed = 2f;

    private Color originalColor;

    private void Start()
    {
        if (text == null) text = GetComponent<TMP_Text>();
        originalColor = text.color;
    }

    private void Update()
    {
        float alpha = (Mathf.Sin(Time.time * fadeSpeed) + 1f) / 2f;
        text.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
    }
}
