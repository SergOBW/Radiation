using System;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameHelper : MonoBehaviour
{
    public static GameHelper Instance;
    const float k_DefaultCameraYOffset = 1.36f;

    public float CameraYOffset { get; private set; }
    public bool CanWalk = true;
    public bool CanSkip = false;

    private XROrigin _xrOrigin;
    private CharacterController _characterController;
    private void Awake()
    {
        DontDestroyOnLoad(this);
        CameraYOffset = k_DefaultCameraYOffset;
        Instance = this;
        SceneManager.sceneLoaded += OnsceneLoaded;
        Initialize();
    }

    private void OnsceneLoaded(Scene arg0, LoadSceneMode arg1)
    {
        Initialize();
    }

    private void Initialize()
    {
        _xrOrigin = FindObjectOfType<XROrigin>(true);
        _xrOrigin.CameraYOffset = CameraYOffset;
        _characterController = _xrOrigin.GetComponent<CharacterController>();
    }

    public void MakeOffset(float value)
    {
        _xrOrigin = FindObjectOfType<XROrigin>(true);
        CameraYOffset = value;
        _xrOrigin.CameraYOffset = CameraYOffset;
        
    }

    private void Update()
    {
        if (_characterController != null)
        {
            _characterController.enabled = CanWalk;
        }
    }
}
