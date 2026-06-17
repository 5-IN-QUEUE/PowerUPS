using UnityEngine;
using System.IO;

[System.Serializable]
public class GameSettings
{
    public float masterVolume         = 100f;
    public float bgmVolume            = 100f;
    public float sfxVolume            = 100f;

    // public int currentResolutionIndex  = 2;
    // public int currentScreenModeIndex  = 0;
    // public int currentLanguageIndex    = 0;
    //
    // public int currentStage            = 1;
    // public bool[] stageClearedStatuses = new bool[0];
}

public class SaveManager : MonoBehaviour
{
    public static SaveManager instance;

    public GameSettings settings = new GameSettings();

    private string savePath;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        savePath = Path.Combine(Application.persistentDataPath, "SaveFile.json");

        // 어느 씬에서 시작하든 저장 파일은 즉시 읽어둔다 (적용은 각 매니저가 알아서)
        LoadSettingsFromDisk();
    }

    public void SaveSettings()
    {
        string json = JsonUtility.ToJson(settings, true);
        File.WriteAllText(savePath, json);
        Debug.Log($"[SaveManager] 설정 저장 완료: {savePath}");
    }

    public void LoadSettings()
    {
        LoadSettingsFromDisk();
        ApplySettings();
    }

    private void LoadSettingsFromDisk()
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            settings = JsonUtility.FromJson<GameSettings>(json);
            Debug.Log("[SaveManager] 설정 불러오기 완료");
        }
        else
        {
            Debug.Log("[SaveManager] 저장 파일 없음 → 기본값 사용");
        }
    }

    public void ApplySettings()
    {
        ApplySoundSettings();
    }

    public bool HasSaveFile() => File.Exists(savePath);

    private void ApplySoundSettings()
    {
        if (SoundManager.instance == null) return;

        SoundManager.instance.SetMasterVolume(settings.masterVolume);
        SoundManager.instance.SetBGMVolume(settings.bgmVolume);
        SoundManager.instance.SetSFXVolume(settings.sfxVolume);

        if (SoundManager.instance.masterSlider != null)
        {
            SoundManager.instance.masterSlider.SetValueWithoutNotify(settings.masterVolume);
            if (SoundManager.instance.masterVolText != null)
                SoundManager.instance.masterVolText.text = Mathf.RoundToInt(settings.masterVolume).ToString();
        }

        if (SoundManager.instance.bgmSlider != null)
        {
            SoundManager.instance.bgmSlider.SetValueWithoutNotify(settings.bgmVolume);
            if (SoundManager.instance.bgmVolText != null)
                SoundManager.instance.bgmVolText.text = Mathf.RoundToInt(settings.bgmVolume).ToString();
        }

        if (SoundManager.instance.sfxSlider != null)
        {
            SoundManager.instance.sfxSlider.SetValueWithoutNotify(settings.sfxVolume);
            if (SoundManager.instance.sfxVolText != null)
                SoundManager.instance.sfxVolText.text = Mathf.RoundToInt(settings.sfxVolume).ToString();
        }
    }

    public void DeleteSaveFile()
    {
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
            settings = new GameSettings();
            Debug.Log("[SaveManager] 세이브 파일 삭제 완료");
        }
        else
        {
            Debug.Log("[SaveManager] 삭제할 세이브 파일 없음");
        }
    }
    
    public void OnMasterVolumeChanged(float v)  { settings.masterVolume            = v;     SaveSettings(); }
    public void OnBGMVolumeChanged(float v)     { settings.bgmVolume               = v;     SaveSettings(); }
    public void OnSFXVolumeChanged(float v)     { settings.sfxVolume               = v;     SaveSettings(); }
}
