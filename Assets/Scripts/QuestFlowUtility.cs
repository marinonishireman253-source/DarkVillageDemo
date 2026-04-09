using UnityEngine;

public static class QuestFlowUtility
{
    public static bool RegisterObjectiveById(string objectiveId)
    {
        if (string.IsNullOrWhiteSpace(objectiveId))
        {
            return false;
        }

        QuestObjectiveTarget[] objectives = Object.FindObjectsByType<QuestObjectiveTarget>(FindObjectsSortMode.None);
        for (int i = 0; i < objectives.Length; i++)
        {
            QuestObjectiveTarget objective = objectives[i];
            if (objective == null || objective.ObjectiveId != objectiveId)
            {
                continue;
            }

            objective.RegisterAsCurrentObjective();
            return true;
        }

        return false;
    }

    public static void WarpPlayer(Vector3 worldPosition, Vector3? worldForward = null)
    {
        PlayerMover player = Object.FindFirstObjectByType<PlayerMover>();
        if (player == null)
        {
            return;
        }

        player.transform.position = worldPosition;
        if (worldForward.HasValue)
        {
            Vector3 flatForward = Vector3.ProjectOnPlane(worldForward.Value, Vector3.up);
            if (flatForward.sqrMagnitude > 0.001f)
            {
                player.transform.rotation = Quaternion.LookRotation(flatForward.normalized, Vector3.up);
            }
        }

        Camera mainCamera = Camera.main;
        CameraFollow follow = mainCamera != null ? mainCamera.GetComponent<CameraFollow>() : null;
        if (follow != null)
        {
            follow.SetTarget(player.transform, true);
        }
    }
}
