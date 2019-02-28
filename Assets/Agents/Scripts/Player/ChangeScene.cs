using UnityEngine;
using UnityEngine.SceneManagement;

public class ChangeScene : MonoBehaviour
{
    [SerializeField] private string sceneName;

    public void Change()
    {
        SceneManager.LoadSceneAsync(sceneName);
    }

    private void Awake()
    {
        Application.targetFrameRate = 300;
    }

    void Update()
    {
        if (Input.GetKey("escape"))
        {
            Application.Quit();
        }
    }
}