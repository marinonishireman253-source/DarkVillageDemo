using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class GameViewAudioGuard
{
    private const string GameViewTypeName = "UnityEditor.GameView";
    private const string PlayAudioFieldName = "m_PlayAudio";
    private const string LegacyAudioFieldName = "m_AudioPlay";

    [MenuItem("Tools/Audio/Enable Game View Audio")]
    private static void EnableGameViewAudioMenu()
    {
        EnsureGameViewAudioEnabled();
    }

    private static void EnsureGameViewAudioEnabled()
    {
        if (Application.isBatchMode)
        {
            return;
        }

        Type gameViewType = Type.GetType($"{GameViewTypeName}, UnityEditor");
        if (gameViewType == null)
        {
            Debug.LogWarning("[GameViewAudioGuard] Could not resolve UnityEditor.GameView.");
            return;
        }

        EditorWindow gameViewWindow = EditorWindow.GetWindow(gameViewType);
        if (gameViewWindow == null)
        {
            Debug.LogWarning("[GameViewAudioGuard] Could not access the Game view window.");
            return;
        }

        bool updated = false;
        SerializedObject serializedWindow = new SerializedObject(gameViewWindow);
        updated |= SetBoolProperty(serializedWindow, PlayAudioFieldName, true);
        updated |= SetBoolProperty(serializedWindow, LegacyAudioFieldName, true);
        if (updated)
        {
            serializedWindow.ApplyModifiedPropertiesWithoutUndo();
        }

        FieldInfo playAudioField = gameViewType.GetField(PlayAudioFieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (playAudioField != null && playAudioField.FieldType == typeof(bool))
        {
            playAudioField.SetValue(gameViewWindow, true);
            updated = true;
        }

        FieldInfo legacyAudioField = gameViewType.GetField(LegacyAudioFieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (legacyAudioField != null && legacyAudioField.FieldType == typeof(bool))
        {
            legacyAudioField.SetValue(gameViewWindow, true);
            updated = true;
        }

        if (updated)
        {
            gameViewWindow.Repaint();
            Debug.Log("[GameViewAudioGuard] Game view audio enabled.");
        }
    }

    private static bool SetBoolProperty(SerializedObject serializedObject, string propertyName, bool value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null || property.propertyType != SerializedPropertyType.Boolean)
        {
            return false;
        }

        if (property.boolValue == value)
        {
            return false;
        }

        property.boolValue = value;
        return true;
    }
}
