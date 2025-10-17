using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public sealed class RadiometerItem : XRGrabInteractable
{
    [Header("Home")]
    [SerializeField] private BeltHome home;

    [Header("Attach points")]
    [SerializeField] private Transform attachRight;
    [SerializeField] private Transform attachLeftAssist;

    private IXRSelectInteractor _rightInteractor;
    private IXRSelectInteractor _leftInteractor;

    protected override void Awake()
    {
        base.Awake();
        selectMode = InteractableSelectMode.Multiple;
    }

    protected override void OnSelectEntering(SelectEnterEventArgs args)
    {
        HandTag hand = GetHand(args.interactorObject);
        if (hand == null)
        {
            args.manager.CancelInteractableSelection((IXRSelectInteractable)this);
            return;
        }

        // Первой обязана взять ПРАВАЯ рука
        if (_rightInteractor == null && hand.HandType != HandType.Right)
        {
            args.manager.CancelInteractableSelection((IXRSelectInteractable)this);
            Debug.Log("[Radiometer] Сначала берёт правая рука.");
            return;
        }

        if (hand.HandType == HandType.Right)
        {
            _rightInteractor = args.interactorObject;
            if (attachRight != null)
            {
                attachTransform = attachRight;
            }
        }
        else // Left
        {
            if (_rightInteractor == null)
            {
                args.manager.CancelInteractableSelection((IXRSelectInteractable)this);
                Debug.Log("[Radiometer] Сначала правая, затем левая.");
                return;
            }

            _leftInteractor = args.interactorObject;
            if (attachLeftAssist != null)
            {
                attachTransform = attachLeftAssist;
            }
        }

        base.OnSelectEntering(args);
    }

    protected override void OnSelectExiting(SelectExitEventArgs args)
    {
        base.OnSelectExiting(args);

        HandTag hand = GetHand(args.interactorObject);
        if (hand != null)
        {
            if (hand.HandType == HandType.Right) _rightInteractor = null;
            if (hand.HandType == HandType.Left)  _leftInteractor = null;
        }

        // Любая рука отпустила → вернуть в родной сокет
        XRInteractionManager mgr = args.manager != null ? args.manager : FindObjectOfType<XRInteractionManager>();
        if (home != null && mgr != null)
        {
            home.ReturnToBelt(this, mgr);
        }

        _rightInteractor = null;
        _leftInteractor = null;
    }

    public override bool IsSelectableBy(IXRSelectInteractor interactor)
    {
        HandTag hand = GetHand(interactor);
        if (hand == null) return false;

        // Первой — только правая
        if (_rightInteractor == null && hand.HandType != HandType.Right)
            return false;

        // Второй — только левая
        if (_rightInteractor != null && _leftInteractor == null && hand.HandType != HandType.Left)
            return false;

        // Больше двух хватов нельзя
        if (_rightInteractor != null && _leftInteractor != null)
            return false;

        return base.IsSelectableBy(interactor);
    }

    private HandTag GetHand(IXRInteractor interactor)
    {
        Transform t = interactor != null ? interactor.transform : null;
        return t != null ? t.GetComponentInParent<HandTag>() : null;
    }
}
