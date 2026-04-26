using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraUnderwaterEffect : MonoBehaviour
{
    public LayerMask waterLayers;
    public Shader shader;

    [Header("Depth Effect")]
    public Color depthColor = new Color(0, 0.42f, 0.87f);
    public float depthStart = -12f;
    public float depthEnd = 98f;
    public LayerMask depthLayers = ~0; // All layers

    private Camera cam, depthCam;
    private RenderTexture depthTexture, colourTexture;
    private Material material;
    private bool inWater = false;

    void Start()
    {
        cam = GetComponent<Camera>();

        // Make our camera send depth information to the shader as well
        cam.depthTextureMode = DepthTextureMode.Depth;

        // Create a material using the assigned shader
        if (shader == null)
        {
            Debug.LogError("Underwater Shader is not assigned in the Inspector!");
            enabled = false; // Disable the script if no shader is assigned
            return;
        }
        material = new Material(shader);

        // Create render textures
        int width = cam.pixelWidth;
        int height = cam.pixelHeight;
        depthTexture = RenderTexture.GetTemporary(width, height, 16, RenderTextureFormat.Depth);
        colourTexture = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.Default);

        // Create depthCam
        GameObject go = new GameObject("Depth Cam");
        depthCam = go.AddComponent<Camera>();
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero; // Reset local position
        go.transform.localRotation = Quaternion.identity; // Reset local rotation

        // Copy main camera settings for depthCam
        depthCam.CopyFrom(cam);
        depthCam.cullingMask = depthLayers;
        depthCam.depthTextureMode = DepthTextureMode.Depth;
        depthCam.clearFlags = CameraClearFlags.Color; // Ensure it clears to a color
        depthCam.backgroundColor = Color.black; // Clear to black for depth
        depthCam.enabled = false; // We'll render it manually

        // Send the depth texture to the shader
        material.SetTexture("_DepthMap", depthTexture);
    }

    private void OnApplicationQuit()
    {
        RenderTexture.ReleaseTemporary(depthTexture);
        RenderTexture.ReleaseTemporary(colourTexture);
    }

    private void FixedUpdate()
    {
        // Get the camera frustum corners
        Vector3[] corners = new Vector3[4];
        cam.CalculateFrustumCorners(new Rect(0, 0, 1, 1), cam.nearClipPlane, Camera.MonoOrStereoscopicEye.Mono, corners);

        // Check for water level
        RaycastHit hit;
        Vector3 start = transform.position + transform.TransformVector(corners[1]); // Top Left
        Vector3 end = transform.position + transform.TransformVector(corners[0]);   // Bottom Left

        Collider[] waterCollidersAtBottom = Physics.OverlapSphere(end, 0.01f, waterLayers);
        if (waterCollidersAtBottom.Length > 0)
        {
            inWater = true;
            Collider[] waterCollidersAtTop = Physics.OverlapSphere(start, 0.01f, waterLayers);
            if (waterCollidersAtTop.Length > 0)
            {
                material.SetVector("_WaterLevel", new Vector2(0, 1)); // Fully submerged
            }
            else
            {
                if (Physics.Linecast(start, end, out hit, waterLayers))
                {
                    float delta = hit.distance / (end - start).magnitude;
                    material.SetVector("_WaterLevel", new Vector2(0, 1 - delta)); // Partially submerged
                }
            }
        }
        else
        {
            inWater = false;
        }
    }

    private void Reset()
    {
        // Try to find the shader if not already assigned
        if (shader == null)
        {
            Shader foundShader = Shader.Find("Hidden/CameraUnderwaterEffect");
            if (foundShader != null)
            {
                shader = foundShader;
            }
        }
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (material && inWater)
        {
            // Render the depth texture
            depthCam.targetTexture = depthTexture;
            depthCam.Render();
            depthCam.targetTexture = null; // Reset target texture

            // Pass properties to the material
            material.SetColor("_DepthColor", depthColor);
            material.SetFloat("_DepthStart", depthStart);
            material.SetFloat("_DepthEnd", depthEnd);
            material.SetTexture("_MainTex", source); // Pass the main camera's view
            material.SetTexture("_DepthMap", depthTexture); // Ensure this is called EVERY frame in water

            // Apply the effect
            Graphics.Blit(source, destination, material);
        }
        else
        {
            // If not in water, just copy the source to the destination
            Graphics.Blit(source, destination);
        }
    }
}