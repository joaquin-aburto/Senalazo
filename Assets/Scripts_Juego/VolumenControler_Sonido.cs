using UnityEngine;
using UnityEngine.UI;

public class ControladorVolumenSFX : MonoBehaviour
{
    public Slider sliderVolumenSFX;               
    public string nombreSfxGO = "CartasAle";      

    private AudioSource sfxSource;
    private bool sliderConfigurado = false;
    private int hipocausiaAnterior = -1;          
    void Awake()
    {
        Debug.Log("[ControladorVolumenSFX] Iniciando Awake");

        // Configurar AudioSource
        ConfigurarAudioSource();

        // Configurar slider si existe
        if (sliderVolumenSFX != null)
        {
            sliderVolumenSFX.onValueChanged.RemoveAllListeners();
            sliderVolumenSFX.onValueChanged.AddListener(CambiarVolumen);
            sliderConfigurado = true;
        }

        // Actualizar estado inicial
        ActualizarVolumenYSlider();
    }

    void OnEnable()
    {
        // Esto asegura que el slider se actualice cuando se reactiva el objeto
        if (sliderVolumenSFX != null && !sliderConfigurado)
        {
            sliderVolumenSFX.onValueChanged.RemoveAllListeners();
            sliderVolumenSFX.onValueChanged.AddListener(CambiarVolumen);
            sliderConfigurado = true;
        }

        // Actualizar estado al activarse
        ActualizarVolumenYSlider();
    }

    void Update()
    {
        // Verificar cambios en el modo hipoacusia
        int hipocausiaActual = PlayerPrefs.GetInt("modoHipocausia", 0);
        if (hipocausiaActual != hipocausiaAnterior)
        {
            hipocausiaAnterior = hipocausiaActual;
            ActualizarVolumenYSlider();
        }
    }

    void ConfigurarAudioSource()
    {
        GameObject obj = GameObject.Find(nombreSfxGO);
        if (obj != null)
        {
            AudioSource[] fuentes = obj.GetComponents<AudioSource>();
            if (fuentes.Length >= 2)
            {
                sfxSource = fuentes[1];  // El segundo AudioSource se asume que es el SFX
                Debug.Log("[ControladorVolumenSFX] AudioSource SFX encontrado");
            }
            else
            {
                Debug.LogWarning("[ControladorVolumenSFX] No se encontr� un segundo AudioSource en CartasAle");
            }
        }
        else
        {
            Debug.LogWarning($"[ControladorVolumenSFX] No se encontr� el GameObject '{nombreSfxGO}'");
        }
    }

    private void ActualizarVolumenYSlider()
    {
        bool hipocausiaActiva = PlayerPrefs.GetInt("modoHipocausia", 0) == 1;
        float volumenActual;

        if (hipocausiaActiva)
        {
            volumenActual = 0f;
            if (sliderVolumenSFX != null)
            {
                sliderVolumenSFX.SetValueWithoutNotify(volumenActual);
                sliderVolumenSFX.interactable = false;
            }
        }
        else
        {
            volumenActual = PlayerPrefs.GetFloat("volumenSFX", 1f);
            if (sliderVolumenSFX != null)
            {
                sliderVolumenSFX.SetValueWithoutNotify(volumenActual);
                sliderVolumenSFX.interactable = true;
            }
        }

        // Aplicar al AudioSource si existe
        if (sfxSource != null)
        {
            sfxSource.volume = volumenActual;
        }

        Debug.Log($"[ControladorVolumenSFX] Volumen SFX actualizado a: {volumenActual} (Hipoacusia: {hipocausiaActiva})");
    }

    void CambiarVolumen(float nuevoVolumen)
    {
        // Si est� en modo hipoacusia, ignoramos cambios
        if (PlayerPrefs.GetInt("modoHipocausia", 0) == 1) return;

        // Aplicar al AudioSource si existe
        if (sfxSource != null)
        {
            sfxSource.volume = nuevoVolumen;
        }

        // Guardar el nuevo valor
        PlayerPrefs.SetFloat("volumenSFX", nuevoVolumen);
        PlayerPrefs.Save();

        Debug.Log($"[ControladorVolumenSFX] Volumen SFX cambiado a: {nuevoVolumen}");
    }

    void OnDestroy()
    {
        // Limpiar el listener para evitar problemas
        if (sliderVolumenSFX != null)
        {
            sliderVolumenSFX.onValueChanged.RemoveListener(CambiarVolumen);
        }
    }
}