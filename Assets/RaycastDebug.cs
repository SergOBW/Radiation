using UnityEngine;

public sealed class RaycastDebug : MonoBehaviour
{
    public float distance = 5f;
    public LayerMask mask = ~0;
    public QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide;

    private void Update()
    {
        var ray = new Ray(transform.position, transform.forward);
        var hits = Physics.RaycastAll(ray, distance, mask, triggerInteraction);
        System.Array.Sort(hits, (a,b) => a.distance.CompareTo(b.distance));

        if (hits.Length > 0)
        {
            Debug.DrawLine(ray.origin, hits[0].point, Color.green);
            string chain = "";
            for (int i = 0; i < hits.Length; i++)
                chain += $"{i}: {hits[i].collider.name} [layer={LayerMask.LayerToName(hits[i].collider.gameObject.layer)}] dist={hits[i].distance:F3}\n";
            //Debug.Log($"[RaycastDebug]\n{chain}");
        }
        else
        {
            Debug.DrawRay(ray.origin, ray.direction * distance, Color.red);
        }
    }
}
