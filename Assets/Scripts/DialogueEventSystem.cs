using System;
using System.Collections.Generic;
using UnityEngine;

public static class DialogueEventSystem
{
    private static readonly Dictionary<string, Action<string>> _handlers = new Dictionary<string, Action<string>>();

    public static event Action<DialogueEvent> OnEventRaised;

    public static void Register(string eventId, Action<string> handler)
    {
        if (string.IsNullOrWhiteSpace(eventId) || handler == null)
        {
            return;
        }
        _handlers[eventId] = handler;
    }

    public static void Unregister(string eventId)
    {
        if (string.IsNullOrWhiteSpace(eventId))
        {
            return;
        }
        _handlers.Remove(eventId);
    }

    public static void Raise(DialogueEvent evt)
    {
        if (evt == null)
        {
            return;
        }

        OnEventRaised?.Invoke(evt);

        switch (evt.Type)
        {
            case DialogueEvent.EventType.TriggerQuest:
                HandleTriggerQuest(evt);
                break;

            case DialogueEvent.EventType.SetFlag:
                HandleSetFlag(evt);
                break;

            case DialogueEvent.EventType.PlaySound:
                // TODO: M3 音频系统接入
                Debug.Log($"[DialogueEvent] PlaySound: {evt.Id} (待实现)");
                break;

            case DialogueEvent.EventType.SpawnObject:
                // TODO: 场景生成
                Debug.Log($"[DialogueEvent] SpawnObject: {evt.Id} (待实现)");
                break;

            case DialogueEvent.EventType.Custom:
                if (_handlers.TryGetValue(evt.Id, out Action<string> handler))
                {
                    handler.Invoke(evt.Parameter);
                }
                else
                {
                    Debug.Log($"[DialogueEvent] Custom event '{evt.Id}' param='{evt.Parameter}' (无注册处理器)");
                }
                break;
        }
    }

    private static void HandleTriggerQuest(DialogueEvent evt)
    {
        if (GameStateHub.Instance == null)
        {
            Debug.LogWarning("[DialogueEvent] GameStateHub not found");
            return;
        }

        GameStateHub.Instance.CompleteObjective(evt.Id);
    }

    private static readonly Dictionary<string, bool> _flags = new Dictionary<string, bool>();

    private static void HandleSetFlag(DialogueEvent evt)
    {
        bool value = string.IsNullOrEmpty(evt.Parameter) || evt.Parameter == "true";
        _flags[evt.Id] = value;
        Debug.Log($"[DialogueEvent] Flag '{evt.Id}' = {value}");
    }

    public static bool GetFlag(string flagId)
    {
        return _flags.TryGetValue(flagId, out bool value) && value;
    }

    public static void ClearFlags()
    {
        _flags.Clear();
    }
}
