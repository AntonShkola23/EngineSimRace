using UnityEngine;
using System;

public class AudioEngineLayers
{
    private readonly AudioEnginePhysics physics;
    private readonly System.Random random = new System.Random();

    private float phase1 = 0f;
    private float phase2 = 0f;
    private float lfoPhase = 0f;
    private float lfoRate = 0.8f;
    private float lfoRateTimer = 0f;
    private float randomPhase = 0f;

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

    private readonly float[] harmonicDetune = new float[16];
    private readonly float[] harmonicPhaseOffset = new float[16];

    public AudioEngineLayers(AudioEnginePhysics physics)
    {
        this.physics = physics;

        for (int i = 0; i < 16; i++)
        {
            harmonicDetune[i] = 1f + (UnityEngine.Random.value * 0.028f - 0.014f);
            harmonicPhaseOffset[i] = UnityEngine.Random.value * Mathf.PI * 2f;
        }
    }

    public float GetSample(
        bool enableMainHarmonics,
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
        if (physics.CurrentRPM < 400f) return 0f;

        lfoRateTimer += 1f / 44100f;
        if (lfoRateTimer > 0.8f)
        {
            lfoRateTimer = 0f;
            lfoRate = 0.8f + (float)random.NextDouble() * 1.2f - 0.6f;
        }
        lfoPhase += lfoRate * Mathf.PI * 2f / 44100f;
        float lfo = Mathf.Sin(lfoPhase) * 0.5f + 0.5f;

        randomPhase += 0.0008f;
        float randMod = Mathf.PerlinNoise(randomPhase * 1.3f, 0f) * 0.3f - 0.15f;

        float baseFreq = physics.FiringFrequency * (1f + lfo * 0.018f + randMod * 0.012f);

        phase1 += baseFreq * Mathf.PI * 2f / 44100f;
        phase2 += baseFreq * 1.122462f * Mathf.PI * 2f / 44100f;

        if (phase1 > Mathf.PI * 2f) phase1 -= Mathf.PI * 2f;
        if (phase2 > Mathf.PI * 2f) phase2 -= Mathf.PI * 2f;

        float sample = 0f;

        if (enableMainHarmonics)
        {
            float resonanceBoost = 1f + physics.ManifoldResonance * 0.6f;

            for (int i = 0; i < 16; i++)
            {
                float amp = 0.72f / (i + 1f) * (i < 8 ? 1.35f : 0.65f);
                float freq1 = baseFreq * (i + 1) * harmonicDetune[i];
                float freq2 = freq1 * 1.122462f;

                float ph1 = phase1 * (i + 1) + harmonicPhaseOffset[i];
                float ph2 = phase2 * (i + 1) + harmonicPhaseOffset[i] * 0.7f;

                sample += Mathf.Sin(ph1) * amp * 0.707f * resonanceBoost;
                sample += Mathf.Sin(ph2) * amp * 0.191f * resonanceBoost;
            }
            sample = (float)Math.Tanh(sample * 2.4f) * 0.82f;
        }

        if (enableAdditionalLayers)
        {
            if (enableLowBodyLayer) { /* LowBody — без изменений */ }
            if (enableMechanicalNoise) { /* Mechanical — без изменений */ }

            // WHITE NOISE — возвращён и усилен
            if (enableWhiteNoise)
            {
                whiteNoisePhase += 2840f * Mathf.PI * 2f / 44100f;

                whiteBreathPhase += 0.85f * Mathf.PI * 2f / 44100f;
                float breath = Mathf.PerlinNoise(whiteBreathPhase * 0.55f, 0f) * 0.75f + 0.65f;

                whitePitchPhase += 1.1f * Mathf.PI * 2f / 44100f;
                float pitchDrift = Mathf.PerlinNoise(whitePitchPhase * 0.35f, 0f) * 0.12f - 0.06f;

                float whiteNoise = ((float)random.NextDouble() * 2f - 1f) * 0.068f;

                float activation = Mathf.Clamp01((physics.CurrentRPM - 1800f) / 3200f);
                float airInfluence = physics.AirFlowVelocity * 0.85f;

                sample += whiteNoise * breath * activation * airInfluence * whiteNoiseVolume * additionalLayersMasterVolume;
            }

            if (enablePulseLayer)
            {
                // Intake Pulse — в 2 раза выше по частоте
                float targetIntake = physics.FiringFrequency * 0.45f;     // ×2
                smoothedIntakeFreq = Mathf.Lerp(smoothedIntakeFreq, targetIntake, 0.085f);

                intakePhase += smoothedIntakeFreq * Mathf.PI * 2f / 44100f;

                float targetEnv = physics.IntakePulseStrength > 0.05f ? 1f : 0f;
                intakeEnvelope = Mathf.MoveTowards(intakeEnvelope, targetEnv, 0.012f);

                float pulse = Mathf.Sin(intakePhase) * 0.55f + Mathf.Sin(intakePhase * 2.7f) * 0.25f;
                float intake = pulse * physics.IntakePulseStrength * 0.14f * intakeEnvelope * pulseVolume;
                sample += intake * additionalLayersMasterVolume;

                // Exhaust Pulse — в 2 раза выше по частоте
                float targetExhaust = physics.FiringFrequency * 0.225f;   // ×2
                smoothedExhaustFreq = Mathf.Lerp(smoothedExhaustFreq, targetExhaust, 0.09f);

                exhaustPhase += smoothedExhaustFreq * Mathf.PI * 2f / 44100f;

                targetEnv = physics.ExhaustPulseStrength > 0.05f ? 1f : 0f;
                exhaustEnvelope = Mathf.MoveTowards(exhaustEnvelope, targetEnv, 0.014f);

                pulse = Mathf.Sin(exhaustPhase) * 0.65f + Mathf.Sin(exhaustPhase * 1.9f) * 0.3f;
                float exhaust = pulse * physics.ExhaustPulseStrength * 0.16f * exhaustEnvelope * pulseVolume;
                sample += exhaust * additionalLayersMasterVolume;
            }
        }

        sample *= 0.92f;
        return sample;
    }
}