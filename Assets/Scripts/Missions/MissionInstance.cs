using System;

[System.Serializable]
public class MissionInstance
{
    public MissionDefinition definition;
    public int currentProgress;
    public bool completed;
    public bool claimed;

    public Action OnProgressChanged;
    public Action OnClaimed;

    public MissionInstance(MissionDefinition def)
    {
        definition = def;
        currentProgress = 0;
        completed = false;
        claimed = false;
    }

    public void AddProgress(int amount)
    {
        if (completed)
            return;

        currentProgress += amount;

        if (currentProgress >= definition.targetAmount)
        {
            currentProgress = definition.targetAmount;
            completed = true;
        }

        OnProgressChanged?.Invoke();
    }
}
