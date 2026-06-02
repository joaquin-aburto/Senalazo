using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.CognitoIdentity;
using Amazon.Lambda;
using Amazon.Runtime;
using UnityEngine;
using System;
using System.Threading.Tasks;
using Amazon.Lambda.Model;
using Newtonsoft.Json;
using System.Collections.Generic;
using Amazon;


public class ActualizarDatoJugador : MonoBehaviour
{
    private const string userPoolId = "";
    private const string identityPoolId = "";
    private const string lambdaFunctionName = "";

    public GameObject panelHistorialPartidas;

    private AmazonCognitoIdentityProviderClient _providerClient;

    void Awake()
    {
        _providerClient = new AmazonCognitoIdentityProviderClient(
            new AnonymousAWSCredentials(),
            RegionEndpoint.USEast1);
    }

    public async Task<bool> ActualizarEstadisticasJugador(int partidasJugadas, int partidasGanadas, int puntuacionMaxima)
    {
        try
        {
            // 1. Cargar todos los datos desde PlayerPrefs
            string idToken = PlayerPrefs.GetString("CognitoIdToken", "");
            string accessToken = PlayerPrefs.GetString("AccessToken", "");
            string refreshToken = PlayerPrefs.GetString("RefreshToken", "");
            string userSub = PlayerPrefs.GetString("CognitoUserSub", "");

            if (string.IsNullOrEmpty(userSub))
            {
                Debug.LogError("No se encontr� el identificador de usuario");
                return false;
            }

            // 2. Verificar y renovar tokens si es necesario
            if (string.IsNullOrEmpty(idToken) || TokenExpirado(idToken))
            {
                if (!await RenovarTokens(refreshToken))
                {
                    Debug.LogError("No se pudo renovar la sesi�n");
                    return false;
                }
                // Recargar tokens despu�s de renovaci�n
                idToken = PlayerPrefs.GetString("CognitoIdToken", "");
            }

            // 3. Configurar credenciales temporales
            var credentials = new CognitoAWSCredentials(identityPoolId, RegionEndpoint.USEast1);
            credentials.AddLogin($"cognito-idp.us-east-1.amazonaws.com/{userPoolId}", idToken);

            // 4. Invocar Lambda con los datos actualizados
            using (var lambdaClient = new AmazonLambdaClient(credentials, RegionEndpoint.USEast1))
            {
                var request = new InvokeRequest
                {
                    FunctionName = lambdaFunctionName,
                    Payload = JsonConvert.SerializeObject(new
                    {
                        idUser = userSub,
                        partidasJugadas,
                        partidasGanadas,
                        puntuacionMaxima,
                        // Puedes incluir m�s datos si es necesario
                        nickname = PlayerPrefs.GetString("Apodo", ""),
                        puntuacionActual = puntuacionMaxima
                    }),
                    InvocationType = InvocationType.Event
                };

                var response = await lambdaClient.InvokeAsync(request);

                if (response.StatusCode >= 200 && response.StatusCode < 300)
                {
                    Debug.Log("Estad�sticas actualizadas exitosamente en DynamoDB");
                    return true;
                }
                else
                {
                    Debug.LogError($"Error en Lambda: {response.StatusCode}");
                    return false;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al actualizar estad�sticas: {e.Message}");
            return false;
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
                ClientId = "72kj748v0cn6cv8srgpqin9o5j",
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
        catch (Exception e)
        {
            Debug.LogError($"Error renovando tokens: {e.Message}");
            return false;
        }
    }

    private bool TokenExpirado(string token)
    {
        try
        {
            var payload = token.Split('.')[1];
            var json = System.Text.Encoding.UTF8.GetString(Base64UrlDecode(payload));
            var claims = JsonUtility.FromJson<JwtClaims>(json);
            var exp = DateTimeOffset.FromUnixTimeSeconds(long.Parse(claims.exp));
            return exp < DateTimeOffset.UtcNow;
        }
        catch
        {
            return true;
        }
    }

    private byte[] Base64UrlDecode(string input)
    {
        input = input.Replace('-', '+').Replace('_', '/');
        while (input.Length % 4 != 0) input += '=';
        return Convert.FromBase64String(input);
    }

    [System.Serializable]
    private class JwtClaims
    {
        public string exp;
    }

  

    public void mostrarPartdias()
    {
        panelHistorialPartidas.SetActive(true);
    }

    public void quitarPartdias()
    {
        panelHistorialPartidas.SetActive(false);
    }
}