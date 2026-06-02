using UnityEngine;

public class PanelController : MonoBehaviour
{
    public GameObject panelHistorial;

    public void MostrarPanel()
    {
        panelHistorial.SetActive(true);
    }

    public void OcultarPanel()
    {
        panelHistorial.SetActive(false);
    }
}
