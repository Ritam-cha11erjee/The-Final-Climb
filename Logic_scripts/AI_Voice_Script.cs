using UnityEngine;
using System.Collections.Generic;
using System.Linq; // For ToList()

public class AI_Voice_Script : MonoBehaviour
{
    [Header("Original Audio Arrays")]
    public AudioClip[] originalGroundAudio;
    public AudioClip[] originalAir1Audio;
    public AudioClip[] originalAir2Audio;
    public AudioClip[] originalFinalStageAudio;

    [Header("Death Audio")]
    public AudioClip[] deathAudio;

    [Header("Height Ranges")]
    public float groundMaxHeight = 50f;
    public float air1MinHeight = 200f;
    public float air1MaxHeight = 300f;
    public float air2MinHeight = 301f;
    public float air2MaxHeight = 450f;
    public float finalStageMinHeight = 500f;

    [Header("Playback Settings")]
    [SerializeField] private float playInterval = 10f;
    private float timer;
    private bool canPlayHeightSounds = true; // Flag to control height-based audio

    private Transform playerTransform;
    private AudioSource audioSource;

    // Persistent lists to track remaining audio for each height
    private List<AudioClip> remainingGroundAudio;
    private List<AudioClip> remainingAir1Audio;
    private List<AudioClip> remainingAir2Audio;
    private List<AudioClip> remainingFinalStageAudio;

    // Singleton instance
    public static AI_Voice_Script Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Initialize the remaining audio lists as copies of the originals
            remainingGroundAudio = originalGroundAudio.ToList();
            remainingAir1Audio = originalAir1Audio.ToList();
            remainingAir2Audio = originalAir2Audio.ToList();
            remainingFinalStageAudio = originalFinalStageAudio.ToList();
        }
        else
        {
            Destroy(gameObject);
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
        else
        {
            Debug.LogError("Player GameObject not found with tag 'Player' for AI Voice Script.");
            enabled = false;
        }

        timer = playInterval;
    }

    private void Update()
    {
        if (playerTransform == null || !canPlayHeightSounds) return;

        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            timer = playInterval;
            PlayUniqueRandomAudioBasedOnHeight();
        }
    }

    private void PlayUniqueRandomAudioBasedOnHeight()
    {
        float playerHeight = playerTransform.position.y;
        List<AudioClip> currentRemainingList = null;

        if (playerHeight >= 0f && playerHeight <= groundMaxHeight)
        {
            currentRemainingList = remainingGroundAudio;
            Debug.Log("Player at Ground Level (Height: " + playerHeight + ")");
        }
        else if (playerHeight >= air1MinHeight && playerHeight <= air1MaxHeight)
        {
            currentRemainingList = remainingAir1Audio;
            Debug.Log("Player at Air Level 1 (Height: " + playerHeight + ")");
        }
        else if (playerHeight >= air2MinHeight && playerHeight <= air2MaxHeight)
        {
            currentRemainingList = remainingAir2Audio;
            Debug.Log("Player at Air Level 2 (Height: " + playerHeight + ")");
        }
        else if (playerHeight >= finalStageMinHeight)
        {
            currentRemainingList = remainingFinalStageAudio;
            Debug.Log("Player at Final Stage (Height: " + playerHeight + ")");
        }
        else
        {
            Debug.Log("Player height is outside defined audio ranges (Height: " + playerHeight + ")");
            return;
        }

        PlayRandomClipFromList(currentRemainingList);
    }

    private void PlayRandomClipFromList(List<AudioClip> clipList)
    {
        if (clipList != null && clipList.Count > 0)
        {
            int randomIndex = Random.Range(0, clipList.Count);
            AudioClip clipToPlay = clipList[randomIndex];

            if (clipToPlay != null)
            {
                audioSource.PlayOneShot(clipToPlay);

                // Remove the played clip from the persistent list
                if (clipList.GetType() != typeof(List<AudioClip>)) Debug.LogError("clipList is not a generic List<AudioClip>");
                clipList.RemoveAt(randomIndex);

                // If the list becomes empty, you might want to handle it (e.g., loop or stop)
                if (clipList.Count == 0 && clipList.Equals(remainingGroundAudio)) remainingGroundAudio = originalGroundAudio.ToList();
                else if (clipList.Count == 0 && clipList.Equals(remainingAir1Audio)) remainingAir1Audio = originalAir1Audio.ToList();
                else if (clipList.Count == 0 && clipList.Equals(remainingAir2Audio)) remainingAir2Audio = originalAir2Audio.ToList(); // Typo fix
                else if (clipList.Count == 0 && clipList.Equals(remainingFinalStageAudio)) remainingFinalStageAudio = originalFinalStageAudio.ToList();
            }
            else
            {
                Debug.LogWarning("Selected audio clip is null.");
            }
        }
        else if (clipList != null)
        {
            Debug.Log("No remaining audio clips for the current state.");
        }
    }

    // Public method to play a random death sound
    public void PlayRandomDeathSound()
    {
        canPlayHeightSounds = false; // Stop height-based sounds when death sound plays
        if (deathAudio != null && deathAudio.Length > 0)
        {
            int randomIndex = Random.Range(0, deathAudio.Length);
            audioSource.PlayOneShot(deathAudio[randomIndex]);
        }
        else
        {
            Debug.LogWarning("No death audio clips assigned.");
        }
    }

    private void OnDestroy()
    {
        // No need to unsubscribe here as DrowningWater will handle the death event
    }
}