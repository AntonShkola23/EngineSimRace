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

    /// <summary>
    /// НОВАЯ СИГНАТУРА — без lowBodyDynamics
    /// </summary>
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

        // WHITE NOISE (постоянный + от газа)
        if (enableWhiteNoise)
        {
            whiteNoisePhase += 2840f * Mathf.PI * 2f / 44100f;

            whiteBreathPhase += 0.85f * Mathf.PI * 2f / 44100f;
            float breath = Mathf.PerlinNoise(whiteBreathPhase * 0.55f, 0f) * 0.75f + 0.65f;

            whitePitchPhase += 1.1f * Mathf.PI * 2f / 44100f;
            float pitchDrift = Mathf.PerlinNoise(whitePitchPhase * 0.35f, 0f) * 0.12f - 0.06f;

            float whiteNoise = ((float)random.NextDouble() * 2f - 1f) * 0.068f;

            float baseLevel = 0.45f;
            float throttleBoost = physics.CurrentThrottle * 1.8f;
            float activation = baseLevel + throttleBoost;
            float airInfluence = physics.AirFlowVelocity * 0.85f;

            sample += whiteNoise * breath * activation * airInfluence * whiteNoiseVolume * additionalLayersMasterVolume;
        }

        // Pulse Layer
        if (enablePulseLayer)
        {
            // Intake Pulse
            float targetIntake = physics.FiringFrequency * 0.45f;
            smoothedIntakeFreq = Mathf.Lerp(smoothedIntakeFreq, targetIntake, 0.085f);
            intakePhase += smoothedIntakeFreq * Mathf.PI * 2f / 44100f;

            float targetEnv = physics.IntakePulseStrength > 0.05f ? 1f : 0f;
            intakeEnvelope = Mathf.MoveTowards(intakeEnvelope, targetEnv, 0.012f);

            float pulse = Mathf.Sin(intakePhase) * 0.55f + Mathf.Sin(intakePhase * 2.7f) * 0.25f;
            float intake = pulse * physics.IntakePulseStrength * 0.14f * intakeEnvelope * pulseVolume;
            sample += intake * additionalLayersMasterVolume;

            // Exhaust Pulse
            float targetExhaust = physics.FiringFrequency * 0.225f;
            smoothedExhaustFreq = Mathf.Lerp(smoothedExhaustFreq, targetExhaust, 0.09f);
            exhaustPhase += smoothedExhaustFreq * Mathf.PI * 2f / 44100f;

            targetEnv = physics.ExhaustPulseStrength > 0.05f ? 1f : 0f;
            exhaustEnvelope = Mathf.MoveTowards(exhaustEnvelope, targetEnv, 0.014f);

            pulse = Mathf.Sin(exhaustPhase) * 0.65f + Mathf.Sin(exhaustPhase * 1.9f) * 0.3f;
            float exhaust = pulse * physics.ExhaustPulseStrength * 0.16f * exhaustEnvelope * pulseVolume;
            sample += exhaust * additionalLayersMasterVolume;
        }

        // Low Rumble
        float load = physics.CurrentLoad;
        rumblePhase += 68f * Mathf.PI * 2f / 44100f;
        float rumble = Mathf.PerlinNoise(rumblePhase * 0.45f, 0f) * 0.42f;
        sample += rumble * 0.9f * (0.7f + load * 0.9f);

        // Low Body Dynamics
        if (enableLowBodyLayer)
        {
            float baseLowFreq = physics.FiringFrequency * 0.47f;

            lowBodyLfoPhase += 2.1f * Mathf.PI * 2f / 44100f;
            float lfo = Mathf.Sin(lowBodyLfoPhase) * 0.5f + 0.5f;

            float throttleReaction = physics.CurrentThrottle * 1.35f * 2.4f;   // lowBodyDynamics теперь внутри (можно потом вынести)

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