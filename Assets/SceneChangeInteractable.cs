using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class SceneChangeInteractable : XRSimpleInteractable
{
    [Header("Input")]
    [SerializeField] private InputActionReference leftTrigger;

    [Header("Scene to Load")]
    [SerializeField] private string sceneNameToLoad;

    private bool isHovered = false;

    protected override void OnEnable()
    {
        base.OnEnable();
        if (leftTrigger != null)
        {
            leftTrigger.action.Enable();
            leftTrigger.action.performed += OnTriggerPerformed;
        }
        hoverEntered.AddListener(OnHoverEntered);
        hoverExited.AddListener(OnHoverExited);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        if (leftTrigger != null)
        {
            leftTrigger.action.performed -= OnTriggerPerformed;
            leftTrigger.action.Disable();
        }
        hoverEntered.RemoveListener(OnHoverEntered);
        hoverExited.RemoveListener(OnHoverExited);
    }

    private void OnTriggerPerformed(InputAction.CallbackContext context)
    {
        if (isHovered)
        {
            if (!string.IsNullOrEmpty(sceneNameToLoad))
            {
                SceneManager.LoadScene(sceneNameToLoad);
            }
            else
            {
                Debug.LogWarning("Scene name to load is not set.");
            }
        }
    }
}