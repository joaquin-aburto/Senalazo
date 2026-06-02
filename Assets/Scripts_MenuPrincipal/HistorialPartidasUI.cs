using UnityEngine;
using TMPro;
using System.Collections.Generic;
using Amazon;
using Amazon.CognitoIdentity;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Text;
using UnityEngine.UI;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.Runtime;

public class HistorialPartidasUI : MonoBehaviour
{
    [Header("Referencias UI")]
    public GameObject filaPartidaPrefab;
    public Transform contenidoTabla;
    public TextMeshProUGUI tituloHistorial;
    public GameObject loadingPanel;
    public TextMeshProUGUI errorText;
    public Button retryButton;

    [Header("Configuración")]
    public Color colorVictoria = Color.green;
    public Color colorDerrota = Color.red;
    public Color colorEmpate = Color.yellow;
    public Color colorEncabezado = new Color(0.4078f, 0.0784f, 0.0549f);
    public Color colorTextoEncabezado = Color.white;

    // Configuración AWS
    private const string USER_POOL_ID = "us-east-1_4u1fe6NJ8";
    private const string ID_POOL_ID = "us-east-1:e5ddddd4-07ab-4c8f-8c98-81a5d970a88a";
    private const string LAMBDA_FUNCTION = "ObtenerHistorialPartidas";
    private const string REGION = "us-east-1";
    private const string CLIENT_ID = "72kj748v0cn6cv8srgpqin9o5j";
    private const string USER_POOL_PROVIDER = "cognito-idp.us-east-1.amazonaws.com/us-east-1_4u1fe6NJ8";

    private AmazonCognitoIdentityProviderClient _providerClient;
    private List<PartidaInfo> partidasCargadas;
    private bool isPanelVisible = false;

    private void Awake()
    {
        _providerClient = new AmazonCognitoIdentityProviderClient(
            new AnonymousAWSCredentials(),
            RegionEndpoint.GetBySystemName(REGION));
    }

    private void OnEnable()
    {
        isPanelVisible = true;
        retryButton.onClick.AddListener(OnRetryClicked);
        CargarHistorial();
    }

    private void OnDisable()
    {
        isPanelVisible = false;
        retryButton.onClick.RemoveListener(OnRetryClicked);
        LimpiarTodo();
    }

    private void LimpiarTodo()
    {
        LimpiarTabla();
        partidasCargadas = null;
        errorText.gameObject.SetActive(false);
        retryButton.gameObject.SetActive(false);
        tituloHistorial.text = "Historial de Partidas";
    }

    private async void CargarHistorial()
    {
        if (!isPanelVisible) return;

        MostrarLoading(true);
        errorText.gameObject.SetActive(false);
        retryButton.gameObject.SetActive(false);

        try
        {
            // Verificar si el token está expirado
            string idToken = PlayerPrefs.GetString("CognitoIdToken", "");
            if (string.IsNullOrEmpty(idToken))
            {
                throw new System.Exception("No hay sesión activa");
            }

            if (TokenExpirado(idToken))
            {
                Debug.Log("Token expirado, solicitando nuevo token...");
                string refreshToken = PlayerPrefs.GetString("RefreshToken", "");
                if (!await RenovarTokens(refreshToken))
                {
                    throw new System.Exception("No se pudo renovar la sesión");
                }
                idToken = PlayerPrefs.GetString("CognitoIdToken", "");
            }

            partidasCargadas = await ObtenerHistorialPartidas(idToken);
            if (partidasCargadas != null && partidasCargadas.Count > 0)
            {
                MostrarHistorialEnUI(partidasCargadas);
            }
            else
            {
                MostrarError("No has jugado ninguna partida");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error al cargar historial: {ex.Message}");
            if (isPanelVisible) // Solo mostrar error si el panel sigue visible
            {
                MostrarError($"Error al cargar datos: {ex.Message}");
            }
        }
        finally
        {
            if (isPanelVisible) // Solo ocultar loading si el panel sigue visible
            {
                MostrarLoading(false);
            }
        }
    }

    private async Task<bool> RenovarTokens(string refreshToken)
    {
        if (string.IsNullOrEmpty(refreshToken))
        {
            Debug.LogError("No hay token de refresco disponible");
            return false;
        }

        try
        {
            var response = await _providerClient.InitiateAuthAsync(new InitiateAuthRequest
            {
                ClientId = CLIENT_ID,
                AuthFlow = AuthFlowType.REFRESH_TOKEN_AUTH,
                AuthParameters = new Dictionary<string, string>
                {
                    { "REFRESH_TOKEN", refreshToken }
                }
            });

            // Guardar los nuevos tokens
            PlayerPrefs.SetString("CognitoIdToken", response.AuthenticationResult.IdToken);
            PlayerPrefs.SetString("AccessToken", response.AuthenticationResult.AccessToken);
            PlayerPrefs.Save();

            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error renovando tokens: {e.Message}");
            return false;
        }
    }

    private bool TokenExpirado(string token)
    {
        if (string.IsNullOrEmpty(token)) return true;

        try
        {
            var payload = token.Split('.')[1];
            var json = System.Text.Encoding.UTF8.GetString(
                System.Convert.FromBase64String(payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=')));
            var claims = JsonUtility.FromJson<JwtClaims>(json);
            var expira = System.DateTimeOffset.FromUnixTimeSeconds(long.Parse(claims.exp));

            bool expirado = expira < System.DateTimeOffset.UtcNow;
            Debug.Log($"[TOKEN] Expira: {expira:g} - Expirado: {expirado}");
            return expirado;
        }
        catch
        {
            return true;
        }
    }

    private async Task<List<PartidaInfo>> ObtenerHistorialPartidas(string idToken)
    {
        try
        {
            var userId = PlayerPrefs.GetString("CognitoUserSub", "");
            if (string.IsNullOrEmpty(userId))
            {
                throw new System.Exception("ID de usuario no disponible");
            }

            var payload = new
            {
                user_id = userId,
                maxRegistros = 50
            };

            var request = new InvokeRequest
            {
                FunctionName = LAMBDA_FUNCTION,
                Payload = JsonConvert.SerializeObject(payload),
                InvocationType = InvocationType.RequestResponse
            };

            using (var client = new AmazonLambdaClient(await ObtenerCredencialesAWS(idToken), RegionEndpoint.GetBySystemName(REGION)))
            {
                var response = await client.InvokeAsync(request);
                var responseStr = Encoding.UTF8.GetString(response.Payload.ToArray());
                Debug.Log("Respuesta AWS: " + responseStr);

                var lambdaResponse = JsonConvert.DeserializeObject<LambdaResponse>(responseStr);

                if (lambdaResponse.statusCode == 200)
                {
                    var body = JsonConvert.DeserializeObject<HistorialResponseBody>(lambdaResponse.body);
                    return ConvertirAPartidaInfo(body.partidas);
                }
                else
                {
                    var errorBody = JsonConvert.DeserializeObject<ErrorResponseBody>(lambdaResponse.body);
                    throw new System.Exception(errorBody?.error ?? "Error desconocido al obtener historial");
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error en ObtenerHistorialPartidas: {ex.Message}");
            throw;
        }
    }

    private List<PartidaInfo> ConvertirAPartidaInfo(List<PartidaAWS> partidasAWS)
    {
        var partidas = new List<PartidaInfo>();
        if (partidasAWS == null) return partidas;

        foreach (var p in partidasAWS)
        {
            partidas.Add(new PartidaInfo(
                p.idPartida,
                p.fecha,
                p.jugador,
                p.oponente,
                p.puntosJugador,
                p.puntosOponente,
                p.resultado
            ));
        }
        return partidas;
    }

    private async Task<CognitoAWSCredentials> ObtenerCredencialesAWS(string idToken)
    {
        try
        {
            var credenciales = new CognitoAWSCredentials(ID_POOL_ID, RegionEndpoint.GetBySystemName(REGION));
            credenciales.AddLogin(USER_POOL_PROVIDER, idToken);

            var creds = await credenciales.GetCredentialsAsync();
            if (creds == null)
            {
                throw new System.Exception("No se pudieron obtener credenciales temporales");
            }

            Debug.Log($"[CREDS] Credenciales obtenidas. AccessKey: {creds.AccessKey.Substring(0, 5)}...");
            return credenciales;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[CREDS ERROR] {ex.Message}");
            throw new System.Exception("Error al obtener credenciales AWS: " + ex.Message);
        }
    }

    private void MostrarHistorialEnUI(List<PartidaInfo> partidas)
    {
        if (!isPanelVisible) return;

        LimpiarTabla();

        tituloHistorial.text = $"Historial de Partidas ({partidas.Count})";

        CrearFilaEncabezado();

        // Mostrar partidas con numeración secuencial
        for (int i = 0; i < partidas.Count; i++)
        {
            CrearFilaPartida(partidas[i], i + 1); // i+1 para que empiece en 1
        }
    }

    private void LimpiarTabla()
    {
        foreach (Transform child in contenidoTabla)
        {
            Destroy(child.gameObject);
        }
    }

    private void CrearFilaEncabezado()
    {
        var filaEncabezado = Instantiate(filaPartidaPrefab, contenidoTabla);
        var textos = filaEncabezado.GetComponentsInChildren<TextMeshProUGUI>();

        textos[0].text = "#";
        textos[1].text = "Fecha";
        textos[2].text = "Jugador";
        textos[3].text = "Oponente";
        textos[4].text = "Puntos";
        textos[5].text = "Puntos Op.";
        textos[6].text = "Resultado";

        // Cambiar color de texto
        foreach (var texto in textos)
        {
            texto.color = colorTextoEncabezado;
        }

        // Cambiar color de fondo (en el objeto principal de la fila)
        var fondo = filaEncabezado.GetComponent<Image>();
        if (fondo != null)
        {
            fondo.color = colorEncabezado;
        }
    }


    private void CrearFilaPartida(PartidaInfo partida, int numeroPartida)
    {
        var nuevaFila = Instantiate(filaPartidaPrefab, contenidoTabla);
        var textos = nuevaFila.GetComponentsInChildren<TextMeshProUGUI>();

        // Mostrar número de partida en lugar del ID
        textos[0].text = numeroPartida.ToString();
        textos[1].text = partida.Fecha;
        textos[2].text = partida.Jugador;
        textos[3].text = partida.Oponente;
        textos[4].text = partida.PuntosJugador.ToString();
        textos[5].text = partida.PuntosOponente.ToString();
        textos[6].text = partida.Ganador;

        textos[6].color = ObtenerColorResultado(partida);

        // Opcional: Mostrar ID real al hacer clic
        Button btn = nuevaFila.GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(() => {
                Debug.Log($"ID Partida Completo: {partida.IdPartida}");
                // O mostrar en un tooltip/panel si lo prefieres
            });
        }
    }

    private Color ObtenerColorResultado(PartidaInfo partida)
    {
        if (partida.Ganador.ToLower() == "empate") return colorEmpate;
        else if (partida.Ganador.ToLower() == "victoria") return colorVictoria;
        else return colorDerrota;
    }

    private void MostrarLoading(bool mostrar)
    {
        if (!isPanelVisible) return;
        loadingPanel.SetActive(mostrar);
    }

    private void MostrarError(string mensaje)
    {
        if (!isPanelVisible) return;
        errorText.text = mensaje;
        errorText.gameObject.SetActive(true);
        retryButton.gameObject.SetActive(true);
    }

    private void OnRetryClicked()
    {
        CargarHistorial();
    }

    [System.Serializable]
    private class JwtClaims
    {
        public string exp;
    }
}

[System.Serializable]
public class LambdaResponse
{
    public int statusCode;
    public string body;
}

[System.Serializable]
public class HistorialResponseBody
{
    public List<PartidaAWS> partidas;
    public int total;
    public string message;
    public string warning;
}

[System.Serializable]
public class ErrorResponseBody
{
    public string error;
    public string message;
}

[System.Serializable]
public class PartidaAWS
{
    public string idPartida;
    public string fecha;
    public string jugador;
    public string oponente;
    public int puntosJugador;
    public int puntosOponente;
    public int diferenciaPuntos;
    public string resultado;
}

public class PartidaInfo
{
    public string IdPartida { get; private set; }
    public string Fecha { get; private set; }
    public string Jugador { get; private set; }
    public string Oponente { get; private set; }
    public int PuntosJugador { get; private set; }
    public int PuntosOponente { get; private set; }
    public string Ganador { get; private set; }

    public PartidaInfo(string idPartida, string fecha, string jugador, string oponente,
                      int puntosJugador, int puntosOponente, string ganador)
    {
        IdPartida = idPartida;
        Fecha = fecha;
        Jugador = jugador;
        Oponente = oponente;
        PuntosJugador = puntosJugador;
        PuntosOponente = puntosOponente;
        Ganador = ganador;
    }
}