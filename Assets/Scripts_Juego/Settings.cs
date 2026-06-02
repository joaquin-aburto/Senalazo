using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Settings : MonoBehaviour
{
    public GameObject panelOpciones;
    public GameObject panelSalir;
    private bool panelActivo = false;

    public void TogglePanel()
    {
        panelActivo = !panelActivo;
        panelOpciones.SetActive(panelActivo);
    }

    public void TogglePanelSalir()
    {
        panelOpciones.SetActive(false); 
        panelSalir.SetActive(true);
    }

    public void QuitarPanelSalir()
    {
        panelSalir.SetActive(false);
    }
}
