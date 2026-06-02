using UnityEngine;
using UnityEngine.UI;

public class ControladorVolumen1 : MonoBehaviour
{
    [Header("Referencias")]
    public Slider sliderVolumen;
    public string nombreObjetoAudio = "CartasAle";

    [Header("Configuraci�n")]
    [SerializeField] private float volumenNormal = 0.75f;
    [SerializeField] private bool buscarAudioSourceEnStart = true;

    private AudioSource _audioSource;
    private float _volumenPrevioHipoacusia;

    private void Start()
    {
        if (buscarAudioSourceEnStart)
            BuscarAudioSource();

        float volumenInicial = PlayerPrefs.GetFloat("volumenMusica", volumenNormal);

        sliderVolumen.value = volumenInicial;
        sliderVolumen.onValueChanged.AddListener(OnSliderValueChanged);

        AplicarVolumen(volumenInicial);
        _volumenPrevioHipoacusia = volumenInicial;
    }

    public void BuscarAudioSource()
    {
        GameObject obj = GameObject.Find(nombreObjetoAudio);
        if (obj == null)
        {
            Debug.LogError($"No se encontr� el GameObject con nombre: {nombreObjetoAudio}");
            return;
        }

        _audioSource = obj.GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            Debug.LogError($"El GameObject {nombreObjetoAudio} no tiene un componente AudioSource.");
        }
    }

    private void OnSliderValueChanged(float nuevoVolumen)
    {
        if (PlayerPrefs.GetInt("modoHipocausia", 0) == 1)
        {
            sliderVolumen.value = 0f; // Forzar slider a 0 en modo hipoacusia
            return;
        }

        _volumenPrevioHipoacusia = nuevoVolumen;
        PlayerPrefs.SetFloat("volumenMusica", nuevoVolumen);
        AplicarVolumen(nuevoVolumen);
    }

    private void AplicarVolumen(float volumen)
    {
        if (_audioSource != null)
        {
            float volumenAAplicar = (PlayerPrefs.GetInt("modoHipocausia", 0) == 1) ? 0f : volumen;
            _audioSource.volume = volumenAAplicar;

            // Actualizar slider visualmente sin disparar el evento
            if (PlayerPrefs.GetInt("modoHipocausia", 0) == 1)
            {
                sliderVolumen.value = 0f;
            }
        }
    }

    public void SetModoHipoacusia(bool activado)
    {
        PlayerPrefs.SetInt("modoHipocausia", activado ? 1 : 0);

        if (activado)
        {
            _volumenPrevioHipoacusia = sliderVolumen.value;
            AplicarVolumen(0f); // Silenciar completamente
            sliderVolumen.value = 0f; // Mover slider a 0
        }
        else
        {
            AplicarVolumen(_volumenPrevioHipoacusia);
            sliderVolumen.value = _volumenPrevioHipoacusia;
        }
    }

    private void OnDestroy()
    {
        if (sliderVolumen != null)
            sliderVolumen.onValueChanged.RemoveListener(OnSliderValueChanged);
    }
}