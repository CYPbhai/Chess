using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingScreen : MonoBehaviour
{
    [SerializeField] private Image barFillImage;

    private void Start()
    {
        StartCoroutine(LoadSceneAsync("GameplayScene"));
    }

    IEnumerator LoadSceneAsync(string sceneName)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        while (!operation.isDone)
        {
            barFillImage.fillAmount = Mathf.Clamp01(operation.progress / 0.9f);
            yield return null;
        }
    }
}
