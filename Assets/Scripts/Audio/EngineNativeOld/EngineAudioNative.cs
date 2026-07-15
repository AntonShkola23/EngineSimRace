using UnityEngine;
using VehiclePhysics;

[RequireComponent(typeof(AudioSource))] // атрибут - контроль наличия компонента audiosource
public class EngineAudioNative : MonoBehaviour
{
    [Header("Master")]
    [Tooltip("Old ESNS path disabled while using REV. Code kept for later A/B.")]
    public bool enableEsns = false;

    [Header("VPP Reference")] // добавления заголовка с описанием
    public VPVehicleController vehicleController;
    [Header("Engine Settings")]
    [Tooltip("Количество цилиндров (влияет на характер звука)")] // подсказка в всплывающем окне
    public int cylinders = 6;
    [Tooltip("Диапазон оборотов двигателя")]
    public float minRPM = 900f;
    public float maxRPM = 7000f;
    [Tooltip("Громкость на холостых и на полном газу")]
    [Range(0.3f, 1.8f)] public float idleVolume = 0.30f; // range превращает значение в ползунок
    [Range(0.3f, 1.8f)] public float fullThrottleVolume = 0.80f;

    [Header("Sound Layers")]
    [Tooltip("Включить/выключить основные гармоники (главный тон двигателя)")]
    public bool enableHarmonics = true;

    [Tooltip("Включить/выключить низкий \"грязный\" слой (басовый rumble)")]
    public bool enableDirtyLowLayer = true;

    [Tooltip("Включить/выключить механический шум (впуск, клапана, механика)")]
    public bool enableMechanicalNoise = true;

    [Header("LFO Modulation")]
    [Tooltip("Скорость колебаний LFO (работает всегда)")]
    [Range(0.2f, 12f)] public float lfoSpeed = 4.5f;

    [Tooltip("Глубина колебаний частоты (pitch wobble)")]
    [Range(0f, 0.04f)] public float lfoPitchDepth = 0.018f;

    [Tooltip("Глубина колебаний громкости (breathing effect)")]
    [Range(0f, 0.15f)] public float lfoVolumeDepth = 0.065f;

    [SerializeField]
    private AudioSource audioSource; // переменная для сохренения ссылки на компонент audiosource
    [Header("Parameter Value")]
    [SerializeField]
    private float currentRPM = 0f; // хранение RPM взятых из VPP
    [SerializeField]
    private float currentThrottle = 0f; // хранение текущего значения положения педали газа из VPP
    [SerializeField]
    private float phase = 0f; // хранение текущей фазы синуса
    [SerializeField]
    private float noisePhase = 0f; // хранение текущей фазы шума
    private float lfoPhase = 0f;   // LFO фаза для колебаний

    void Awake() // метод, который автоматически вызывается, когда объект появляется на сцене еще до start
    {
        audioSource = GetComponent<AudioSource>(); // ищем на game object компонент audiosource
        audioSource.loop = true; // делаем из audiosource loop
        audioSource.playOnAwake = false; // не запускаем звук при появлении объекта на сцене
        audioSource.spatialBlend = 1f; // 1f = полностью спозиционированный 3D звук

        if (!enableEsns)
        {
            enabled = false;
            return;
        }
    }

    void Start()
    {
        if (!enableEsns) return;

        if (vehicleController == null) // проверка не нулевое ли поле с контроллером
        {
            vehicleController = GetComponentInParent<VPVehicleController>(); // ищет компонент выше по иерархии (InParent)
        }
        if (vehicleController == null) // еще одна проверка наличия компонента
        {
            Debug.LogError("EngineAudioSim: VPVehicleController не найден!");
        }
    }

    void Update()
    {
        if (!enableEsns || vehicleController == null) return;
        currentRPM = vehicleController.data.Get(Channel.Vehicle, VehicleData.EngineRpm) / 1000f; // забираем RPM и делим на 1000
        currentThrottle = vehicleController.data.Get(Channel.Input, InputData.Throttle) / 10000f; // забираем throttle и делим на 10000
        bool engineRunning = currentRPM > 400f; // если RPM больше 400 = true
        if (engineRunning && !audioSource.isPlaying) // блок запуска и остановки двигателя
            audioSource.Play();
        else if (!engineRunning && audioSource.isPlaying)
            audioSource.Stop();
    }

    void OnAudioFilterRead(float[] data, int channels) // callback unity, который вызывается 44100 раз в секунду
                                                       // float[] data - пустой массив семплов, int channels количество каналов звука (стерео-моно)
    {
        if (!enableEsns)
            return;

        if (currentRPM < 400f) // если RPM ниже 400 буфер затирается нулями
        {
            for (int i = 0; i < data.Length; i++)
                data[i] = 0f;
            return;
        }
        float rpmFactor = Mathf.InverseLerp(minRPM, maxRPM, currentRPM); // превращает оборотов в число от 0 до 1
        float baseFrequency = Mathf.Lerp(32f, 320f, rpmFactor); // установка базовой гармоники: idle, max rpm
        float volume = Mathf.Lerp(idleVolume, fullThrottleVolume, currentThrottle); // плавно меняет громкость в зависимости throttle

        // LFO — работает всегда
        lfoPhase += lfoSpeed * Mathf.PI * 2f / 44100f;
        float lfo = Mathf.Sin(lfoPhase);

        // Применяем LFO к частоте и громкости
        baseFrequency *= (1f + lfo * lfoPitchDepth);
        volume *= (1f + lfo * lfoVolumeDepth);

        for (int i = 0; i < data.Length; i += channels) // цикл заполнения буфера аудио-данными
        {
            phase += baseFrequency * Mathf.PI * 2f / 44100f; // генератор синуса со смещением
            // чем выше baseFrequency, тем быстрее крутится phase, тем выше звук
            if (phase > Mathf.PI * 2f) phase -= Mathf.PI * 2f;
            // когда phase превышает полный круг — мы вычитаем его, чтобы phase не рос до бесконечности
            float sample = 0f; // сумма всех компонентов звука
                               // суммирование синусоид, аддитивный синтез

            if (enableHarmonics)
            {
                // 48 гармоник — верх сильно убран
                sample += Mathf.Sin(phase) * 0.72f; // 1
                sample += Mathf.Sin(phase * 1.99f) * 0.51f; // 2
                sample += Mathf.Sin(phase * 3.02f) * 0.38f; // 3
                sample += Mathf.Sin(phase * 4.01f) * 0.29f; // 4
                sample += Mathf.Sin(phase * 5.04f) * 0.22f; // 5
                sample += Mathf.Sin(phase * 6.08f) * 0.17f; // 6
                sample += Mathf.Sin(phase * 7.19f) * 0.13f; // 7
                sample += Mathf.Sin(phase * 8.45f) * 0.10f; // 8
                sample += Mathf.Sin(phase * 9.92f) * 0.078f;// 9
                sample += Mathf.Sin(phase * 11.6f) * 0.061f;// 10
                sample += Mathf.Sin(phase * 13.5f) * 0.048f;// 11
                sample += Mathf.Sin(phase * 15.7f) * 0.038f;// 12
                sample += Mathf.Sin(phase * 18.2f) * 0.030f;// 13
                sample += Mathf.Sin(phase * 21.0f) * 0.024f;// 14
                sample += Mathf.Sin(phase * 24.1f) * 0.019f;// 15
                sample += Mathf.Sin(phase * 27.6f) * 0.015f;// 16
                sample += Mathf.Sin(phase * 31.5f) * 0.012f;// 17
                sample += Mathf.Sin(phase * 35.8f) * 0.0095f;// 18
                sample += Mathf.Sin(phase * 40.6f) * 0.0075f;// 19
                sample += Mathf.Sin(phase * 45.9f) * 0.006f;// 20
                sample += Mathf.Sin(phase * 51.7f) * 0.0048f;// 21
                sample += Mathf.Sin(phase * 58.1f) * 0.0038f;// 22
                sample += Mathf.Sin(phase * 65.1f) * 0.003f;// 23
                sample += Mathf.Sin(phase * 72.8f) * 0.0024f;// 24
                sample += Mathf.Sin(phase * 81.2f) * 0.0019f;// 25
                sample += Mathf.Sin(phase * 90.4f) * 0.0015f;// 26
                sample += Mathf.Sin(phase * 100.5f) * 0.0012f;// 27
                sample += Mathf.Sin(phase * 111.5f) * 0.00095f;// 28
                sample += Mathf.Sin(phase * 123.5f) * 0.00075f;// 29
                sample += Mathf.Sin(phase * 136.6f) * 0.0006f;// 30
                sample += Mathf.Sin(phase * 150.8f) * 0.00048f;// 31
                sample += Mathf.Sin(phase * 166.2f) * 0.00038f;// 32
                sample += Mathf.Sin(phase * 183.0f) * 0.0003f;// 33
                sample += Mathf.Sin(phase * 201.3f) * 0.00024f;// 34
                sample += Mathf.Sin(phase * 221.2f) * 0.00019f;// 35
                sample += Mathf.Sin(phase * 242.9f) * 0.00015f;// 36
                sample += Mathf.Sin(phase * 266.5f) * 0.00012f;// 37
                sample += Mathf.Sin(phase * 292.1f) * 0.000095f;// 38
                sample += Mathf.Sin(phase * 320.0f) * 0.000075f;// 39
                sample += Mathf.Sin(phase * 350.3f) * 0.00006f;// 40
                sample += Mathf.Sin(phase * 383.2f) * 0.000048f;// 41
                sample += Mathf.Sin(phase * 418.9f) * 0.000038f;// 42
                sample += Mathf.Sin(phase * 457.7f) * 0.00003f;// 43
                sample += Mathf.Sin(phase * 500.0f) * 0.000024f;// 44
                sample += Mathf.Sin(phase * 545.9f) * 0.000019f;// 45
                sample += Mathf.Sin(phase * 595.8f) * 0.000015f;// 46
                sample += Mathf.Sin(phase * 650.0f) * 0.000012f;// 47
                sample += Mathf.Sin(phase * 708.7f) * 0.000009f;// 48
            }

            if (enableDirtyLowLayer)
            {
                // Дополнительный низкочастотный "грязный" слой (усилен и понижен)
                sample += Mathf.Sin(phase * 28f) * 0.022f * rpmFactor;
                sample += Mathf.Sin(phase * 34f) * 0.019f * rpmFactor;
            }

            if (enableMechanicalNoise)
            {
                // Механический шум — теперь привязан к RPM, а не к throttle
                noisePhase += 920f * Mathf.PI * 2f / 44100f;
                float noise = Mathf.PerlinNoise(noisePhase * 0.75f, 0f) * 0.22f;
                sample += noise * rpmFactor;   // теперь зависит от оборотов
            }

            // Применяем громкость
            sample *= volume;
            for (int c = 0; c < channels; c++)
            {
                data[i + c] = sample;
            }
        }
    }
}