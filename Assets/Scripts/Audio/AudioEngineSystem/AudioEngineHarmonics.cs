using UnityEngine;
using System;

public class AudioEngineHarmonics
{
    private readonly AudioEnginePhysics physics;

    private float phase1 = 0f;
    private float phase2 = 0f;
    private float modulatorPhase = 0f;
    private float lfoPhase = 0f;
    private float lfoRate = 0.8f;
    private float lfoRateTimer = 0f;
    private float randomPhase = 0f;

    private readonly float[] harmonicDetune = new float[16];
    private readonly float[] harmonicPhaseOffset = new float[16];

    private readonly System.Random _rng = new System.Random();

    public AudioEngineHarmonics(AudioEnginePhysics physics)
    {
        this.physics = physics;
        for (int i = 0; i < 16; i++)
        {
            harmonicDetune[i] = 1f + (UnityEngine.Random.value * 0.028f - 0.014f);
            harmonicPhaseOffset[i] = UnityEngine.Random.value * Mathf.PI * 2f;
        }
    }

    public float GetSample(bool enableMainHarmonics, PresetBlender blender)
    {
        if (!enableMainHarmonics || physics.CurrentRPM < 400f) return 0f;

        float rpm = physics.CurrentRPM;

        // === Параметры с тройным blend (Idle → Low → Med) ===
        float fmAmount = blender.LerpParam(blender.idlePreset.fmAmount, blender.lowPreset.fmAmount, blender.medPreset.fmAmount, rpm);
        float fmRatio = blender.LerpParam(blender.idlePreset.fmRatio, blender.lowPreset.fmRatio, blender.medPreset.fmRatio, rpm);
        float fmFeedback = blender.LerpParam(blender.idlePreset.fmFeedback, blender.lowPreset.fmFeedback, blender.medPreset.fmFeedback, rpm);
        float fmNoiseAmount = blender.LerpParam(blender.idlePreset.fmNoiseAmount, blender.lowPreset.fmNoiseAmount, blender.medPreset.fmNoiseAmount, rpm);
        float fmEnvelopeSpeed = blender.LerpParam(blender.idlePreset.fmEnvelopeSpeed, blender.lowPreset.fmEnvelopeSpeed, blender.medPreset.fmEnvelopeSpeed, rpm);

        float distortionAmount = blender.LerpParam(blender.idlePreset.distortionAmount, blender.lowPreset.distortionAmount, blender.medPreset.distortionAmount, rpm);
        float airAbsorption = blender.LerpParam(blender.idlePreset.airAbsorption, blender.lowPreset.airAbsorption, blender.medPreset.airAbsorption, rpm);
        float clatterVolume = blender.LerpParam(blender.idlePreset.clatterVolume, blender.lowPreset.clatterVolume, blender.medPreset.clatterVolume, rpm);
        float clatterPitch = blender.LerpParam(blender.idlePreset.clatterPitch, blender.lowPreset.clatterPitch, blender.medPreset.clatterPitch, rpm);
        float lowBodyDynamics = blender.LerpParam(blender.idlePreset.lowBodyDynamics, blender.lowPreset.lowBodyDynamics, blender.medPreset.lowBodyDynamics, rpm);

        // === Vital-подобная модуляция ===
        lfoRateTimer += 1f / 44100f;
        if (lfoRateTimer > 0.8f)
        {
            lfoRateTimer = 0f;
            lfoRate = 0.8f + (float)_rng.NextDouble() * 1.2f - 0.6f;
        }

        lfoPhase += lfoRate * Mathf.PI * 2f / 44100f;
        randomPhase += 0.0008f;
        float randMod = Mathf.PerlinNoise(randomPhase * 1.3f, 0f) * 0.3f - 0.15f;
        float lfo = Mathf.Sin(lfoPhase) * 0.018f + randMod * 0.012f;

        float baseFreq = physics.FiringFrequency * (1f + lfo);

        // === FM Модуляция ===
        float fmMod = fmAmount * (1f + physics.CurrentThrottle * 1.45f);
        modulatorPhase += baseFreq * fmRatio * Mathf.PI * 2f / 44100f;
        float fmOffset = Mathf.Sin(modulatorPhase) * fmMod;

        phase1 += (baseFreq + fmOffset) * Mathf.PI * 2f / 44100f;
        phase2 += (baseFreq + fmOffset) * 1.122462f * Mathf.PI * 2f / 44100f;

        if (phase1 > Mathf.PI * 2f) phase1 -= Mathf.PI * 2f;
        if (phase2 > Mathf.PI * 2f) phase2 -= Mathf.PI * 2f;

        float sample = 0f;
        float resonanceBoost = 1f + physics.ManifoldResonance * 0.6f;

        for (int i = 0; i < 16; i++)
        {
            float harmIndex = i + 1;
            float amp = 0.72f / harmIndex * (harmIndex < 8 ? 1.35f : 0.65f);

            float freq1 = baseFreq * harmIndex * harmonicDetune[i];
            float freq2 = freq1 * 1.122462f;

            float ph1 = phase1 * harmIndex + harmonicPhaseOffset[i];
            float ph2 = phase2 * harmIndex + harmonicPhaseOffset[i] * 0.7f;

            float harmonic = Mathf.Sin(ph1) * 0.707f + Mathf.Sin(ph2) * 0.191f;

            float distortion = distortionAmount * (1f + physics.CurrentLoad * 0.8f);
            if (harmIndex <= 5) distortion *= 1.3f;
            if (harmIndex >= 11) distortion *= 0.75f;

            harmonic = (float)Math.Tanh(harmonic * distortion) * 0.89f;

            float freqFactor = harmIndex / 16f;
            float rpmFactor = rpm / 7800f;
            float highLoss = Mathf.Pow(1f - freqFactor, airAbsorption * (0.6f + rpmFactor * 1.1f));
            harmonic *= highLoss;

            sample += harmonic * amp * resonanceBoost;
        }

        // Clatter
        if (clatterVolume > 0.01f)
        {
            float clatterPhase = phase1 * clatterPitch * 4.2f;
            float clatter = Mathf.PerlinNoise(clatterPhase, 0f) * 0.45f;
            sample += clatter * clatterVolume * 0.6f;
        }

        return sample;
    }
}