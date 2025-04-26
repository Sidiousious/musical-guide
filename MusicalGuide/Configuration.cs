using System;
using System.Collections.Generic;
using Dalamud.Configuration;

namespace MusicalGuide;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public bool Enabled { get; set; } = true;
    
    public bool UseAutomaticDistance { get; set; } = true;
    
    public bool UseFurtherCameraForLargerMounts { get; set; } = true;
    
    // Version for migrations
    public int Version { get; set; } = 1;

    public Dictionary<State, float> Distances = new();

    public void SetAutomatedDistance(State state, float distance)
    {
        if (!UseAutomaticDistance) return;
        SetManualDistance(state, distance);
    }

    public void SetManualDistance(State state, float distance)
    {
        Distances[state] = distance;
        Save();   
    }

    // the below exist just to make saving less cumbersome
    public void Save()
    {
        S.PluginInterface.SavePluginConfig(this);
    }
}
