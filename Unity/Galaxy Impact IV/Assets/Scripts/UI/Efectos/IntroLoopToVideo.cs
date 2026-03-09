using UnityEngine;
using UnityEngine.Video;
using System.Collections;

public class IntroLoopToVideo : MonoBehaviour
{
    public VideoPlayer introPlayer;
    public VideoPlayer loopPlayer;
    public UIFader uiFader;
    public CanvasGroup blackScreen; // Panel negro fullscreen

    void Awake()
    {
        // INTRO
        introPlayer.source = VideoSource.Url;
        introPlayer.url = System.IO.Path.Combine(
            Application.streamingAssetsPath,
            "intro.mp4"
        );

        introPlayer.playOnAwake = false;
        introPlayer.isLooping = false;

        // LOOP
        loopPlayer.source = VideoSource.Url;
        loopPlayer.url = System.IO.Path.Combine(
            Application.streamingAssetsPath,
            "loop.mp4"
        );

        loopPlayer.playOnAwake = false;
        loopPlayer.isLooping = true;
    }

    void Start()
    {
        // Negro al inicio para evitar flashes
        if (blackScreen != null)
            blackScreen.alpha = 1f;

        introPlayer.loopPointReached += OnIntroEnd;

        // Preparamos ambos
        introPlayer.Prepare();
        loopPlayer.Prepare();

        StartCoroutine(PlayIntroWhenReady());
    }

    IEnumerator PlayIntroWhenReady()
    {
        while (!introPlayer.isPrepared)
            yield return null;

        // Quitamos negro cuando el video está listo
        if (blackScreen != null)
            blackScreen.alpha = 0f;

        introPlayer.Play();
    }

    void OnIntroEnd(VideoPlayer vp)
    {
        StartCoroutine(SwapToLoop());
    }

    IEnumerator SwapToLoop()
    {
        while (!loopPlayer.isPrepared)
            yield return null;

        // Para Camera Near Plane: NO desactivar GameObject
        introPlayer.enabled = false;
        loopPlayer.enabled = true;

        loopPlayer.Play();
        uiFader.FadeIn();
    }
}
