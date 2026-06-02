using UnityEngine;
using UnityEngine.UI;

public class ControladorVolumenSFX_Menu : MonoBehaviour
{
    public Slider sliderVolumenSFX;
    private int hipocausiaAnterior = -1;

    void Awake()
    {
        // Configuramos el listener una sola vez
        if (sliderVolumenSFX != null)
        {
            sliderVolumenSFX.onValueChanged.RemoveAllListeners();
            sliderVolumenSFX.onValueChanged.AddListener(ActualizarVolumen);
        }
    }

    void Start()
    {
        // Inicializamos el slider al comenzar
        ActualizarSlider();
    }

    void OnEnable()
    {
        // Actualización forzada cada vez que se activa el objeto
        ActualizarSlider();
    }

    void Update()
    {
        // Verificamos cambios en hipoacusia
        int hipocausiaActual = PlayerPrefs.GetInt("modoHipocausia", 0);
        if (hipocausiaActual != hipocausiaAnterior)
        {
            hipocausiaAnterior = hipocausiaActual;
            ActualizarSlider();
        }
    }

    private void ActualizarSlider()
    {
        if (sliderVolumenSFX == null) return;

        float volumenActual;
        bool hipocausiaActiva = PlayerPrefs.GetInt("modoHipocausia", 0) == 1;

        if (hipocausiaActiva)
        {
            volumenActual = 0f;
            sliderVolumenSFX.interactable = false;
        }
        else
        {
            volumenActual = PlayerPrefs.GetFloat("volumenSFX", 1f);
            sliderVolumenSFX.interactable = true;
        }

        // Usamos SetValueWithoutNotify para evitar triggerear el listener
        sliderVolumenSFX.SetValueWithoutNotify(volumenActual);
    }

    void ActualizarVolumen(float nuevoVolumen)
    {
        // Si está en modo hipoacusia, ignoramos cambios
        if (PlayerPrefs.GetInt("modoHipocausia", 0) == 1) return;

        // Actualizamos directamente en PlayerPrefs
        PlayerPrefs.SetFloat("volumenSFX", nuevoVolumen);
        PlayerPrefs.Save();

        Debug.Log($"Volumen SFX actualizado a: {nuevoVolumen}");
    }

    void OnDestroy()
    {
        // Limpieza del listener
        if (sliderVolumenSFX != null)
        {
            sliderVolumenSFX.onValueChanged.RemoveListener(ActualizarVolumen);
        }
    }
}