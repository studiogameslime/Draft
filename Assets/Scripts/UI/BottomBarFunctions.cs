using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class BottomBarFunctions : MonoBehaviour
{
    [Header("Drag here the 5 buttons")]
    public Button button1;
    public Button button2;
    public Button button3; // this one will change scenes
    public Button button4;
    public Button button5;
    public Button buttonPlay;


    [Header("Scene Navigation")]
    public string sceneNameForButton3; 

    void Start()
    {
        // ????? listener ?????? ???? 3
        buttonPlay.onClick.AddListener(() =>
        {
            SceneManager.LoadScene(sceneNameForButton3); 
        });
    }
}
