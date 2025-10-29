using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class SceneChangeInteractable : XRBaseInteractable
{
    [Header("Input")]
    [SerializeField] private InputActionReference leftTrigger;

    [Header("Scene to Load")]
    [SerializeField] private string sceneNameToLoad;

    protected override void OnEnable()
    {
        base.OnEnable();
        if (leftTrigger != null)
        {
            leftTrigger.action.Enable();
            leftTrigger.action.performed += OnTriggerPerformed;
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        if (leftTrigger != null)
        {
            leftTrigger.action.performed -= OnTriggerPerformed;
            leftTrigger.action.Disable();
        }
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