using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class SeleccionarOpcion : MonoBehaviour
{
    public GameObject panelLogin;
    public GameObject panelRegistro_primero;
    public GameObject panelLogin_primero;
    public GameObject panelRegistro;
    public GameObject panelPrincipal;

    [Header("Configuraci�n de Error de Conexi�n")]
    public GameObject panelErrorConexion;
    public TextMeshProUGUI textoError;
    public string mensajeError = "Error de conexi�n. Por favor, verifica tu conexi�n a internet.";
    public float timeout = 3f;

    void Start()
    {
        panelLogin.SetActive(false);
        panelRegistro.SetActive(false);
        panelPrincipal.SetActive(true);

        if (panelErrorConexion != null)
        {
            panelErrorConexion.SetActive(false);
        }
    }

    public void ShowRegisterPanel()
    {
        StartCoroutine(VerificarConexion(() =>
        {
            panelLogin.SetActive(false);
            panelRegistro.SetActive(true);
            panelPrincipal.SetActive(false);
            panelRegistro_primero.SetActive(true);
            OcultarErrorConexion();
        }));
    }

    public void ShowLoginPanel()
    {
        StartCoroutine(VerificarConexion(() =>
        {
            panelRegistro.SetActive(false);
            panelLogin.SetActive(true);
            panelPrincipal.SetActive(false);
            panelLogin_primero.SetActive(true);
            OcultarErrorConexion();
        }));
    }

    private IEnumerator VerificarConexion(System.Action onSuccess)
    {
        using (UnityWebRequest request = UnityWebRequest.Get("https://clients3.google.com/generate_204"))

        {
            request.timeout = Mathf.RoundToInt(timeout);
            yield return request.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
            if (request.result == UnityWebRequest.Result.Success)
#else
            if (!request.isNetworkError && !request.isHttpError)
#endif
            {
                onSuccess?.Invoke();
            }
            else
            {
                MostrarErrorConexion();
            }
        }
    }

    private void MostrarErrorConexion()
    {
        if (panelErrorConexion != null && textoError != null)
        {
            textoError.text = mensajeError;
            panelErrorConexion.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Error de conexi�n: " + mensajeError);
        }
    }

    private void OcultarErrorConexion()
    {
        if (panelErrorConexion != null)
        {
            panelErrorConexion.SetActive(false);
        }
    }

    // Llama este m�todo desde el bot�n "Cerrar" del panel de error
    public void CerrarPanelError()
    {
        OcultarErrorConexion();
    }
}
