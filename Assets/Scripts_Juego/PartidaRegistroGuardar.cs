using Amazon;
using Amazon.CognitoIdentity;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using UnityEngine;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System;
using System.Text;

public class PartidaRegistroGuardar : MonoBehaviour
{
    // Configuraci�n AWS
    private const string ID_POOL_ID = "";
    private const string LAMBDA_FUNCTION = "";
    private const string REGION = "";
    private const string USER_POOL_PROVIDER = "";

    public async Task<bool> GuardarPartida(PartidaData datosPartida)
    {
        if (string.IsNullOrEmpty(datosPartida.idPartida))
        {
            Debug.LogError("ID de partida vac�o");
            return false;
        }

        try
        {
            var payload = new
            {
                accion = "guardar_partida",
                datos = new
                {
                    idPartida = datosPartida.idPartida,
                    idJugador1 = datosPartida.idJugador1,
                    idJugador2 = datosPartida.idJugador2,
                    idGanador = datosPartida.idGanador,
                    maxPuntajeJugador1 = datosPartida.maxPuntajeJugador1,
                    maxPuntajeJugador2 = datosPartida.maxPuntajeJugador2,
                    fechaPartida = datosPartida.fechaPartida,
                    terminacion = datosPartida.terminacion,
                    esEmpate = datosPartida.esEmpate
                }
            };

            var request = new InvokeRequest
            {
                FunctionName = LAMBDA_FUNCTION,
                Payload = JsonConvert.SerializeObject(payload),
                InvocationType = InvocationType.RequestResponse
            };

            using (var client = new AmazonLambdaClient(await ObtenerCredencialesAWS(), RegionEndpoint.GetBySystemName(REGION)))
            {
                var response = await client.InvokeAsync(request);
                var responseStr = Encoding.UTF8.GetString(response.Payload.ToArray());
                Debug.Log("Respuesta AWS: " + responseStr);

                return response.StatusCode == 200;
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error al guardar partida: " + e.Message);
            return false;
        }
    }
    private bool VerificarAutenticacion()
    {
        try
        {
            string idToken = PlayerPrefs.GetString("CognitoIdToken", "");
            if (string.IsNullOrEmpty(idToken))
            {
                Debug.LogWarning("[AUTH] No se encontr� token en PlayerPrefs");
                return false;
            }

            bool expirado = TokenExpirado(idToken);
            if (expirado)
            {
                Debug.LogWarning("[AUTH] Token expirado");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError("[AUTH ERROR] Error verificando autenticaci�n");
            Debug.LogError($"[AUTH DETAILS] {ex.Message}");
            return false;
        }
    }

    private async Task<CognitoAWSCredentials> ObtenerCredencialesAWS()
    {
        try
        {
            string idToken = PlayerPrefs.GetString("CognitoIdToken", "");
            if (string.IsNullOrEmpty(idToken))
            {
                Debug.LogError("[CREDS ERROR] Token no disponible");
                return null;
            }

            var credenciales = new CognitoAWSCredentials(ID_POOL_ID, RegionEndpoint.GetBySystemName(REGION));
            credenciales.AddLogin(USER_POOL_PROVIDER, idToken);

            // Forzar obtenci�n de credenciales para verificar
            var creds = await credenciales.GetCredentialsAsync();
            if (creds == null)
            {
                Debug.LogError("[CREDS ERROR] No se pudieron obtener credenciales temporales");
                return null;
            }

            Debug.Log($"[CREDS OK] Credenciales obtenidas. AccessKey: {creds.AccessKey.Substring(0, 5)}...");
            return credenciales;
        }
        catch (Exception ex)
        {
            Debug.LogError("[CREDS ERROR] Error obteniendo credenciales");
            Debug.LogError($"[CREDS DETAILS] {ex.Message}");
            return null;
        }
    }

    private bool TokenExpirado(string token)
    {
        try
        {
            var payload = token.Split('.')[1];
            var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=')));
            var claims = JsonUtility.FromJson<JwtClaims>(json);
            var expira = DateTimeOffset.FromUnixTimeSeconds(long.Parse(claims.exp));

            bool expirado = expira < DateTimeOffset.UtcNow;
            Debug.Log($"[AUTH] Token expira: {expira:g} (Expirado: {expirado})");
            return expirado;
        }
        catch (Exception ex)
        {
            Debug.LogError("[AUTH ERROR] Error verificando token");
            Debug.LogError($"[AUTH DETAILS] {ex.Message}");
            return true; // Si no podemos verificar, asumimos expirado
        }
    }

    [System.Serializable]
    private class JwtClaims
    {
        public string exp;
    }

    [System.Serializable]
    private class LambdaResponse
    {
        public bool exito;
        public string idPartida;
        public string error;
        public string detalles;
    }
}
[System.Serializable]
public class PartidaData
{
    public string idPartida;      
    public string idJugador1;     
    public string idJugador2;     
    public string idGanador;      
    public int maxPuntajeJugador1;
    public int maxPuntajeJugador2; 
    public string fechaPartida;   
    public string terminacion;   
    public bool esEmpate;         
}