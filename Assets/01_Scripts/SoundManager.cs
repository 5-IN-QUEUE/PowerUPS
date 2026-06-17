using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Audio;

public class SoundManager : MonoBehaviour
{
	public static SoundManager instance;

	[Header("Audio Mixer")]
	public AudioMixer audioMixer;
	
	public Slider masterSlider;
	public Slider bgmSlider;
	public Slider sfxSlider;
	
	public TextMeshProUGUI masterVolText;
	public TextMeshProUGUI bgmVolText;
	public TextMeshProUGUI sfxVolText;

	[Header("SFX Random Pitch")]
	public float sfxMinPitch = 0.9f;
	public float sfxMaxPitch = 1.1f;

	[Header("SFX Pool")]
	[SerializeField] private int sfxPoolSize = 16;

	[System.Serializable]
	public struct ClipEntry
	{
		public string clipName;
		public AudioClip clip;
	}

	[Header("SFX Clips")]
	[SerializeField] private List<ClipEntry> sfxEntries = new();

	[Header("BGM Clips")]
	[SerializeField] private List<ClipEntry> bgmEntries = new();

	public Dictionary<string, AudioClip> sfxDict = new();
	public Dictionary<string, AudioClip> bgmDict = new();

	private AudioSource[] bgmSources;
	private int currentBGMIndex = 0;
	private Coroutine crossfadeCoroutine;
	private AudioSource[] sfxPool;
	private int sfxPoolCursor = 0;

	private const string MASTER_VOL = "MasterVolume";
	private const string BGM_VOL = "BGMVolume";
	private const string SFX_VOL = "SFXVolume";

	private void Awake()
	{
		if (instance == null)
		{
			instance = this;
			DontDestroyOnLoad(gameObject);
			InitializePools();
			InitializeDictionaries();
		}
		else
		{
			Destroy(gameObject);
		}
	}
	
	private void Start()
	{
		SetupSlidersAndTexts();

		// ★ 리스너 등록 완료 후 저장값 적용
		// SaveManager가 이미 LoadSettings()를 호출했더라도 슬라이더가 없었으므로
		// 여기서 직접 슬라이더 UI + AudioMixer를 동기화
		if (SaveManager.instance != null)
		{
			GameSettings s = SaveManager.instance.settings;

			// AudioMixer 적용
			SetMasterVolume(s.masterVolume);
			SetBGMVolume(s.bgmVolume);
			SetSFXVolume(s.sfxVolume);

			// 슬라이더 UI 복원 (리스너 중복 발화 방지)
			if (masterSlider != null)
			{
				masterSlider.SetValueWithoutNotify(s.masterVolume);
				if (masterVolText != null) masterVolText.text = Mathf.RoundToInt(s.masterVolume).ToString();
			}
			if (bgmSlider != null)
			{
				bgmSlider.SetValueWithoutNotify(s.bgmVolume);
				if (bgmVolText != null) bgmVolText.text = Mathf.RoundToInt(s.bgmVolume).ToString();
			}
			if (sfxSlider != null)
			{
				sfxSlider.SetValueWithoutNotify(s.sfxVolume);
				if (sfxVolText != null) sfxVolText.text = Mathf.RoundToInt(s.sfxVolume).ToString();
			}
		}
	}

	private void SetupSlidersAndTexts()
	{
		if (masterSlider != null)
		{
			masterSlider.onValueChanged.AddListener((value) => {
				SetMasterVolume(value);
				if (masterVolText != null) 
					masterVolText.text = Mathf.RoundToInt(value).ToString();
         
				if (SaveManager.instance != null) SaveManager.instance.OnMasterVolumeChanged(value);
			});
		}

		if (bgmSlider != null)
		{
			bgmSlider.onValueChanged.AddListener((value) => {
				SetBGMVolume(value);
				if (bgmVolText != null) 
					bgmVolText.text = Mathf.RoundToInt(value).ToString();
         
				if (SaveManager.instance != null) SaveManager.instance.OnBGMVolumeChanged(value);
			});
		}

		if (sfxSlider != null)
		{
			sfxSlider.onValueChanged.AddListener((value) => {
				SetSFXVolume(value);
				if (sfxVolText != null) 
					sfxVolText.text = Mathf.RoundToInt(value).ToString();
         
				if (SaveManager.instance != null) SaveManager.instance.OnSFXVolumeChanged(value);
			});
		}
	}

	private void OnEnable()
	{
		PlayBGM("BGM1");
	}

	private void InitializeDictionaries()
	{
		foreach (ClipEntry entry in sfxEntries)
		{
			if (entry.clip == null) continue;
			if (!sfxDict.TryAdd(entry.clipName, entry.clip))
				Debug.LogWarning($"[SoundManager] SFX : {entry.clipName}");
		}

		foreach (ClipEntry entry in bgmEntries)
		{
			if (entry.clip == null) continue;
			if (!bgmDict.TryAdd(entry.clipName, entry.clip))
				Debug.LogWarning($"[SoundManager] BGM : {entry.clipName}");
		}
	}

	private void InitializePools()
	{
		bgmSources = new AudioSource[2];
		for (int i = 0; i < 2; i++)
		{
			bgmSources[i] = gameObject.AddComponent<AudioSource>();
			bgmSources[i].outputAudioMixerGroup = audioMixer.FindMatchingGroups("BGM")[0];
			bgmSources[i].loop = true;
			bgmSources[i].playOnAwake = false;
		}

		sfxPool = new AudioSource[sfxPoolSize];
		AudioMixerGroup sfxGroup = audioMixer.FindMatchingGroups("SFX")[0];
		for (int i = 0; i < sfxPoolSize; i++)
		{
			sfxPool[i] = gameObject.AddComponent<AudioSource>();
			sfxPool[i].outputAudioMixerGroup = sfxGroup;
			sfxPool[i].playOnAwake = false;
		}
	}
	
	public void PlaySFX(string clipName)
	{
		if (!sfxDict.TryGetValue(clipName, out AudioClip clip))
		{
			Debug.LogWarning($"[SoundManager] SFX : {clipName}");
			return;
		}
		AudioSource src = GetPooledSFXSource();
		src.clip = clip;
		src.pitch = Random.Range(sfxMinPitch, sfxMaxPitch);
		src.Play();
	}

	public AudioSource GetPooledSFXSource()
	{
		for (int i = 0; i < sfxPoolSize; i++)
		{
			int idx = (sfxPoolCursor + i) % sfxPoolSize;
			if (!sfxPool[idx].isPlaying)
			{
				sfxPoolCursor = (idx + 1) % sfxPoolSize;
				return sfxPool[idx];
			}
		}
		AudioSource overwrite = sfxPool[sfxPoolCursor];
		sfxPoolCursor = (sfxPoolCursor + 1) % sfxPoolSize;
		return overwrite;
	}

	public void PlayBGM(string clipName, float fadeDuration = 1f)
	{
		if (!bgmDict.TryGetValue(clipName, out AudioClip clip))
		{
			Debug.LogWarning($"[SoundManager] BGM Ŭ���� ã�� �� ����: {clipName}");
			return;
		}
		if (crossfadeCoroutine != null) StopCoroutine(crossfadeCoroutine);
		crossfadeCoroutine = StartCoroutine(CrossfadeBGM(clip, fadeDuration));
	}

	public void StopBGM(float fadeDuration = 1f)
	{
		if (crossfadeCoroutine != null) StopCoroutine(crossfadeCoroutine);
		crossfadeCoroutine = StartCoroutine(FadeOutBGM(fadeDuration));
	}

	private IEnumerator CrossfadeBGM(AudioClip newClip, float duration)
	{
		int next = 1 - currentBGMIndex;

		bgmSources[next].clip = newClip;
		bgmSources[next].volume = 0f;
		bgmSources[next].pitch = 1f;
		bgmSources[next].Play();

		float elapsed = 0f;
		float startVol = bgmSources[currentBGMIndex].volume;

		while (elapsed < duration)
		{
			elapsed += Time.deltaTime;
			float t = Mathf.Clamp01(elapsed / Mathf.Max(duration, 0.01f));
			bgmSources[currentBGMIndex].volume = Mathf.Lerp(startVol, 0f, t);
			bgmSources[next].volume = Mathf.Lerp(0f, 1f, t);
			yield return null;
		}

		bgmSources[currentBGMIndex].Stop();
		bgmSources[currentBGMIndex].volume = 1f;
		currentBGMIndex = next;
	}

	private IEnumerator FadeOutBGM(float duration)
	{
		float startVol = bgmSources[currentBGMIndex].volume;
		float elapsed = 0f;

		while (elapsed < duration)
		{
			elapsed += Time.deltaTime;
			bgmSources[currentBGMIndex].volume =
				Mathf.Lerp(startVol, 0f, elapsed / duration);
			yield return null;
		}
		bgmSources[currentBGMIndex].Stop();
		bgmSources[currentBGMIndex].volume = 1f;
	}
    
	public void SetMasterVolume(float value) => SetMixerVolume(MASTER_VOL, value);
	public void SetBGMVolume(float value) => SetMixerVolume(BGM_VOL, value);
	public void SetSFXVolume(float value) => SetMixerVolume(SFX_VOL, value);

	private void SetMixerVolume(string paramName, float sliderValue)
	{
		if (Mathf.Approximately(sliderValue, 0f))
		{
			audioMixer.SetFloat(paramName, -80f);
		}
		else
		{
			float volumeRatio = sliderValue / 100f;
			float db = Mathf.Log10(volumeRatio) * 20f;
			audioMixer.SetFloat(paramName, db);
		}
	}
	
	public float GetMasterVolume() => GetMixerVolume(MASTER_VOL);
	public float GetBGMVolume() => GetMixerVolume(BGM_VOL);
	public float GetSFXVolume() => GetMixerVolume(SFX_VOL);

	private float GetMixerVolume(string paramName)
	{
		if (audioMixer.GetFloat(paramName, out float db))
		{
			if (db <= -80f) return 0f;
			return Mathf.Pow(10f, db / 20f) * 100f;
		}
		return 100f;
	}
	
	public void BindUIElements(Slider master, Slider bgm, Slider sfx, TextMeshProUGUI masterText, TextMeshProUGUI bgmText, TextMeshProUGUI sfxText)
	{
		if (master != null) master.onValueChanged.RemoveAllListeners();
		if (bgm != null) bgm.onValueChanged.RemoveAllListeners();
		if (sfx != null) sfx.onValueChanged.RemoveAllListeners();

		masterSlider = master;
		bgmSlider = bgm;
		sfxSlider = sfx;

		masterVolText = masterText;
		bgmVolText = bgmText;
		sfxVolText = sfxText;

		SetupSlidersAndTexts();

		if (SaveManager.instance != null)
		{
			GameSettings s = SaveManager.instance.settings;

			// MASTER
			if (masterSlider != null)
			{
				masterSlider.SetValueWithoutNotify(s.masterVolume);
				if (masterVolText != null) 
					masterVolText.text = Mathf.RoundToInt(s.masterVolume).ToString();
			}

			// BGM
			if (bgmSlider != null)
			{
				bgmSlider.SetValueWithoutNotify(s.bgmVolume);
				if (bgmVolText != null) 
					bgmVolText.text = Mathf.RoundToInt(s.bgmVolume).ToString();
			}

			// SFX
			if (sfxSlider != null)
			{
				sfxSlider.SetValueWithoutNotify(s.sfxVolume);
				if (sfxVolText != null) 
					sfxVolText.text = Mathf.RoundToInt(s.sfxVolume).ToString();
			}
		}
	}
}