using UnityEngine;

public class DrowningWater : MonoBehaviour
{
    public float drowningTime = 2f;
    public string playerTag = "Player";
    public GameObject waterSurface; // Assign your water surface GameObject here
    public float floatSpeed = 0.5f;
    public float floatStopThreshold = 0.1f;

    private GameObject player;
    private float timeInWater = 0f;
    private bool isDrowning = false;
    private Rigidbody[] playerRigidbodies;
    private Animator playerAnimator;
    private Transform playerRootBone; // Usually pelvis for floating
    private LogicManager logicManager; // Reference to your LogicManager
    private AI_Voice_Script aiVoiceScript; // Reference to the AI Voice Script

    void Start()
    {
        if (waterSurface == null)
        {
            Debug.LogError("Water Surface GameObject not assigned on " + gameObject.name);
            enabled = false;
        }

        // Find the LogicManager instance
        logicManager = FindObjectOfType<LogicManager>();
        if (logicManager == null)
        {
            Debug.LogError("LogicManager not found in the scene!");
        }

        // Find the AI_Voice_Script instance
        aiVoiceScript = FindObjectOfType<AI_Voice_Script>();
        if (aiVoiceScript == null)
        {
            Debug.LogError("AI_Voice_Script not found in the scene!");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag) && !isDrowning)
        {
            player = other.gameObject;
            timeInWater = 0f;
            playerAnimator = player.GetComponentInChildren<Animator>();
            playerRigidbodies = player.GetComponentsInChildren<Rigidbody>();

            playerRootBone = null; // Initialize to null

            // Directly get the Rigidbody on the player's root GameObject
            Rigidbody playerRb = player.GetComponent<Rigidbody>();
            if (playerRb != null)
            {
                playerRootBone = player.transform;
                Debug.Log("Using player's Rigidbody transform for floating on " + player.name + (playerAnimator != null ? " (Animator found)" : " (No Animator)"));
            }
            else if (playerAnimator != null && playerAnimator.avatar.isHuman)
            {
                Transform hipsBone = playerAnimator.GetBoneTransform(HumanBodyBones.Hips);
                if (hipsBone != null && hipsBone.GetComponent<Rigidbody>() != null)
                {
                    playerRootBone = hipsBone;
                    Debug.Log("Using Hips bone's Rigidbody transform for floating on " + player.name);
                }
                else
                {
                    Debug.LogWarning("No Rigidbody found on player root or Hips bone for floating on " + player.name + " (Animator found).");
                }
            }
            else if (playerRigidbodies.Length > 0)
            {
                playerRootBone = playerRigidbodies[0].transform;
                Debug.LogWarning("No Animator or Rigidbody on player root/Hips. Using transform of first child Rigidbody for floating on " + player.name);
            }
            else
            {
                Debug.LogWarning("Could not find a suitable Rigidbody transform for floating on " + player.name);
                player = null;
            }

            Debug.Log(playerRootBone ? "Root bone for floating: " + playerRootBone.name : "No root bone for floating found.");
            Debug.Log(player.name + " entered water.");
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag(playerTag) && player != null && !isDrowning)
        {
            timeInWater += Time.deltaTime;
            if (timeInWater >= drowningTime)
            {
                StartDrowning();
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag) && !isDrowning)
        {
            player = null;
            timeInWater = 0f;
            Debug.Log(other.name + " exited water before drowning.");
        }
    }

    void StartDrowning()
    {
        isDrowning = true;
        Debug.Log(player.name + " is drowning! Setting 'Dead' trigger on Animator.");

        // Disable player control scripts
        Player movementController = player.GetComponent<Player>();
        if (movementController != null)
        {
            movementController.enabled = false;
        }

        // **SET THE 'Dead' TRIGGER ON THE ANIMATOR**
        if (playerAnimator != null)
        {
            playerAnimator.SetTrigger("Dead"); // Assuming you have a trigger parameter named "Dead"
            Debug.Log("Set 'Dead' trigger on Animator for " + player.name);

            // Call GameOver and handle physics after a short delay (Animator now controls the animation)
            Invoke("DelayedDeathAndGameOver", 1.5f); // Adjust delay based on your Animator's transition
        }
        else
        {
            Debug.LogWarning("Player Animator is null on " + player.name + ".");
            // If no animator, proceed with limp death and game over immediately
            InitiateLimpDeathAndGameOver();
        }

        // Play death sound from AI_Voice_Script
        if (aiVoiceScript != null)
        {
            aiVoiceScript.GetComponent<AudioSource>().Stop(); // Optionally stop any ongoing AI voice
            aiVoiceScript.PlayRandomDeathSound();
        }
        else
        {
            Debug.LogError("AI_Voice_Script reference is null. Cannot play death sound.");
        }
    }

    void DelayedDeathAndGameOver()
    {
        InitiateLimpDeathAndGameOver();
    }

    void InitiateLimpDeathAndGameOver()
    {
        Debug.Log("Initiating limp death and game over.");

        // **DISABLE THE ANIMATOR** (so physics can take over)
        if (playerAnimator != null)
        {
            playerAnimator.enabled = false;
            Debug.Log("Animator DISABLED on " + player.name + " after setting 'Dead' trigger.");
        }

        // Get the Rigidbody on the parent
        Rigidbody playerRb = player.GetComponent<Rigidbody>();
        if (playerRb != null)
        {
            playerRb.isKinematic = false; // Let physics take over
            playerRb.useGravity = true;     // Ensure gravity is enabled
            //playerRb.constraints = RigidbodyConstraints.None; // Remove any rotation/position constraints
            playerRb.linearVelocity = Random.insideUnitSphere * 2f; // Reduced force for a single body
            playerRb.angularVelocity = Random.insideUnitSphere * 5f; // Reduced torque
            Debug.Log("Parent Rigidbody activated for limp death.");
        }
        else
        {
            Debug.LogError("Parent GameObject (" + player.name + ") has no Rigidbody for physics-based death.");
        }

        // Call GameOver on the LogicManager
        if (logicManager != null)
        {
            logicManager.GameOver();
        }
        else
        {
            Debug.LogError("LogicManager reference is null. Cannot trigger Game Over.");
        }

        // Start floating
        if (playerRootBone != null && waterSurface != null)
        {
            StartCoroutine(FloatToSurface());
        }
    }

    public float waterDensity = 1f; // Adjust for different liquid densities
    public float submergedThreshold = 0.5f; // How far below water to start strong buoyancy

    System.Collections.IEnumerator FloatToSurface()
    {
        Rigidbody rootRigidbody = playerRootBone.GetComponent<Rigidbody>();

        if (rootRigidbody == null)
        {
            Debug.LogError("Root bone (" + playerRootBone.name + ") does not have a Rigidbody. Natural floating will not work.");
            yield break;
        }

        rootRigidbody.linearDamping = 0.8f;

        while (isDrowning && waterSurface != null)
        {
            float surfaceY = waterSurface.transform.position.y;
            float rootY = playerRootBone.position.y;
            float depth = surfaceY - rootY;

            Vector3 buoyancyForce = Vector3.up * waterDensity * rootRigidbody.mass * Mathf.Clamp01(depth / submergedThreshold);

            rootRigidbody.AddForce(buoyancyForce, ForceMode.Force);

            yield return null;
        }

        if (rootRigidbody != null)
        {
            rootRigidbody.linearVelocity = Vector3.zero;
            rootRigidbody.angularVelocity = Vector3.zero;
            rootRigidbody.isKinematic = true;
        }
        Debug.Log(player.name + " reached the water surface naturally.");
    }
}