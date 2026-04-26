using UnityEngine;
using UnityEngine.Events;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }
    private Vector3 currentCheckpoint;
    public UnityEvent<Vector3> OnCheckpointActivated; // Add this

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Make the CheckpointManager persist across scene loads
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }

        // Initialize checkpoint (e.g., from saved data or a default)
        currentCheckpoint = Vector3.zero; // Or your initial checkpoint
        if (OnCheckpointActivated == null)
        {
            OnCheckpointActivated = new UnityEvent<Vector3>();
        }
        // Load saved checkpoint if it exists (optional, for game sessions)
        // LoadCheckpoint();
    }

    public void SetCheckpoint(Vector3 position)
    {
        currentCheckpoint = position;
        Debug.Log("Checkpoint set at: " + currentCheckpoint);
        OnCheckpointActivated?.Invoke(currentCheckpoint); // Invoke the event
        // SaveCheckpoint(); // Optional: Save checkpoint for future game sessions
    }

    public Vector3 GetCheckpoint()
    {
        return currentCheckpoint;
    }

    public bool HasCheckpoint()
    {
        return currentCheckpoint != Vector3.zero; // Or your initial default value check
    }

    // Optional methods for saving and loading checkpoint data (e.g., using PlayerPrefs)
    // void SaveCheckpoint() { ... }
    // void LoadCheckpoint() { ... }
}