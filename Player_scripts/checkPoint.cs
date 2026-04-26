using UnityEngine;
using System.Collections;

public class PlayerCheckpoint : MonoBehaviour
{
    private Vector3 targetCheckpointPosition;
    public float holdDuration = 1f; // Duration to force position
    private float holdStartTime;
    private bool isHoldingPosition = false;
    private bool hasTeleportedOnStart = false;

    void Start()
    {
        // Check if a checkpoint is saved AND if we are respawning
        if (CheckpointManager.Instance != null && CheckpointManager.Instance.HasCheckpoint() && LogicManager.isRespawning)
        {
            targetCheckpointPosition = CheckpointManager.Instance.GetCheckpoint();
            transform.position = targetCheckpointPosition;
            Debug.Log("Teleported to checkpoint on respawn: " + targetCheckpointPosition);
            StartHolding(); // Begin holding immediately
            hasTeleportedOnStart = true;
            // Reset the respawn flag as the respawn action is complete for this load
            LogicManager.isRespawning = false;
        }
        else if (CheckpointManager.Instance != null && CheckpointManager.Instance.HasCheckpoint() && !hasTeleportedOnStart)
        {
            // Initial teleport on first scene load (no holding)
            transform.position = CheckpointManager.Instance.GetCheckpoint();
            Debug.Log("Initial teleport to checkpoint: " + transform.position);
            hasTeleportedOnStart = true;
        }
        else
        {
            isHoldingPosition = false;
        }
    }

    void StartHolding()
    {
        isHoldingPosition = true;
        holdStartTime = Time.time;
    }

    void Update()
    {
        if (isHoldingPosition)
        {
            if (Time.time < holdStartTime + holdDuration)
            {
                Debug.Log("Holding Checkpoint on Respawn");
                // Force the player's position to the checkpoint every frame
                transform.position = targetCheckpointPosition;
                // Optionally, zero out Rigidbody velocity
                if (GetComponent<Rigidbody>() != null)
                {
                    GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
                    GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
                }
            }
            else
            {
                // Stop forcing the position after the duration
                isHoldingPosition = false;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("check"))
        {
            if (CheckpointManager.Instance != null)
            {
                CheckpointManager.Instance.SetCheckpoint(other.transform.position);
                Debug.Log("Checkpoint reached and saved.");
                // We don't start holding here anymore
            }
        }
    }
}