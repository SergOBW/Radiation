using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public sealed class BeltHome : MonoBehaviour
{
    [SerializeField] private XRSocketInteractor socket; // домашний сокет на поясе

    public XRSocketInteractor Socket
    {
        get { return socket; }
    }

    public void ReturnToBelt(XRGrabInteractable item, XRInteractionManager mgr)
    {
        if (socket == null || item == null || mgr == null)
            return;

        // 1) гарантированно отпускаем всеми, кто держит
        //    ВАЖНО: использовать SelectExit через менеджер и IXRSelectInteractable
        var interactors = item.interactorsSelecting;
        for (int i = interactors.Count - 1; i >= 0; i--)
        {
            var interactor = interactors[i];
            mgr.SelectExit(interactor, (IXRSelectInteractable)item);
        }

        // 2) ручная «посадка» в родной сокет
        //    Start/EndManualInteraction остаются на самом interactor'е (сокете)
        socket.StartManualInteraction((IXRSelectInteractable)item);
        mgr.SelectEnter(socket, (IXRSelectInteractable)item);
        socket.EndManualInteraction();
    }
}