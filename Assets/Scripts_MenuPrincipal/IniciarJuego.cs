using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // Asegúrate de incluir este namespace para usar TextMeshPro

public class IniciarJuego : MonoBehaviour
{
    public GameObject PanelEscoger;
    public GameObject PanelPrincipal;
    public TextMeshProUGUI apodoText; // Referencia al TextMeshPro para mostrar el apodo

    // Start is called before the first frame update
    void Start()
    {
        // Obtener el apodo guardado en PlayerPrefs
        string apodo = PlayerPrefs.GetString("Apodo", "");

        // Mostrar el apodo en el TextMeshPro
        if (!string.IsNullOrEmpty(apodo))
        {
            apodoText.text = "Bienvenido, " + apodo + "!";
        }
        else
        {
            apodoText.text = "Bienvenido, Jugador!";
        }
    }

    // Update is called once per frame
    void Update()
    {
        // No se necesita lógica en Update para este caso
    }

    public void EscogeCarta()
    {
        PanelEscoger.SetActive(true);
        PanelPrincipal.SetActive(false);
    }

    public void VolverMenu()
    {
        PanelEscoger.SetActive(false);
        PanelPrincipal.SetActive(true);
    }

    
}