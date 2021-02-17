using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameUI : MonoBehaviour
{

    public GameObject gameLoseUI;
    public GameObject gameWinUI;
    public GameObject gameAlarmUI;
    public GameObject gameCautionUI;
    public GameObject gameCannotWinUI;

    void Start()
    {
        GameManager.ShowAlarmUI += showAlarmUI;
        GameManager.HideAlarmUI += hideAlarmUI;
        GameManager.ShowCautionUI += showCautionUI;
        GameManager.HideCautionUI += hideCautionUI;
        GameManager.ShowGameWinUI += showGameWinUI;
        GameManager.ShowGameLoseUI += showGameLoseUI;
        GameManager.ShowGameCannotWinUI += showGameCannotWinUI;
        GameManager.HideGameCannotWinUI += hideGameCannotWinUI;

    }


    void showGameWinUI() {
        gameWinUI.SetActive(true);
    }

    void showGameLoseUI() {
        gameLoseUI.SetActive(true);
    }
    
    void showAlarmUI() {
        gameAlarmUI.SetActive(true);
    }

    void hideAlarmUI() {
        gameAlarmUI.SetActive(false);
    }

    void showCautionUI() {
        gameCautionUI.SetActive(true);
    }

    void hideCautionUI() {
        gameCautionUI.SetActive(false);
    }

    void showGameCannotWinUI() {
        gameCannotWinUI.SetActive(true);
    }

    void hideGameCannotWinUI() {
        gameCannotWinUI.SetActive(false);
    }
}
