using UnityEngine;
using VehiclePhysics;

[RequireComponent(typeof(AudioSource))]
public class AudioEngineSystem : MonoBehaviour
{
    [Header("Master")]
    [Tooltip("ESNS disabled while using REV. Code kept for later A/B.")]
    public bool enableEsns = false;

    [Header("Range Presets")]
    public EngineRangePreset idlePreset;
    public EngineRangePreset lowPreset;
    public EngineRangePreset medPreset;       

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

    private AudioEnginePhysics physics;
    private AudioEngineHarmonics harmonics;
    private AudioEngineNoise noise;
    private PresetBlender blender;
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;

        if (!enableEsns)
        {
            if (DebugLogs)
                Debug.Log("[AudioEngineSystem] ESNS disabled (enableEsns = false). Using REV or silence.");
            enabled = false;
            return;
        }

        physics = new AudioEnginePhysics();
        physics.InitializeDefault();        

        harmonics = new AudioEngineHarmonics(physics);
        noise = new AudioEngineNoise(physics);

        // Blender теперь поддерживает Med
        if (idlePreset != null && lowPreset != null && medPreset != null)
        {
            blender = new PresetBlender(idlePreset, lowPreset, medPreset);
            Debug.Log("[AudioEngineSystem] PresetBlender создан (Idle + Low + Med)");
        }
        else
        {
            Debug.LogWarning("[AudioEngineSystem] Не все пресеты назначены (Idle, Low, Med)");
        }
    }

    private void Update()
    {
        if (!enableEsns || physics == null) return;
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
        // OnAudioFilterRead still runs when script is disabled on some Unity versions —
        // hard guard so ESNS never mixes with REV on the same AudioSource.
        if (!enableEsns || harmonics == null || noise == null || blender == null) return;

        for (int i = 0; i < data.Length; i += channels)
        {
            float harm = harmonics.GetSample(MainHarmonics, blender);

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
                pulseVolume);

            float sample = harm + nois;

            for (int c = 0; c < channels; c++)
                data[i + c] = sample;
        }
    }
}