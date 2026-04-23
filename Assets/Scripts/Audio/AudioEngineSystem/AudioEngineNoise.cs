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
        float pulseVolume)
    {
        if (!enableAdditionalLayers || physics.CurrentRPM < 400f) return 0f;

        float sample = 0f;

        if (enableLowBodyLayer) { /* LowBody */ }
        if (enableMechanicalNoise) { /* Mechanical */ }

        // === WHITE NOISE — теперь только при нажатии throttle ===
        if (enableWhiteNoise)
        {
            whiteNoisePhase += 2840f * Mathf.PI * 2f / 44100f;

            whiteBreathPhase += 0.85f * Mathf.PI * 2f / 44100f;
            float breath = Mathf.PerlinNoise(whiteBreathPhase * 0.55f, 0f) * 0.75f + 0.65f;

            whitePitchPhase += 1.1f * Mathf.PI * 2f / 44100f;
            float pitchDrift = Mathf.PerlinNoise(whitePitchPhase * 0.35f, 0f) * 0.12f - 0.06f;

            float whiteNoise = ((float)random.NextDouble() * 2f - 1f) * 0.068f;

            // Активация ТОЛЬКО от throttle
            float activation = Mathf.Clamp01(physics.CurrentThrottle * 2.2f);   // плавно появляется при газе

            float airInfluence = physics.AirFlowVelocity * 0.85f;

            sample += whiteNoise * breath * activation * airInfluence * whiteNoiseVolume * additionalLayersMasterVolume;
        }

        if (enablePulseLayer)
        {
            /* Pulse — без изменений */
        }

        // Low Rumble (оставлен как был)
        float load = physics.CurrentLoad;
        rumblePhase += 68f * Mathf.PI * 2f / 44100f;
        float rumble = Mathf.PerlinNoise(rumblePhase * 0.45f, 0f) * 0.42f;
        sample += rumble * 0.9f * (0.7f + load * 0.9f);

        return sample;
    }
}