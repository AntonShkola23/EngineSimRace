using UnityEngine;

[CreateAssetMenu(menuName = "Engine Audio/Engine Range Preset", fileName = "New Range Preset")]
public class EngineRangePreset : ScriptableObject
{
    [Header("Preset Info")]
    public string presetName = "Low Preset";

    [Header("FM Synthesis (èç Vital)")]
    [Range(0f, 8f)] public float fmAmount = 2.8f;
    [Range(0.5f, 5f)] public float fmRatio = 2.02f;
    [Range(0f, 3f)] public float fmFeedback = 0.0f;
    [Range(0f, 2f)] public float fmNoiseAmount = 0.0f;
    [Range(0.1f, 4f)] public float fmEnvelopeSpeed = 1.0f;

    [Header("Distortion & Waveshaping")]
    [Range(0f, 6f)] public float distortionAmount = 3.2f;

    [Header("Air Absorption / Highs Loss")]
    [Range(0f, 2.5f)] public float airAbsorption = 0.85f;

    [Header("Clatter / Treùotka")]
    [Range(0f, 3f)] public float clatterVolume = 1.0f;
    [Range(0.5f, 5f)] public float clatterPitch = 1.8f;

    [Header("Low Body Dynamics")]
    [Range(0f, 3f)] public float lowBodyDynamics = 1.35f;

    [Header("Extra Vital-like parameters")]
    [Range(0f, 2f)] public float resonanceBoost = 1.0f;
    [Range(0f, 1f)] public float unisonSpread = 0.028f;   // detune äëÿ unison
    [Range(0f, 1f)] public float pitchDriftAmount = 0.012f;
}