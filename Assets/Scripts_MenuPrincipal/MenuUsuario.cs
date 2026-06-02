using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuUsuario : MonoBehaviour
{
    public GameObject panelUsuario;
    public GameObject botonAbrirPanel; // Botón que aparece fuera del panel
    public GameObject botonCerrarPanel; // Botón que aparece dentro del panel
    public GameObject panelInformacion;

    // Llamado por el botón que abre el panel
    public void AbrirPanelUsuario()
    {
        Debug.Log("AbrirPanelUsuario(): Se presionó el botón para abrir el panel.");

        if (panelUsuario != null && botonAbrirPanel != null && botonCerrarPanel != null)
        {
            panelUsuario.SetActive(true);
            botonAbrirPanel.SetActive(false);
            botonCerrarPanel.SetActive(true);
            Debug.Log("Panel activado, botón de abrir oculto, botón de cerrar activado.");
        }
        else
        {
            Debug.LogWarning("Uno de los objetos (panelUsuario, botonAbrirPanel, botonCerrarPanel) no está asignado.");
        }
    }

    // Llamado por el botón dentro del panel para cerrarlo
    public void CerrarPanelUsuario()
    {
        Debug.Log("CerrarPanelUsuario(): Se presionó el botón para cerrar el panel.");

        if (panelUsuario != null && botonAbrirPanel != null && botonCerrarPanel != null)
        {
            panelUsuario.SetActive(false);
            botonAbrirPanel.SetActive(true);
            botonCerrarPanel.SetActive(false);
            Debug.Log("Panel ocultado, botón de abrir mostrado, botón de cerrar oculto.");
        }
        else
        {
            Debug.LogWarning("Uno de los objetos (panelUsuario, botonAbrirPanel, botonCerrarPanel) no está asignado.");
        }
    }

    public void cerrarSesion()
    {
        Debug.Log("cerrarSesion(): Cerrando sesión y borrando PlayerPrefs.");

        PlayerPrefs.DeleteKey("CognitoIdToken");
        PlayerPrefs.DeleteKey("AccessToken");
        PlayerPrefs.DeleteKey("RefreshToken");
        PlayerPrefs.DeleteKey("Apodo");

        PlayerPrefs.Save();

        Debug.Log("PlayerPrefs eliminados. Cargando escena 'MenuAutenticacion'.");
        SceneManager.LoadScene("MenuAutenticacion");
    }

    public void AbrirInformacion()
    {
        Debug.Log("AbrirInformacion(): Mostrando panel de información.");
        if (panelInformacion != null)
            panelInformacion.SetActive(true);
        else
            Debug.LogWarning("panelInformacion no está asignado.");
    }

    public void CerrarInformacion()
    {
        Debug.Log("CerrarInformacion(): Ocultando panel de información.");
        if (panelInformacion != null)
            panelInformacion.SetActive(false);
        else
            Debug.LogWarning("panelInformacion no está asignado.");
    }

    void Start()
    {
        Debug.Log("Start(): Inicializando paneles y botones.");
        if (panelUsuario != null) panelUsuario.SetActive(false);
        if (botonCerrarPanel != null) botonCerrarPanel.SetActive(false);
        if (botonAbrirPanel != null) botonAbrirPanel.SetActive(true);
    }

    void Update()
    {
        // Por ahora no hay lógica aquí, pero puedes agregarla si necesitas detectar algo cada frame.
    }
}
