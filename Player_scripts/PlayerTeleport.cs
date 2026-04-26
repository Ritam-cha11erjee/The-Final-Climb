using UnityEngine;

public class PlayerTeleport : MonoBehaviour
{
    [Tooltip("Assign the root GameObject of the player here.")]
    public Transform playerRoot;

    private Transform teleportTarget;
    private bool isInTeleportTrigger = false;

    void Awake()
    {
        if (playerRoot == null)
        {
            Debug.LogError("Player Root Transform is not assigned in the Inspector for PlayerTeleport on " + gameObject.name + "!");
            enabled = false;
            return;
        }
    }

    public void SetTeleportTarget(Transform target)
    {
        teleportTarget = target;
        isInTeleportTrigger = true;
    }

    public void ClearTeleportTarget()
    {
        teleportTarget = null;
        isInTeleportTrigger = false;
    }

    void Update()
    {
        if (playerRoot != null && isInTeleportTrigger && Input.GetKeyDown(KeyCode.E) && teleportTarget != null)
        {
            // Perform the teleport on the player root ONLY
            playerRoot.position = teleportTarget.position;
            Debug.Log("<color=red>TELEPORTED PLAYER TO: " + teleportTarget.position + "</color>");
        }
    }
}