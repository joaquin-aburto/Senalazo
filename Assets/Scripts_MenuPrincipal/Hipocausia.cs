using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HipocausiaBoton : MonoBehaviour
{
    public Button botonHipocausia;   // Asignar desde el Inspector
    public TextMeshProUGUI textoBoton;          // Asignar el componente Text del botón

    private const string claveEstado = "modoHipocausia";

    void Start()
    {
        // Cargar el estado guardado y actualizar el botón
        ActualizarTexto();

        // Asociar la función al evento del botón
        if (botonHipocausia != null)
            botonHipocausia.onClick.AddListener(ToggleHipocausia);
        else
            Debug.LogWarning("[HipocausiaBoton] No se asignó el botón");
    }

    void ToggleHipocausia()
    {
        bool nuevoEstado = !EstaActivo();
        PlayerPrefs.SetInt(claveEstado, nuevoEstado ? 1 : 0);
        PlayerPrefs.Save();
        Debug.Log("[HipocausiaBoton] Modo hipocausia: " + (nuevoEstado ? "Activado" : "Desactivado"));
        ActualizarTexto();
    }

    bool EstaActivo()
    {
        return PlayerPrefs.GetInt(claveEstado, 0) == 1;
    }

    void ActualizarTexto()
    {
        if (textoBoton != null)
            textoBoton.text = EstaActivo() ? "Sí" : "No";
    }
}
