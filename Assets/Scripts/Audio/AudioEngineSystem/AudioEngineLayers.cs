using UnityEngine;
using System;

public class AudioEngineLayers
{
    private readonly AudioEnginePhysics physics;

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

    private readonly float[] harmonicDetune = new float[16];
    private readonly float[] harmonicPhaseOffset = new float[16];

    private readonly System.Random random = new System.Random();

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
        float additionalLayersMasterVolume)
    {
        if (physics.CurrentRPM < 400f) return 0f;

        // Общий LFO + Random (для синхронной модуляции)
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

        // 1. Main Harmonics
        if (enableMainHarmonics)
        {
            for (int i = 0; i < 16; i++)
            {
                float amp = 0.72f / (i + 1f) * (i < 8 ? 1.35f : 0.65f);
                float freq1 = baseFreq * (i + 1) * harmonicDetune[i];
                float freq2 = freq1 * 1.122462f;
                float ph1 = phase1 * (i + 1) + harmonicPhaseOffset[i];
                float ph2 = phase2 * (i + 1) + harmonicPhaseOffset[i] * 0.7f;

                sample += Mathf.Sin(ph1) * amp * 0.707f;
                sample += Mathf.Sin(ph2) * amp * 0.191f;
            }
            sample = (float)Math.Tanh(sample * 2.4f) * 0.82f;
        }

        // 2. LowBodyLayer (сильная синхронная модуляция + мастер-управление)
        if (enableAdditionalLayers && enableLowBodyLayer)
        {
            float lowShift = physics.FiringFrequency * 0.035f;
            lowBodyPhase1 += (90f + lowShift) * Mathf.PI * 2f / 44100f;
            lowBodyPhase2 += (180f + lowShift * 1.05f) * Mathf.PI * 2f / 44100f;
            lowBodyPhase3 += (280f + lowShift * 1.1f) * Mathf.PI * 2f / 44100f;

            float lowBody = Mathf.Sin(lowBodyPhase1) * 0.092f +
                             Mathf.Sin(lowBodyPhase2) * 0.068f +
                             Mathf.Sin(lowBodyPhase3) * 0.048f;

            float lowBodyGain = physics.CurrentLoad * (1.75f + physics.CurrentRPM / 5000f * 0.35f);
            // Сильная синхронная модуляция (чтобы не было статики)
            lowBodyGain *= (1f + lfo * 0.38f + randMod * 0.28f);

            sample += lowBody * lowBodyGain * lowBodyVolume * additionalLayersMasterVolume;
        }

        // 3. Mechanical Noise
        if (enableAdditionalLayers && enableMechanicalNoise)
        {
            sample += Mathf.PerlinNoise(phase1 * 28f, 0f) * 0.035f * physics.CurrentLoad
                      * mechanicalNoiseVolume * additionalLayersMasterVolume;
        }

        // 4. White Noise
        if (enableAdditionalLayers && enableWhiteNoise)
        {
            whiteNoisePhase += 2840f * Mathf.PI * 2f / 44100f;
            float whiteNoise = ((float)random.NextDouble() * 2f - 1f) * 0.042f;
            float whiteNoiseActivation = Mathf.Clamp01((physics.CurrentRPM - 1800f) / 3200f);
            sample += whiteNoise * whiteNoiseActivation * whiteNoiseVolume * additionalLayersMasterVolume;
        }

        sample *= 0.92f;
        return sample;
    }
}