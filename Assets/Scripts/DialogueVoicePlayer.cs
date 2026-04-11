using System.Collections.Generic;
using UnityEngine;

public class DialogueVoicePlayer : MonoBehaviour
{
    public static DialogueVoicePlayer Instance { get; private set; }

    [SerializeField] private float volume = 0.85f;

    private AudioSource _audioSource;
    private readonly Dictionary<string, AudioClip> _clipByExactText = new Dictionary<string, AudioClip>();
    private readonly Dictionary<string, AudioClip> _clipBySpeakerLine = new Dictionary<string, AudioClip>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 0f;
        _audioSource.volume = volume;
        _audioSource.priority = 64;
    }

    /// <summary>
    /// Register a clip matched by speaker name + line text (exact prefix match).
    /// </summary>
    public void RegisterClip(string speakerName, string lineText, AudioClip clip)
    {
        if (clip == null || string.IsNullOrEmpty(lineText)) return;
        _clipByExactText[$"{speakerName}::{lineText}"] = clip;
    }

    /// <summary>
    /// Register a clip matched by speaker name + line index (for sequential dialogue).
    /// </summary>
    public void RegisterClipByIndex(string speakerName, int lineIndex, AudioClip clip)
    {
        if (clip == null) return;
        _clipBySpeakerLine[$"{speakerName}::{lineIndex}"] = clip;
    }

    public void PlayLine(string speakerName, int lineIndex, string lineText)
    {
        if (!_isOpen) return;

        // Try exact text match first
        string textKey = $"{speakerName}::{lineText}";
        if (_clipByExactText.TryGetValue(textKey, out AudioClip clip) && clip != null)
        {
            PlayClip(clip);
            return;
        }

        // Fallback: try prefix match
        foreach (var kvp in _clipByExactText)
        {
            if (kvp.Key.StartsWith($"{speakerName}::") && lineText.Contains(kvp.Key.Substring(kvp.Key.IndexOf("::") + 2, Mathf.Min(6, lineText.Length))))
            {
                PlayClip(kvp.Value);
                return;
            }
        }

        // Fallback: try index match
        string indexKey = $"{speakerName}::{lineIndex}";
        if (_clipBySpeakerLine.TryGetValue(indexKey, out clip) && clip != null)
        {
            PlayClip(clip);
        }
    }

    private void PlayClip(AudioClip clip)
    {
        _audioSource.Stop();
        _audioSource.clip = clip;
        _audioSource.Play();
    }

    public void Stop()
    {
        if (_audioSource != null && _audioSource.isPlaying)
        {
            _audioSource.Stop();
        }
    }

    private bool _isOpen;

    public void OnDialogueOpened()
    {
        _isOpen = true;
    }

    public void OnDialogueClosed()
    {
        _isOpen = false;
        Stop();
    }

    /// <summary>
    /// Load voice clips from Resources/Audio/Voice/
    /// </summary>
    public void LoadDefaultClips()
    {
        // Sagiri - 软萌少女音旁白
        LoadClipByIndex("Sagiri", 0, "Sagiri/sagiri_explore01");
        LoadClipByIndex("Sagiri", 1, "Sagiri/sagiri_explore02");
        LoadClipByIndex("Sagiri", 2, "Sagiri/sagiri_combat01");
        LoadClipByIndex("Sagiri", 3, "Sagiri/sagiri_combat02");
        LoadClipByIndex("Sagiri", 4, "Sagiri/sagiri_discovery01");

        // Also register by text for narration triggers
        LoadClipByText("Sagiri", "这里……好像很久没人来过了。", "Sagiri/sagiri_explore01");
        LoadClipByText("Sagiri", "什么声音……", "Sagiri/sagiri_explore02");
        LoadClipByText("Sagiri", "我不会退缩的。", "Sagiri/sagiri_combat01");
        LoadClipByText("Sagiri", "得继续前进……", "Sagiri/sagiri_combat02");
        LoadClipByText("Sagiri", "这是……有人留下的记录。", "Sagiri/sagiri_discovery01");

    }

    private void LoadClipByIndex(string speakerName, int lineIndex, string resourcePath)
    {
        AudioClip clip = Resources.Load<AudioClip>($"Audio/Voice/{resourcePath}");
        if (clip != null)
        {
            RegisterClipByIndex(speakerName, lineIndex, clip);
        }
        else
        {
            Debug.LogWarning($"[DialogueVoicePlayer] Missing clip: Audio/Voice/{resourcePath}");
        }
    }

    private void LoadClipByText(string speakerName, string lineText, string resourcePath)
    {
        AudioClip clip = Resources.Load<AudioClip>($"Audio/Voice/{resourcePath}");
        if (clip != null)
        {
            RegisterClip(speakerName, lineText, clip);
        }
    }
}
