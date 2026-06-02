using UnityEngine;
using UnityEngine.SceneManagement;

public class MusicaMenuController : MonoBehaviour
{
    private static MusicaMenuController instancia;
    private AudioSource audioSource;

    void Awake()
    {
        if (instancia == null)
        {
            instancia = this;
            DontDestroyOnLoad(gameObject);
            audioSource = GetComponent<AudioSource>();
            SceneManager.sceneLoaded += OnSceneLoaded;

            if (audioSource != null && !audioSource.isPlaying)
            {
                audioSource.Play();
                Debug.Log("M�sica iniciada en " + SceneManager.GetActiveScene().name);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("Escena cargada: " + scene.name);

        if (audioSource == null) return;

        if (scene.name == "Loteria")
        {
            if (audioSource.isPlaying)
            {
                audioSource.Pause();
                Debug.Log("Escena 'Loteria': m�sica pausada.");
            }
        }
        else
        {
            if (!audioSource.isPlaying)
            {
                audioSource.Play();
                Debug.Log("Escena " + scene.name + ": m�sica reanudada.");
            }
        }
    }
}
