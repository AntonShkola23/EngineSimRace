using UnityEngine;

/// <summary>
/// AudioEnginePresetSO — Preset for a specific engine.
/// Drag this asset into AudioEngineSystem to select the engine.
/// </summary>
[CreateAssetMenu(menuName = "Audio Engine/Preset", fileName = "New Engine Preset")]
public class AudioEngineStage : ScriptableObject
{
    [Header("Basic Engine Information")]
    public string EngineName = "VR38DETT";
    public string EngineCode = "VR38DETT";

    public int CylinderCount = 6;
    public float DisplacementLiters = 3.8f;
    public bool IsTurbocharged = true;

    [Header("Tuning Stage")]
    [Tooltip("0 = Stock, 1 = Stage 1, 2 = Stage 2, etc.")]
    public int DefaultEngineStage = 0;

    [Header("Sound Character")]
    [Range(0f, 1f)] public float ExhaustAggressiveness = 0.75f;
    [Range(0f, 1f)] public float MechanicalHarshness = 0.65f;
    [Range(0f, 1f)] public float ManifoldResonanceStrength = 0.8f;

    [Header("Default Volume Settings")]
    [Range(0.5f, 2f)] public float BaseVolume = 1.0f;
    [Range(0f, 1f)] public float IdleVolumeMultiplier = 0.6f;
    [Range(0f, 1f)] public float FullLoadVolumeMultiplier = 1.35f;
}