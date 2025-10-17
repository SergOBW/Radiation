using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public sealed class TabletItem : XRGrabInteractable
{
    [Header("Home")]
    [SerializeField] private BeltHome home;

    [Header("Attach (left hand only)")]
    [SerializeField] private Transform attachLeft;

    protected override void Awake()
    {
        base.Awake();
        selectMode = InteractableSelectMode.Single;
    }

    protected override void OnSelectEntering(SelectEnterEventArgs args)
    {
        HandTag hand = GetHand(args.interactorObject);
        if (hand == null || hand.HandType != HandType.Left)
        {
            // явный каст к IXRSelectInteractable — иначе ловишь obsolete на старой перегрузке
            args.manager.CancelInteractableSelection((IXRSelectInteractable)this);
            Debug.Log("[Tablet] Брать можно только левой рукой.");
            return;
        }

        if (attachLeft != null)
        {
            attachTransform = attachLeft;
        }

        base.OnSelectEntering(args);
    }

    protected override void OnSelectExiting(SelectExitEventArgs args)
    {
        base.OnSelectExiting(args);

        // Игрок отпустил Grab → вернуть в домашний сокет
        BeltHome back = home;
        XRInteractionManager mgr = args.manager != null ? args.manager : FindObjectOfType<XRInteractionManager>();
        if (back != null && mgr != null)
        {
            back.ReturnToBelt(this, mgr);
        }
    }

    public override bool IsSelectableBy(IXRSelectInteractor interactor)
    {
        HandTag hand = GetHand(interactor);
        if (hand == null || hand.HandType != HandType.Left)
        {
            return false;
        }
        return base.IsSelectableBy(interactor);
    }

    private HandTag GetHand(IXRInteractor interactor)
    {
        Transform t = interactor != null ? interactor.transform : null;
        return t != null ? t.GetComponentInParent<HandTag>() : null;
    }
}
