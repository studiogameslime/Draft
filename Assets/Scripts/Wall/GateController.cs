using System.Collections.Generic;
using UnityEngine;

public class GateController : MonoBehaviour
{
    [Header("Points")]
    [SerializeField] private Transform[] entryPoints;
    [SerializeField] private Transform[] exitPoints;

    [Header("Entry Pick")]
    [SerializeField] private int pickFromClosestCount = 2; // CHANGED: random among closest K entries

    [Header("Visual optional")]
    [SerializeField] private Animator gateAnimator;
    [SerializeField] private string openBoolName = "Open";

    private int passers = 0;

    private class GatePath
    {
        public Transform entry;
        public Transform exit;
    }

    private readonly Dictionary<int, GatePath> activePaths = new Dictionary<int, GatePath>();

    private void Awake()
    {
        GateRegistry.Register(this);
        SetOpenVisual(false);
    }

    private void OnDestroy()
    {
        GateRegistry.Unregister(this);
    }

    public float GetDistanceToClosestEntry(Vector3 fromPosition)
    {
        Transform e = GetEntryForUnit(fromPosition); // CHANGED
        if (e == null) return float.PositiveInfinity;
        return Vector3.Distance(fromPosition, e.position);
    }

    public void BeginPassing(Transform unit, out Transform entry, out Transform exit)
    {
        entry = null;
        exit = null;

        if (unit == null)
            return;

        int id = unit.GetInstanceID();

        GatePath path;
        if (activePaths.TryGetValue(id, out path))
        {
            entry = path.entry;
            exit = path.exit;
            return;
        }

        path = new GatePath();

        // CHANGED: use smarter entry pick (random among closest K)
        path.entry = GetEntryForUnit(unit.position);
        path.exit = GetBestExitForEntry(path.entry);

        activePaths[id] = path;

        passers++;
        if (passers < 0) passers = 0;
        SetOpenVisual(true);

        entry = path.entry;
        exit = path.exit;
    }

    public void EndPassing(Transform unit)
    {
        if (unit == null)
            return;

        int id = unit.GetInstanceID();

        if (activePaths.ContainsKey(id))
            activePaths.Remove(id);

        passers--;
        if (passers < 0) passers = 0;

        if (passers == 0)
            SetOpenVisual(false);
    }

    // CHANGED
    // Returns a random entry among the closest K entries to the unit.
    private Transform GetEntryForUnit(Vector3 fromPosition)
    {
        if (entryPoints == null || entryPoints.Length == 0)
            return null;

        List<Transform> valid = new List<Transform>();
        for (int i = 0; i < entryPoints.Length; i++)
        {
            if (entryPoints[i] != null)
                valid.Add(entryPoints[i]);
        }

        if (valid.Count == 0)
            return null;

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

    private Transform GetBestExitForEntry(Transform chosenEntry)
    {
        if (exitPoints == null || exitPoints.Length == 0)
            return null;

        if (chosenEntry != null && entryPoints != null && entryPoints.Length == exitPoints.Length)
        {
            for (int i = 0; i < entryPoints.Length; i++)
            {
                if (entryPoints[i] == chosenEntry)
                    return exitPoints[i];
            }
        }

        Transform best = null;
        float bestDist = float.PositiveInfinity;

        Vector3 refPos = (chosenEntry != null) ? chosenEntry.position : transform.position;

        for (int i = 0; i < exitPoints.Length; i++)
        {
            Transform t = exitPoints[i];
            if (t == null) continue;

            float d = Vector3.Distance(refPos, t.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = t;
            }
        }

        return best;
    }

    private void SetOpenVisual(bool open)
    {
        if (gateAnimator != null && !string.IsNullOrEmpty(openBoolName))
            gateAnimator.SetBool(openBoolName, open);
    }
}
