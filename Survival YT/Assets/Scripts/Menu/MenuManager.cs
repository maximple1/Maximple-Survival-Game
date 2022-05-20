using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    [SerializeField] private CinemachineVirtualCamera _mainCamera;
    [SerializeField] private CinemachineVirtualCamera _settingsCamera;
    [SerializeField] private GameObject _menuPanel;
    [SerializeField] private GameObject _settingsPanel;
    public float _currentSensitivity;
    private int _currentResolutionIndex;
    
    
    #region SettingsVars
    [Header("Settings Menu")]
    [SerializeField] private AudioMixer _audioMixer;
    [SerializeField] private TMP_Dropdown _resolutionDropdown;
    [SerializeField] private TMP_Dropdown _qualityDropdown;
    [SerializeField] private Toggle _toggle;
    
    private Resolution[] _resolutions;
    [SerializeField] private Transform _moveableNailTransform;
    
     #endregion
     // max x value -1.841574
     // min x value -0.136
     private void Start()
     {
         PopulateResolutionDropdown();
         LoadSettings(_currentResolutionIndex);
     }

     #region Settings Funcs
        private void PopulateResolutionDropdown()
        {
            _resolutionDropdown.ClearOptions();
            List<string> options = new List<string>();
            _resolutions = Screen.resolutions;
            _currentResolutionIndex = 0;

            for (int i = 0; i < _resolutions.Length; i++)
            {
                string option = _resolutions[i].width + "x" + _resolutions[i].height;
                options.Add(option);
                if (_resolutions[i].width == Screen.currentResolution.width &&
                    _resolutions[i].height == Screen.currentResolution.height)
                    _currentResolutionIndex = i;
            }
            _resolutionDropdown.AddOptions(options);
            _resolutionDropdown.RefreshShownValue();
        }
        public void SetFullscreen(bool isFullscreen)
        {
            Screen.fullScreen = isFullscreen;
        }
        public void SetResolution(int resolutionIndex)
        {
            Resolution resolution = _resolutions[resolutionIndex];
            Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
        }
        public void SetQuality(int qualityIndex)
        {
            QualitySettings.SetQualityLevel(qualityIndex);
        }
        public void SetVolume()
        {
            float desiredVolume = Mathf.InverseLerp(-0.136f, -1.841574f, _moveableNailTransform.localPosition.x);
            if (desiredVolume <= 0.0001f)
                desiredVolume = 0.0001f;
            
            _audioMixer.SetFloat("MasterVolume",Mathf.Log10(desiredVolume) * 20);
        }
        public void SetSensitivity(float desiredSensitivity)
        {
            PlayerPrefs.SetFloat("Sensitivity",desiredSensitivity);
            _currentSensitivity = desiredSensitivity;
        }
        public void SaveSettings()
        {
            PlayerPrefs.SetInt("QualitySettings", _qualityDropdown.value);
            PlayerPrefs.SetInt("Resolution", _resolutionDropdown.value);
            PlayerPrefs.SetInt("Fullscreen", System.Convert.ToInt32(Screen.fullScreen));
            
            float volume; 
            _audioMixer.GetFloat("MasterVolume", out volume);
            PlayerPrefs.SetFloat("Volume", volume);
        }
        public void LoadSettings(int currentResolutionIndex)
        {
            if (PlayerPrefs.HasKey("QualitySettings"))
                _qualityDropdown.value = PlayerPrefs.GetInt("QualitySettings");
            else
                _qualityDropdown.value = 3;
            
            SetQuality(_qualityDropdown.value);
            
            if (PlayerPrefs.HasKey("Resolution"))
                _resolutionDropdown.value = PlayerPrefs.GetInt("Resolution");
            else
                _resolutionDropdown.value = currentResolutionIndex;
            
            SetResolution(_resolutionDropdown.value);
            
            if (PlayerPrefs.HasKey("Fullscreen"))
                Screen.fullScreen = System.Convert.ToBoolean(PlayerPrefs.GetInt("Fullscreen"));
            else
                Screen.fullScreen = true;
            
            _toggle.isOn = Screen.fullScreen;

            if (PlayerPrefs.HasKey("Volume"))
            {
                _audioMixer.SetFloat("MasterVolume", PlayerPrefs.GetFloat("Volume"));
                _moveableNailTransform.localPosition =
                    new Vector3(Mathf.Lerp(-0.136f, -1.841574f, Mathf.Pow(10,PlayerPrefs.GetFloat("Volume")/20)), 0, 0);
            }
            else
            {
                _audioMixer.SetFloat("MasterVolume", 0);
                _moveableNailTransform.localPosition =
                    new Vector3(Mathf.Lerp(-0.136f, -1.841574f, 1), 0, 0);
            }
        }
        #endregion

        public void Play()
        {
            SceneManager.LoadScene(1);
        }

        public void Quit()
        {
            Application.Quit();
            Debug.Log("Quit game");
        }

        public void Settings()
        {
            _mainCamera.Priority = 0;
            _menuPanel.SetActive(false);
            _settingsCamera.Priority = 1;

            StartCoroutine(OpenSettingsAfterTime());
        }
        public void Back()
        {
            _settingsCamera.Priority = 0;
            _mainCamera.Priority = 1;
            _settingsPanel.SetActive(false);

            StartCoroutine(OpenMainMenuAfterTime());
        }

        IEnumerator OpenSettingsAfterTime()
        {
            yield return new WaitForSeconds(2);
            _settingsPanel.SetActive(true);
        }
        IEnumerator OpenMainMenuAfterTime()
        {
            yield return new WaitForSeconds(2);
            _menuPanel.SetActive(true);
        }
}
