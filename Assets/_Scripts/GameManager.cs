using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class GameManagerStatic{
    public static GameManager gameManager;
}

public class GameManager: MonoBehaviour {

    bool alarm;
    bool searchMode;
    bool cautionMode;
    bool gameIsOver;

    public static event System.Action ShowAlarmUI;
    public static event System.Action HideAlarmUI;
    public static event System.Action ShowCautionUI;
    public static event System.Action HideCautionUI;
    public static event System.Action ShowGameWinUI;
    public static event System.Action ShowGameLoseUI;
    public static event System.Action informGuardAlarmIsSetOff;
    public static event System.Action ShowGameCannotWinUI;
    public static event System.Action HideGameCannotWinUI;

    public float alarmPeriod;
    public float cautionPeriod;
    float alarmTime = 0f;
    float cautionTime = 0f;

    public Text alarmText;
    public Text cautionText;

    Transform player;
    Vector3 reportedPlayerLocation;

    public float viewDstPenalty;
    public float viewAnglePenalty;
    public float guardSpeedPenalty;

    void Start() {
        GameManagerStatic.gameManager = this;
        alarm = false;
        searchMode = false;
        cautionMode = false;
        gameIsOver = false;

        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Update() {
        if (searchMode) {
            alarmTime += Time.deltaTime;
            alarmText.text = "Alarm " + (99.99f - (Mathf.Clamp(alarmTime, 0, alarmPeriod) / alarmPeriod) * 99.99f).ToString("F2");
            if (alarmTime >= alarmPeriod)
                startCaution();
        } else if (cautionMode) {
            cautionTime += Time.deltaTime;
            cautionText.text = "Caution " + (99.99f - (Mathf.Clamp(cautionTime, 0, cautionPeriod) / cautionPeriod) * 99.99f).ToString("F2");
            if (cautionTime >= cautionPeriod) {
                alarm = false;
                searchMode = false;
                cautionMode = false;
                GameManager.HideCautionUI();
            }
        }
    }

    public void setOffAlarm() {
        alarm = true;
        searchMode = false;
        cautionMode = false;
        alarmTime = 0f;
        alarmText.text = "Alarm";
        GameManager.ShowAlarmUI();
        GameManager.HideCautionUI();
        GameManager.informGuardAlarmIsSetOff();
    }

    void startCaution() {
        GameManager.HideAlarmUI();
        GameManager.ShowCautionUI();
        alarm = false;
        searchMode = false;
        cautionMode = true;
        cautionTime = 0f;
    }

    public void gameWin() {
        gameIsOver = true;
        GameManager.ShowGameWinUI();
    }

    public void gameLose() {
        gameIsOver = true;
        GameManager.ShowGameLoseUI();
    }

    public void reportMissingPlayer() {
        searchMode = true;
    }
    
    public void reportPlayerPosition() {
        reportedPlayerLocation = player.position;
    }

    public Vector3 getPlayerLocation() {
        return reportedPlayerLocation;
    }
    
    public bool isAlarmSetOff() {
        return alarm;
    }

    public bool isSearchOn() {
        return searchMode;
    }

    public bool isCautionOn() {
        return cautionMode;
    }

    public float getViewDstPenalty() {
        return viewDstPenalty;
    }

    public float getViewAnglePenalty() {
        return viewAnglePenalty;
    }

    public float getGuardSpeedPenalty() {
        return guardSpeedPenalty;
    }

    public bool isGameOver() {
        return gameIsOver;
    }

    public void gameCannotWinShow() {
        GameManager.ShowGameCannotWinUI();
    }

    public void gameCannotWinHide() {
        GameManager.HideGameCannotWinUI();
    }
}
