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

        SceneManager.sceneLoaded += OnSceneLoaded;

        // Play home music immediately — sceneLoaded already fired before we subscribed
        Debug.Log($"[SoundManager] Awake: homeMusic={homeMusic != null}, battleMusic={battleMusic != null}, musicSource={musicSource != null}");
        PlayMusic(homeMusic);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[SoundManager] OnSceneLoaded: {scene.name}, mode={mode}");
        if (scene.name == "HomeScreen")
            PlayMusic(homeMusic);
        else if (scene.name != "LoadingScreen")
            PlayMusic(battleMusic);
    }

    private void PlayMusic(AudioClip clip)
    {
        Debug.Log($"[SoundManager] PlayMusic: clip={clip?.name ?? "NULL"}, musicSource={musicSource != null}, isPlaying={musicSource?.isPlaying}");
        if (clip == null) return;
        if (musicSource.clip == clip && musicSource.isPlaying) return;

        musicSource.clip = clip;
        musicSource.Play();
        Debug.Log($"[SoundManager] Started playing: {clip.name}, isPlaying={musicSource.isPlaying}");
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
            sfxSource.PlayOneShot(clip);
    }

    public void PlayButtonClick() => PlaySFX(buttonClickSound);
    public void PlayPageSwipe() => PlaySFX(pageSwipeSound);
}
