using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ControladorVolumen : MonoBehaviour
{
    public static ControladorVolumen Instance;

    public Slider sliderVolumen;
    public string nombreObjetoMusica = "MusicaFondo";

    private AudioSource _audioSource;
    private float _volumenActual = 0.75f;
    private bool _modoHipocausia = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnNuevaEscena;

            // Cargar valores guardados
            _volumenActual = PlayerPrefs.GetFloat("volumenMusica", 0.75f);
            _modoHipocausia = PlayerPrefs.GetInt("modoHipocausia", 0) == 1;

            // Buscar y configurar AudioSource
            GameObject objMusica = GameObject.Find(nombreObjetoMusica);
            if (objMusica != null)
            {
                _audioSource = objMusica.GetComponent<AudioSource>();
                if (_audioSource != null)
                {
                    DontDestroyOnLoad(_audioSource.gameObject);
                    _audioSource.volume = _modoHipocausia ? 0f : _volumenActual;
                }
            }

            // Configurar slider y aplicar volumen
            BuscarYConfigurarSlider();
            AplicarVolumenActual();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnNuevaEscena(Scene scene, LoadSceneMode mode)
    {
        // Rebuscar audio por si se perdió en el cambio de escena
        if (_audioSource == null)
        {
            GameObject objMusica = GameObject.Find(nombreObjetoMusica);
            if (objMusica != null)
            {
                _audioSource = objMusica.GetComponent<AudioSource>();
            }
        }

        // Recargar valores desde PlayerPrefs
        _volumenActual = PlayerPrefs.GetFloat("volumenMusica", 0.75f);
        _modoHipocausia = PlayerPrefs.GetInt("modoHipocausia", 0) == 1;

        BuscarYConfigurarSlider();
        AplicarVolumenActual();
    }

    void BuscarYConfigurarSlider()
    {
        if (sliderVolumen == null)
        {
            Slider[] todosSliders = FindObjectsOfType<Slider>(true);
            foreach (Slider s in todosSliders)
            {
                if (s.name.ToLower().Contains("volumen") || s.name.ToLower().Contains("music"))
                {
                    sliderVolumen = s;
                    break;
                }
            }

            if (sliderVolumen == null && todosSliders.Length > 0)
            {
                sliderVolumen = todosSliders[0];
            }
        }

        if (sliderVolumen != null)
        {
            sliderVolumen.onValueChanged.RemoveAllListeners();
            sliderVolumen.value = _volumenActual;
            sliderVolumen.onValueChanged.AddListener(ActualizarVolumen);
            sliderVolumen.interactable = !_modoHipocausia;

            if (!sliderVolumen.gameObject.activeSelf)
            {
                sliderVolumen.gameObject.SetActive(true);
            }
        }
    }

    void ActualizarVolumen(float nuevoVolumen)
    {
        if (_modoHipocausia) return;

        _volumenActual = nuevoVolumen;

        if (_audioSource != null)
        {
            _audioSource.volume = _volumenActual;
        }

        PlayerPrefs.SetFloat("volumenMusica", _volumenActual);
        PlayerPrefs.Save();
    }

    void AplicarVolumenActual()
    {
        if (_audioSource != null)
        {
            _audioSource.volume = _modoHipocausia ? 0f : _volumenActual;
        }

        if (sliderVolumen != null)
        {
            sliderVolumen.interactable = !_modoHipocausia;
            sliderVolumen.SetValueWithoutNotify(_modoHipocausia ? 0f : _volumenActual);
        }

        Debug.Log("Aplicando volumen: " + _volumenActual + " | Modo hipocausia: " + _modoHipocausia);
    }

    void Update()
    {
        bool hipocausiaActual = PlayerPrefs.GetInt("modoHipocausia", 0) == 1;
        if (hipocausiaActual != _modoHipocausia)
        {
            _modoHipocausia = hipocausiaActual;
            ManejarModoHipocausia();
        }
    }

    void ManejarModoHipocausia()
    {
        if (_modoHipocausia)
        {
            PlayerPrefs.SetFloat("volumenPrevioHipocausia", _volumenActual);

            if (_audioSource != null) _audioSource.volume = 0f;
            if (sliderVolumen != null)
            {
                sliderVolumen.interactable = false;
                sliderVolumen.SetValueWithoutNotify(0f);
            }
        }
        else
        {
            _volumenActual = PlayerPrefs.GetFloat("volumenPrevioHipocausia", _volumenActual);

            if (sliderVolumen != null)
            {
                sliderVolumen.interactable = true;
                sliderVolumen.SetValueWithoutNotify(_volumenActual);
            }

            if (_audioSource != null)
            {
                _audioSource.volume = _volumenActual;
            }

            PlayerPrefs.SetFloat("volumenMusica", _volumenActual);
            PlayerPrefs.Save();
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnNuevaEscena;
            if (sliderVolumen != null)
            {
                sliderVolumen.onValueChanged.RemoveListener(ActualizarVolumen);
            }
        }
    }
}
