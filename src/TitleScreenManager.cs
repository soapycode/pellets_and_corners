using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using TMPro;

public class TitleScreenManager : MonoBehaviour
{
    private GameObject canvas;
    private GameObject settingsPanel;
    private GameObject settingsButton;

    private List<string> resolutionList;

    void Start()
    {
        canvas = GameObject.Find("Canvas");

        settingsButton = GameObject.Find("SettingsButtonText");

        settingsPanel = GameObject.Find("SettingsPanel");
        settingsPanel.SetActive(false);

        resolutionList = new List<string>();
        string currentResolutionString = Screen.width + "x" + Screen.height;

        foreach (var resolution in Screen.resolutions)
        {
            string tempString = resolution.width + "x" + resolution.height;
    
            if (!resolutionList.Contains(tempString))
                resolutionList.Add(tempString);
        }
        
        TMP_Dropdown resolutionDropdown = settingsPanel.GetComponentInChildren<TMP_Dropdown>();
        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(resolutionList);

        resolutionDropdown.onValueChanged.AddListener(ResolutionOptionClicked);

        int currentIndex = resolutionList.IndexOf(currentResolutionString);
        if (currentIndex != -1)
            resolutionDropdown.value = currentIndex;

        UnityEngine.UI.Toggle fullscreenToggle = settingsPanel.GetComponentInChildren<UnityEngine.UI.Toggle>();
        fullscreenToggle.isOn = Screen.fullScreen;
        fullscreenToggle.onValueChanged.AddListener(ToggleFullScreen);

        // NOTE: The resolution settings don't really work in WebGL, so I disable them
        if (Application.platform == RuntimePlatform.WebGLPlayer)
            settingsButton.SetActive(false);
    }

    public void ToggleSettingsPanel()
    {
        settingsPanel.SetActive(!settingsPanel.activeSelf);
    }

    public void ToggleFullScreen(bool newValue)
    {
        Screen.fullScreen = newValue;
    }

    public void ResolutionOptionClicked(int newValue)
    {
        string newResolutionString = resolutionList[newValue];
        string[] dimensionStrings = newResolutionString.Split('x');

        Debug.Assert(dimensionStrings.Length == 2);

        int newWidth = int.Parse(dimensionStrings[0]);
        int newHeight = int.Parse(dimensionStrings[1]);

        Screen.SetResolution(newWidth, newHeight, Screen.fullScreen);
    }

    public void PlayGame()
    {
        SceneManager.LoadScene("Game");
    }
}
