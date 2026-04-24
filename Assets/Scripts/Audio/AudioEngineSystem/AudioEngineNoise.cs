using UnityEngine;
using System;

public class AudioEngineNoise
{
    private readonly AudioEnginePhysics physics;
    private readonly System.Random random = new System.Random();

    private float lowBodyPhase1 = 0f;
    private float lowBodyPhase2 = 0f;
    private float lowBodyPhase3 = 0f;
    private float whiteNoisePhase = 0f;
    private float whiteBreathPhase = 0f;
    private float whitePitchPhase = 0f;

    private float intakePhase = 0f;
    private float exhaustPhase = 0f;
    private float smoothedIntakeFreq = 0f;
    private float smoothedExhaustFreq = 0f;

    private float intakeEnvelope = 0f;
    private float exhaustEnvelope = 0f;

    private float rumblePhase = 0f;
    private float lowBodyLfoPhase = 0f;

    public AudioEngineNoise(AudioEnginePhysics physics)
    {
        this.physics = physics;
    }

    public float GetSample(
        bool enableAdditionalLayers,
        bool enableLowBodyLayer,
        bool enableMechanicalNoise,
        bool enableWhiteNoise,
        float lowBodyVolume,
        float mechanicalNoiseVolume,
        float whiteNoiseVolume,
        float additionalLayersMasterVolume,
        bool enablePulseLayer,
        float pulseVolume,
        float lowBodyDynamics)
    {
        if (!enableAdditionalLayers || physics.CurrentRPM < 400f) return 0f;

        float sample = 0f;

        if (enableMechanicalNoise) { /* Mechanical */ }

        // === WHITE NOISE — возвращён в постоянное состояние ===
        if (enableWhiteNoise)
        {
            whiteNoisePhase += 2840f * Mathf.PI * 2f / 44100f;

            whiteBreathPhase += 0.85f * Mathf.PI * 2f / 44100f;
            float breath = Mathf.PerlinNoise(whiteBreathPhase * 0.55f, 0f) * 0.75f + 0.65f;

            whitePitchPhase += 1.1f * Mathf.PI * 2f / 44100f;
            float pitchDrift = Mathf.PerlinNoise(whitePitchPhase * 0.35f, 0f) * 0.12f - 0.06f;

            float whiteNoise = ((float)random.NextDouble() * 2f - 1f) * 0.068f;

            // Постоянная база + усиление от газа
            float baseLevel = 0.45f;                                    // всегда присутствует
            float throttleBoost = physics.CurrentThrottle * 1.8f;      // сильно усиливается при газе

            float activation = baseLevel + throttleBoost;

            float airInfluence = physics.AirFlowVelocity * 0.85f;

            sample += whiteNoise * breath * activation * airInfluence * whiteNoiseVolume * additionalLayersMasterVolume;
        }

        if (enablePulseLayer)
        {
            /* Pulse — без изменений */
        }

        // Low Rumble
        float load = physics.CurrentLoad;
        rumblePhase += 68f * Mathf.PI * 2f / 44100f;
        float rumble = Mathf.PerlinNoise(rumblePhase * 0.45f, 0f) * 0.42f;
        sample += rumble * 0.9f * (0.7f + load * 0.9f);

        // Low Body Dynamics (оставлен как в предыдущей версии)
        if (enableLowBodyLayer)
        {
            float baseLowFreq = physics.FiringFrequency * 0.47f;

            lowBodyLfoPhase += 2.1f * Mathf.PI * 2f / 44100f;
            float lfo = Mathf.Sin(lowBodyLfoPhase) * 0.5f + 0.5f;

            float throttleReaction = physics.CurrentThrottle * lowBodyDynamics * 2.4f;

            lowBodyPhase1 += (baseLowFreq + lfo * 5f) * Mathf.PI * 2f / 44100f;
            lowBodyPhase2 += (baseLowFreq * 1.97f + lfo * 7f) * Mathf.PI * 2f / 44100f;
            lowBodyPhase3 += (baseLowFreq * 0.52f) * Mathf.PI * 2f / 44100f;

            float lb = Mathf.Sin(lowBodyPhase1) * 0.75f +
                       Mathf.Sin(lowBodyPhase2) * 0.44f +
                       Mathf.Sin(lowBodyPhase3) * 0.31f;

            float dynamics = 0.4f + throttleReaction + load * 1.1f;

            sample += lb * lowBodyVolume * dynamics * additionalLayersMasterVolume;
        }

        return sample;
    }
}