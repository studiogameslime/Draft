using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    private AudioSource sfxSource;
    private AudioSource musicSource;

    [Header("UI Sounds")]
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private AudioClip pageSwipeSound;

    [Header("Music")]
    [SerializeField] private AudioClip homeMusic;
    [SerializeField] private AudioClip battleMusic;
    [SerializeField] private float musicVolume = 0.5f;

    public bool SoundEnabled { get; private set; } = true;
    public bool MusicEnabled { get; private set; } = true;
    public bool VibrationEnabled { get; private set; } = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
        sfxSource = GetComponent<AudioSource>();

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.volume = musicVolume;

        LoadSettings();

        SceneManager.sceneLoaded += OnSceneLoaded;

        PlayMusic(homeMusic);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void LoadSettings()
    {
        if (GameData.Instance != null && GameData.Instance.Save != null)
        {
            SoundEnabled = GameData.Instance.Save.soundEnabled;
            MusicEnabled = GameData.Instance.Save.musicEnabled;
            VibrationEnabled = GameData.Instance.Save.vibrationEnabled;
        }

        ApplySoundSetting();
        ApplyMusicSetting();
    }

    public void SetSoundEnabled(bool enabled)
    {
        SoundEnabled = enabled;
        ApplySoundSetting();
        SaveSettings();
    }

    public void SetMusicEnabled(bool enabled)
    {
        MusicEnabled = enabled;
        ApplyMusicSetting();
        SaveSettings();
    }

    public void SetVibrationEnabled(bool enabled)
    {
        VibrationEnabled = enabled;
        SaveSettings();
    }

    private void ApplySoundSetting()
    {
        if (sfxSource != null)
            sfxSource.mute = !SoundEnabled;
    }

    private void ApplyMusicSetting()
    {
        if (musicSource != null)
        {
            musicSource.mute = !MusicEnabled;
        }
    }

    private void SaveSettings()
    {
        if (GameData.Instance == null || GameData.Instance.Save == null) return;
        GameData.Instance.Save.soundEnabled = SoundEnabled;
        GameData.Instance.Save.musicEnabled = MusicEnabled;
        GameData.Instance.Save.vibrationEnabled = VibrationEnabled;
        GameData.Instance.SaveNow();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Reload settings in case save data wasn't ready on first Awake
        if (!settingsLoaded && GameData.Instance != null && GameData.Instance.Save != null)
        {
            LoadSettings();
            settingsLoaded = true;
        }

        if (scene.name == "HomeScreen")
            PlayMusic(homeMusic);
        else if (scene.name != "LoadingScreen")
            PlayMusic(battleMusic);
    }
    private bool settingsLoaded;

    private void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;
        if (musicSource.clip == clip && musicSource.isPlaying) return;

        musicSource.clip = clip;
        musicSource.Play();
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
            sfxSource.PlayOneShot(clip);
    }

    public void PlayButtonClick() => PlaySFX(buttonClickSound);
    public void PlayPageSwipe() => PlaySFX(pageSwipeSound);

    public static void Vibrate()
    {
        if (Instance != null && !Instance.VibrationEnabled) return;
#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#endif
    }
}
