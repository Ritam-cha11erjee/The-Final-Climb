using UnityEngine;
using System.Collections.Generic;

public class DistanceCulling : MonoBehaviour
{
    [Tooltip("The camera to check distance from. If null, the main camera will be used.")]
    public Camera targetCamera;

    [Tooltip("The distance beyond which objects will be culled.")]
    public float cullDistance = 30f;

    [Tooltip("Should the object's renderer be disabled?")]
    public bool disableRenderer = true;

    [Tooltip("Should other components (besides Renderer) be disabled? Add their script names here.")]
    public string[] disableComponents;

    private List<CullableObject> cullableObjects = new List<CullableObject>();

    private class CullableObject
    {
        public Renderer renderer;
        public Behaviour[] components;
        public bool isCulled = false;
    }

    void Start()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            if (targetCamera == null)
            {
                Debug.LogError("No main camera found and no target camera assigned to " + gameObject.name);
                enabled = false;
                return;
            }
        }

        FindCullableObjects(transform);

        // Initial check for all cullable objects
        foreach (var cullable in cullableObjects)
        {
            UpdateCullingState(cullable);
        }
    }

    void Update()
    {
        foreach (var cullable in cullableObjects)
        {
            UpdateCullingState(cullable);
        }
    }

    void FindCullableObjects(Transform parent)
    {
        // Check the parent itself
        Renderer renderer = parent.GetComponent<Renderer>();
        List<Behaviour> componentsToDisable = new List<Behaviour>();

        if (disableComponents != null)
        {
            foreach (string componentName in disableComponents)
            {
                Component component = parent.GetComponent(componentName);
                if (component != null && component is Behaviour)
                {
                    componentsToDisable.Add((Behaviour)component);
                }
                else if (component != null)
                {
                    Debug.LogWarning($"Component '{componentName}' on {parent.gameObject.name} is not a Behaviour and cannot be disabled by this script.");
                }
                else
                {
                    // Don't spam warnings if the component isn't found on every child
                }
            }
        }

        if (renderer != null || componentsToDisable.Count > 0)
        {
            cullableObjects.Add(new CullableObject { renderer = renderer, components = componentsToDisable.ToArray() });
        }

        // Recursively check children
        foreach (Transform child in parent)
        {
            FindCullableObjects(child);
        }
    }

    void UpdateCullingState(CullableObject cullable)
    {
        if (targetCamera == null) return;

        Transform targetTransform = cullable.renderer != null ? cullable.renderer.transform : (cullable.components.Length > 0 ? cullable.components[0].transform : null);
        if (targetTransform == null) return; // Should not happen, but safety check

        float distanceToCamera = Vector3.Distance(targetTransform.position, targetCamera.transform.position);

        if (distanceToCamera > cullDistance)
        {
            if (!cullable.isCulled)
            {
                SetCulled(cullable, true);
            }
        }
        else
        {
            if (cullable.isCulled)
            {
                SetCulled(cullable, false);
            }
        }
    }

    void SetCulled(CullableObject cullable, bool culled)
    {
        cullable.isCulled = culled;

        if (disableRenderer && cullable.renderer != null)
        {
            cullable.renderer.enabled = !culled;
        }

        foreach (var component in cullable.components)
        {
            if (component != null)
            {
                component.enabled = !culled;
            }
        }
    }

    // Optional: Gizmo to visualize the cull distance (drawn at the parent's position)
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, cullDistance);
    }
}