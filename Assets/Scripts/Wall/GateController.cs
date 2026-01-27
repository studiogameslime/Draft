using System.Collections.Generic;
using UnityEngine;

public class GateController : MonoBehaviour
{
    [Header("Points")]
    [SerializeField] private Transform[] entryPoints;
    [SerializeField] private Transform[] exitPoints;

    [Header("Entry Pick")]
    [SerializeField] private int pickFromClosestCount = 2;

    [Header("Visual")]
    [SerializeField] private Animator gateAnimator;
    [SerializeField] private string isOpenBoolName = "IsOpen";

    private class GatePath
    {
        public Transform entry;
        public Transform exit;
        public bool entered;
        public bool exited;
    }

    private readonly Dictionary<int, GatePath> activePaths = new Dictionary<int, GatePath>();
    private int passersInside = 0;

    private void Awake()
    {
        GateRegistry.Register(this);
        SetOpenVisual(false);
    }

    private void OnDestroy()
    {
        GateRegistry.Unregister(this);
    }

    // CHANGED: Returns a stable entry and exit per unit (cached in activePaths)
    public void GetOrCreatePathForUnit(Transform unit, out Transform entry, out Transform exit)
    {
        entry = null;
        exit = null;

        if (unit == null) return;

        int id = unit.GetInstanceID();

        GatePath path;
        if (activePaths.TryGetValue(id, out path))
        {
            entry = path.entry;
            exit = path.exit;
            return;
        }

        path = new GatePath();
        path.entry = PickEntryFor(unit.position);
        path.exit = PickExitFor(path.entry);

        activePaths[id] = path;

        entry = path.entry;
        exit = path.exit;
    }

    // CHANGED: Open only when unit actually reaches entry
    public void NotifyUnitReachedEntry(Transform unit)
    {
        if (unit == null) return;

        int id = unit.GetInstanceID();

        GatePath path;
        if (!activePaths.TryGetValue(id, out path))
            return;

        if (path.entered)
            return;

        path.entered = true;

        passersInside++;
        if (passersInside < 0) passersInside = 0;

        SetOpenVisual(true);
    }

    // CHANGED: Close only after unit actually reaches exit, and only when last passer is done
    public void NotifyUnitReachedExit(Transform unit)
    {
        if (unit == null) return;

        int id = unit.GetInstanceID();

        GatePath path;
        if (!activePaths.TryGetValue(id, out path))
            return;

        if (path.exited)
            return;

        path.exited = true;

        if (path.entered)
            passersInside--;

        if (passersInside < 0) passersInside = 0;

        activePaths.Remove(id);

        if (passersInside == 0)
            SetOpenVisual(false);
    }

    // CHANGED: If unit disappears mid-pass, cleanup counts and maybe close
    public void CancelUnit(Transform unit)
    {
        if (unit == null) return;

        int id = unit.GetInstanceID();

        GatePath path;
        if (!activePaths.TryGetValue(id, out path))
            return;

        if (path.entered && !path.exited)
            passersInside--;

        if (passersInside < 0) passersInside = 0;

        activePaths.Remove(id);

        //if (passersInside == 0)
        //    SetOpenVisual(false);
    }

    public float DistanceToClosestEntry(Vector3 fromPosition)
    {
        Transform e = PickClosestEntry(fromPosition);
        if (e == null) return float.PositiveInfinity;
        return Vector3.Distance(fromPosition, e.position);
    }

    private Transform PickEntryFor(Vector3 fromPosition)
    {
        List<Transform> valid = new List<Transform>();

        if (entryPoints != null)
        {
            for (int i = 0; i < entryPoints.Length; i++)
                if (entryPoints[i] != null)
                    valid.Add(entryPoints[i]);
        }

        if (valid.Count == 0)
            return transform;

        valid.Sort((a, b) =>
        {
            float da = Vector3.Distance(fromPosition, a.position);
            float db = Vector3.Distance(fromPosition, b.position);
            return da.CompareTo(db);
        });

        int k = Mathf.Clamp(pickFromClosestCount, 1, valid.Count);
        int idx = Random.Range(0, k);
        return valid[idx];
    }

    private Transform PickClosestEntry(Vector3 fromPosition)
    {
        Transform best = null;
        float bestDist = float.PositiveInfinity;

        if (entryPoints == null) return null;

        for (int i = 0; i < entryPoints.Length; i++)
        {
            Transform t = entryPoints[i];
            if (t == null) continue;

            float d = Vector3.Distance(fromPosition, t.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = t;
            }
        }

        return best;
    }

    // CHANGED: Fixed random selection bug (no double Random.Range)
    private Transform PickExitFor(Transform chosenEntry)
    {
        if (exitPoints == null || exitPoints.Length == 0)
            return transform;

        // Optional pairing by index if lengths match
        if (chosenEntry != null && entryPoints != null && entryPoints.Length == exitPoints.Length)
        {
            for (int i = 0; i < entryPoints.Length; i++)
            {
                if (entryPoints[i] == chosenEntry && exitPoints[i] != null)
                    return exitPoints[i];
            }
        }

        int idx = Random.Range(0, exitPoints.Length);
        Transform t = exitPoints[idx];
        if (t != null) return t;

        // Fallback: find any non-null
        for (int i = 0; i < exitPoints.Length; i++)
            if (exitPoints[i] != null)
                return exitPoints[i];

        return transform;
    }

    private void SetOpenVisual(bool open)
    {
        if (gateAnimator == null) return;
        if (string.IsNullOrEmpty(isOpenBoolName)) return;
        gateAnimator.SetBool(isOpenBoolName, open);
    }
}
