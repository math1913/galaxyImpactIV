using UnityEngine;

public class SceneChangeButton : MonoBehaviour
{
    public string sceneName;

    public void ChangeScene()
    {
        SceneTransition.Instance.FadeToScene(sceneName);
    }
}
