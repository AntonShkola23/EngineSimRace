using UnityEngine;
using VehiclePhysics;

[RequireComponent(typeof(AudioSource))]
public class AudioEngineSystem : MonoBehaviour
{
    [Header("Engine Preset")]
    public AudioEnginePresetSO currentPreset;

    [Header("Debug")]
    public bool DebugLogs = true;

    [Header("Layer Control")]
    public bool MainHarmonics = true;
    public bool AdditionalLayers = true;
    public bool LowBodyLayer = true;
    public bool MechanicalNoise = true;
    public bool WhiteNoise = true;
    public bool EnablePulseLayer = true;

    [Header("Layer Volumes")]
    [Range(0f, 2f)] public float LayersMaster = 1f;
    [Range(0f, 2f)] public float lowBody = 1f;
    [Range(0f, 2f)] public float mechanicalNoise = 1f;
    [Range(0f, 2f)] public float whiteNoise = 1f;

    [Header("Pulse Volume")]
    [Range(0f, 3f)] public float pulseVolume = 1.0f;

    [Header("Harmonics Distortion")]
    [Range(0f, 5f)] public float distortionAmount = 3.2f;

    [Header("Air Absorption")]
    [Range(0f, 2f)] public float airAbsorption = 0.85f;

    [Header("Clatter / Treщotka Control")]
    [Range(0f, 2f)] public float clatterVolume = 1.0f;
    [Range(0.5f, 4f)] public float clatterPitch = 1.8f;

    [Header("Low Body Dynamics")]
    [Range(0f, 2f)] public float lowBodyDynamics = 1.35f;     // ← новый ползунок

    private AudioEnginePhysics physics;
    private AudioEngineHarmonics harmonics;
    private AudioEngineNoise noise;
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;

        physics = new AudioEnginePhysics();
        if (currentPreset != null)
            physics.Initialize(currentPreset);
        else
            physics.InitializeDefault();

        harmonics = new AudioEngineHarmonics(physics);
        noise = new AudioEngineNoise(physics);
    }

    private void Update()
    {
        if (physics == null) return;
        VPVehicleController vpp = GetComponentInParent<VPVehicleController>();
        if (vpp == null) return;

        float rpm = vpp.data.Get(Channel.Vehicle, VehicleData.EngineRpm) / 1000f;
        float throttle = vpp.data.Get(Channel.Input, InputData.Throttle) / 10000f;
        float load = throttle;

        physics.Update(rpm, throttle, load);

        if (rpm > 400f && !audioSource.isPlaying)
            audioSource.Play();
        else if (rpm < 400f && audioSource.isPlaying)
            audioSource.Stop();
    }

    private void OnAudioFilterRead(float[] data, int channels)
    {
        if (harmonics == null || noise == null) return;

        for (int i = 0; i < data.Length; i += channels)
        {
            float harm = harmonics.GetSample(MainHarmonics, distortionAmount, airAbsorption, clatterVolume, clatterPitch);
            float nois = noise.GetSample(
                AdditionalLayers,
                LowBodyLayer,
                MechanicalNoise,
                WhiteNoise,
                lowBody,
                mechanicalNoise,
                whiteNoise,
                LayersMaster,
                EnablePulseLayer,
                pulseVolume,
                lowBodyDynamics);

            float sample = harm + nois;

            for (int c = 0; c < channels; c++)
                data[i + c] = sample;
        }
    }
}