using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class BackgroundMusicPlayer : MonoBehaviour
{
    private const string BackgroundMusicResourcePath = "Audio/Music/Where_The_Wind_Rests";

    public static BackgroundMusicPlayer Instance { get; private set; }

    [SerializeField, Range(0f, 1f)] private float volume = 0.22f;

    private AudioSource _audioSource;
    private AudioClip _backgroundClip;
    private Coroutine _playRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _audioSource = gameObject.GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }

        _audioSource.playOnAwake = false;
        _audioSource.loop = true;
        _audioSource.spatialBlend = 0f;
        _audioSource.priority = 96;
        _audioSource.volume = volume;
        _audioSource.mute = false;
        _audioSource.bypassEffects = true;
        _audioSource.bypassListenerEffects = true;
        _audioSource.bypassReverbZones = true;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnValidate()
    {
        if (_audioSource != null)
        {
            _audioSource.volume = volume;
        }
    }

    public void RefreshForActiveScene()
    {
        RefreshForScene(SceneManager.GetActiveScene());
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshForScene(scene);
    }

    private void RefreshForScene(Scene scene)
    {
        if (!IsMainScene(scene))
        {
            if (_playRoutine != null)
            {
                StopCoroutine(_playRoutine);
                _playRoutine = null;
            }

            if (_audioSource != null && _audioSource.isPlaying)
            {
                _audioSource.Stop();
            }

            return;
        }

        EnsureClipLoaded();
        if (_backgroundClip == null || _audioSource == null)
        {
            return;
        }

        _audioSource.volume = volume;
        _audioSource.spatialBlend = 0f;
        _audioSource.loop = true;
        _audioSource.clip = _backgroundClip;

        if (_playRoutine != null)
        {
            StopCoroutine(_playRoutine);
        }

        _playRoutine = StartCoroutine(PlayWhenReady());
    }

    private void EnsureClipLoaded()
    {
        if (_backgroundClip != null)
        {
            return;
        }

        _backgroundClip = Resources.Load<AudioClip>(BackgroundMusicResourcePath);
        if (_backgroundClip == null)
        {
            Debug.LogWarning($"[BackgroundMusicPlayer] Missing clip: {BackgroundMusicResourcePath}");
        }
    }

    private System.Collections.IEnumerator PlayWhenReady()
    {
        if (_backgroundClip == null || _audioSource == null)
        {
            yield break;
        }

        if (_backgroundClip.loadState == AudioDataLoadState.Unloaded)
        {
            _backgroundClip.LoadAudioData();
        }

        while (_backgroundClip.loadState == AudioDataLoadState.Loading)
        {
            yield return null;
        }

        if (_backgroundClip.loadState != AudioDataLoadState.Loaded)
        {
            Debug.LogWarning($"[BackgroundMusicPlayer] Failed to load clip data: {_backgroundClip.loadState}");
            yield break;
        }

        if (!_audioSource.isPlaying)
        {
            _audioSource.Play();
            Debug.Log($"[BackgroundMusicPlayer] Playing {_backgroundClip.name}");
        }

        _playRoutine = null;
    }

    private static bool IsMainScene(Scene scene)
    {
        return scene.name == SceneLoader.MainSceneName || scene.path == SceneLoader.MainScenePath;
    }
}
