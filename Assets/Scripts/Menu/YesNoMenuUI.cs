using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class YesNoMenuUI : MenuUI
{
    public static YesNoMenuUI Instance { get; private set; }
    [SerializeField] Button yesButton;
    [SerializeField] Button noButton;

    private void Awake()
    {
        if(Instance)
        {
            Debug.LogWarning("YesNoMenuUI already exists");
            Destroy(gameObject);
            return;
        }
        Instance = this;

        yesButton.onClick.AddListener(() =>
        {
            AudioManager.Instance.PlayClickSound();
            if (previousMenu == MainMenuUI.Instance)
            {
                Application.Quit();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#endif
            }
            if(previousMenu == PauseMenuUI.Instance || previousMenu == VictoryScreenMenuUI.Instance)
            {
                Time.timeScale = 1.0f;
                SceneManager.LoadScene("MainMenuScene");
            }
        });

        noButton.onClick.AddListener(() =>
        {
            AudioManager.Instance.PlayClickSound();
            Hide(true);
        });
    }

    private void Start()
    {
        Hide(false);
    }
}
