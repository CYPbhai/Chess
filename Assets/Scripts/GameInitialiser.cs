using UnityEngine;

public class GameInitialiser : MonoBehaviour
{
    [SerializeField] GameManager gameManager;
    [SerializeField] AudioManager audioManager;
    private void Start()
    {
        if(GameManager.Instance==null)
            Instantiate(gameManager);
        if(AudioManager.Instance==null)
            Instantiate(audioManager);
    }
}
