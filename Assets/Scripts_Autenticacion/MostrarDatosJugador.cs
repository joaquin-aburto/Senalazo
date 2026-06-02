using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class MostrarDatosJugador : MonoBehaviour
{
    public GameObject panel;                  
    public TextMeshProUGUI textoInfo;         
    public Button botonAbrir;                 
    public Button botonCerrar;                

    void Start()
    {
        // Asegura que el panel comience oculto
        panel.SetActive(false);

        // Asigna funciones a los botones
        if (botonAbrir != null)
            botonAbrir.onClick.AddListener(AbrirPanel);

        if (botonCerrar != null)
            botonCerrar.onClick.AddListener(CerrarPanel);
    }

   
    public void AbrirPanel()
    {
        ActualizarTexto();
        panel.SetActive(true);
    }

    // Cierra el panel
    public void CerrarPanel()
    {
        panel.SetActive(false);
    }

    // Actualiza los datos desde PlayerPrefs
    void ActualizarTexto()
    {
        string apodo = PlayerPrefs.GetString("Apodo", "Sin Apodo");
        string nombre = PlayerPrefs.GetString("Nombre", "Sin Nombre");
        int ganadas = PlayerPrefs.GetInt("PartidasGanadas", 0);    // Clave actualizada
        int puntuacion = PlayerPrefs.GetInt("Puntuacion", 0);
        int jugadas = PlayerPrefs.GetInt("PartidasJugadas", 0);     // Clave actualizada

        string textoFinal = $"Apodo: {apodo}\n" +
                            $"Nombre: {nombre}\n" +
                            $"Partidas Ganadas: {ganadas}\n" +
                            $"Puntuaci�n Maxima: {puntuacion}\n" +
                            $"Partidas Jugadas: {jugadas}";

        if (textoInfo != null)
            textoInfo.text = textoFinal;
    }
}
