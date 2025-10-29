using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AdminPanel : MonoBehaviour
{
    [SerializeField] private Slider offsetSlider;

    [SerializeField] private TMP_InputField moneyAmount;
    [SerializeField] private Button addMoneyButton;
    
    private bool _isActive;

    [SerializeField] private GameObject canvas;
    void Start()
    {
        DontDestroyOnLoad(this);
        offsetSlider.minValue = 0;
        offsetSlider.maxValue = 5;
        offsetSlider.value =  GameHelper.Instance.CameraYOffset;
        offsetSlider.onValueChanged.AddListener(MakeOffset);
        addMoneyButton.onClick.AddListener(AddMoney);
        UpdateState();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.BackQuote))
        {
            _isActive = !_isActive;
            UpdateState();
        }
    }

    private void UpdateState()
    {
        canvas.gameObject.SetActive(_isActive);
    }

    private void MakeOffset(float value)
    {
        GameHelper.Instance.MakeOffset(value);
    }

    private void AddMoney()
    {
        int.TryParse(moneyAmount.text, out int value);
    }
    
}
