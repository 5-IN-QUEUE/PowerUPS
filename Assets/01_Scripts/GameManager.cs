using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    
    void Awake()
    {
        if (instance == null) instance = this;
        else { Destroy(gameObject);}
        
    }

    void Start()
    {
        
    }
    
    public void StartButtonOnClick()
    {
        SoundManager.instance.PlaySFX("UIClick");
        SceneManager.LoadScene("InGameScene");
    }

    public void QuitButtonOnClick()
    {
        SoundManager.instance.PlaySFX("UIClick");
        Application.Quit();
    }
}
