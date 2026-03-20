using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Menu : MonoBehaviour
{
    public GameObject gamePlay;

    public void PlayGame()
    {
        gameObject.SetActive(false);
        gamePlay.SetActive(true);
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
