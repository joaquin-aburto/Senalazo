using System;
using UnityEngine;
using TMPro;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.Runtime;
using Amazon;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using Amazon.CognitoIdentity;
using Newtonsoft.Json;
using Amazon.Lambda.Model;
using Amazon.Lambda;
using System.Net.NetworkInformation;
using UnityEngine.UI;
using System.Text.RegularExpressions;

public class ProcesoInicioSesion : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject panelInicioSesion;
    public GameObject panelMenuInicio;
    public GameObject messagePanel;
    public GameObject PanelInicioPrimario;
    public TextMeshProUGUI messageText;
    public Button closeMessageButton;
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public Button togglePasswordButton;
    public Sprite eyeOpenSprite;
    public Sprite eyeClosedSprite;

    [Header("AWS Configuration")]
    private string userPoolId = "";
    private string appClientId = "";
    private string identityPoolId = "";

    private bool isPasswordVisible = false;
    private AmazonCognitoIdentityProviderClient providerClient;

    // Expresión regular mejorada para validación de email
    private const string emailPattern =
        @"^(?("")("".+?(?<!\\)""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@)" +
        @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9][\-a-z0-9]{0,22}[a-z0-9]))$";

    // Dominios permitidos
    private readonly string[] allowedDomains = {
        "gmail.com", "hotmail.com", "outlook.com", "yahoo.com",
        "icloud.com", "protonmail.com", "mail.com", "aol.com",
        "zoho.com", "yandex.com", "gmx.com", "live.com, ceti.mx"
    };

    [System.Serializable]
    public class LambdaResponseWrapper
    {
        public int statusCode;
        public string body;
    }

    [System.Serializable]
    public class UserDataResponse
    {
        public string idUser;
        public string nickname;
        public string Nombre;
        public int PartidasGanadas;
        public int Puntuacion;
        public int PartidasJugadas;
    }

    void Awake()
    {
        // Configurar listeners de botones
        closeMessageButton.onClick.AddListener(CerrarMensaje);
        togglePasswordButton.onClick.AddListener(TogglePasswordVisibility);
    }

    void Start()
    {
        // Configuración inicial de UI
        closeMessageButton.gameObject.SetActive(false);
        UpdatePasswordVisibility();

        // Verificar conexión a internet
        if (!TieneConexionInternet())
        {
            MostrarErrorConexion();
            return;
        }

        // Configurar cliente AWS
        providerClient = new AmazonCognitoIdentityProviderClient(new AnonymousAWSCredentials(), RegionEndpoint.USEast1);

        // Intentar renovar sesión si existe refresh token
        string refreshToken = PlayerPrefs.GetString("RefreshToken", "");
        if (!string.IsNullOrEmpty(refreshToken))
        {
            _ = RenovarSesion(refreshToken);
        }
        else
        {
            panelInicioSesion.SetActive(true);
        }
    }

    private void ShowProcessMessage(string message)
    {
        messageText.text = message;
        messagePanel.SetActive(true);
        closeMessageButton.gameObject.SetActive(false);

        // Ocultar automáticamente después de 3 segundos si no es un error
        CancelInvoke(nameof(CerrarMensaje));
        Invoke(nameof(CerrarMensaje), 3f);
    }

    private void ShowErrorMessage(string message, bool showCloseButton = true)
    {
        messageText.text = message;
        messagePanel.SetActive(true);
        closeMessageButton.gameObject.SetActive(showCloseButton);

        // Cancelar ocultamiento automático para errores
        CancelInvoke(nameof(CerrarMensaje));
    }

    private bool ValidarEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            ShowErrorMessage("Por favor, ingresa tu dirección de email.");
            return false;
        }

        // Verifica que tenga @ y al menos un punto después del @
        if (!email.Contains("@") || !email.Contains(".") || email.IndexOf('@') > email.LastIndexOf('.'))
        {
            ShowErrorMessage("El correo debe tener un formato válido (ejemplo: usuario@dominio.com).");
            return false;
        }

        // Verifica que no empiece o termine con @ o .
        if (email.StartsWith("@") || email.EndsWith("@") || email.StartsWith(".") || email.EndsWith("."))
        {
            ShowErrorMessage("El correo no puede empezar o terminar con '@' o '.'.");
            return false;
        }

        // Si pasa todas las validaciones básicas, lo acepta
        return true;
    }

    private void TogglePasswordVisibility()
    {
        isPasswordVisible = !isPasswordVisible;
        UpdatePasswordVisibility();
    }

    private void UpdatePasswordVisibility()
    {
        passwordInput.contentType = isPasswordVisible ? TMP_InputField.ContentType.Standard : TMP_InputField.ContentType.Password;
        passwordInput.ForceLabelUpdate();
        togglePasswordButton.image.sprite = isPasswordVisible ? eyeOpenSprite : eyeClosedSprite;
    }

    private bool TieneConexionInternet()
    {
        try
        {
            var ping = new System.Net.NetworkInformation.Ping();
            string host = "8.8.8.8"; // Google DNS
            int timeout = 2000; // 2 segundos
            var reply = ping.Send(host, timeout);
            return reply != null && reply.Status == System.Net.NetworkInformation.IPStatus.Success;
        }
        catch (Exception)
        {
            return false;
        }
    
}

    private void MostrarErrorConexion()
    {
        ShowErrorMessage("No hay conexión a Internet. Por favor, conéctate e intenta nuevamente.");
        panelInicioSesion.SetActive(true);
        panelMenuInicio.SetActive(true);
    }

    public async void IniciarSesion()
    {
        if (!TieneConexionInternet())
        {
            MostrarErrorConexion();
            return;
        }

        string email = emailInput.text.Trim();
        string password = passwordInput.text;

        if (string.IsNullOrEmpty(email))
        {
            ShowErrorMessage("Por favor, ingresa tu dirección de email.");
            return;
        }

        if (!ValidarEmail(email))
        {
           
            return;
        }

        if (string.IsNullOrEmpty(password))
        {
            ShowErrorMessage("Por favor, ingresa tu contraseña.");
            return;
        }

        if (password.Length < 8)
        {
            ShowErrorMessage("La contraseña debe tener al menos 8 caracteres.");
            return;
        }

        try
        {
            ShowProcessMessage("Iniciando sesión...");

            var authRequest = new InitiateAuthRequest
            {
                AuthFlow = AuthFlowType.USER_PASSWORD_AUTH,
                ClientId = appClientId,
                AuthParameters = new Dictionary<string, string>
                {
                    { "USERNAME", email },
                    { "PASSWORD", password }
                }
            };

            var authResponse = await providerClient.InitiateAuthAsync(authRequest);

            if (authResponse.AuthenticationResult != null)
            {
                GuardarSesion(authResponse.AuthenticationResult);

                var userDetailsResponse = await providerClient.GetUserAsync(new GetUserRequest
                {
                    AccessToken = authResponse.AuthenticationResult.AccessToken
                });

                string userSub = userDetailsResponse.Username;
                PlayerPrefs.SetString("CognitoUserSub", userSub);
                PlayerPrefs.SetString("UserEmail", email);
                PlayerPrefs.Save();

                await ObtenerDatosUsuarioDesdeLambda(userSub);

                if (SesionActiva())
                {
                    ShowProcessMessage("Inicio de sesión exitoso. Cargando menú principal...");
                    await Task.Delay(1500);
                    SceneManager.LoadScene("MenuPrincipal");
                }
                else
                {
                    ShowErrorMessage("Error: No se pudieron cargar todos los datos. Intenta nuevamente.");
                }
            }
        }
        catch (AmazonCognitoIdentityProviderException e)
        {
            ShowErrorMessage(ObtenerMensajeErrorCognito(e));
        }
        catch (Exception e)
        {
            ShowErrorMessage("Ocurrió un error inesperado. Por favor, intenta nuevamente más tarde.");
            Debug.LogError("Error inesperado: " + e.Message);
        }
    }

    private string ObtenerMensajeErrorCognito(AmazonCognitoIdentityProviderException e)
    {
        switch (e.ErrorCode)
        {
            case "NotAuthorizedException": return "Credenciales incorrectas. Verifica tu email y contraseña.";
            case "UserNotConfirmedException": return "Tu cuenta no ha sido confirmada. Por favor verifica tu email.";
            case "UserNotFoundException": return "No existe una cuenta con este email. Regístrate primero.";
            case "InvalidParameterException": return e.Message.Contains("password") ? "La contraseña no es válida." : "Datos de entrada no válidos.";
            case "PasswordResetRequiredException": return "Debes restablecer tu contraseña. Por favor revisa tu email.";
            case "TooManyFailedAttemptsException": return "Demasiados intentos fallidos. Tu cuenta está bloqueada temporalmente.";
            case "LimitExceededException": return "Has excedido el límite de intentos. Por favor espera e intenta más tarde.";
            case "CodeMismatchException": return "El código de verificación es incorrecto.";
            case "ExpiredCodeException": return "El código de verificación ha expirado. Solicita uno nuevo.";
            case "InvalidPasswordException": return "La contraseña no cumple con los requisitos de seguridad.";
            case "UsernameExistsException": return "Este email ya está registrado.";
            default: return "Error al iniciar sesión: " + e.Message;
        }
    }

    private async Task<bool> RenovarSesion(string refreshToken)
    {
        try
        {
            var refreshRequest = new InitiateAuthRequest
            {
                AuthFlow = AuthFlowType.REFRESH_TOKEN_AUTH,
                ClientId = appClientId,
                AuthParameters = new Dictionary<string, string>
                {
                    { "REFRESH_TOKEN", refreshToken }
                }
            };

            var refreshResponse = await providerClient.InitiateAuthAsync(refreshRequest);

            if (refreshResponse.AuthenticationResult != null)
            {
                GuardarSesion(refreshResponse.AuthenticationResult);
                return true;
            }
            return false;
        }
        catch (Exception e)
        {
            Debug.LogError("Error al renovar sesión: " + e.Message);
            return false;
        }
    }

    private void GuardarSesion(AuthenticationResultType authResult)
    {
        PlayerPrefs.SetString("CognitoIdToken", authResult.IdToken);
        PlayerPrefs.SetString("AccessToken", authResult.AccessToken);
        PlayerPrefs.SetString("RefreshToken", authResult.RefreshToken);
        PlayerPrefs.SetString("LastAuthTime", DateTime.UtcNow.ToString("o"));
        PlayerPrefs.Save();
    }

    private async Task ObtenerDatosUsuarioDesdeLambda(string userSub)
    {
        try
        {
            ShowProcessMessage("Cargando tus datos...");

            var credentials = new CognitoAWSCredentials(identityPoolId, RegionEndpoint.USEast1);
            credentials.AddLogin($"cognito-idp.us-east-1.amazonaws.com/{userPoolId}", PlayerPrefs.GetString("CognitoIdToken"));

            using (var lambdaClient = new AmazonLambdaClient(credentials, RegionEndpoint.USEast1))
            {
                var request = new InvokeRequest
                {
                    FunctionName = "ObtenerDatos",
                    Payload = JsonConvert.SerializeObject(new { idUser = userSub }),
                    InvocationType = InvocationType.RequestResponse
                };

                var response = await lambdaClient.InvokeAsync(request);

                if (response.StatusCode == 200)
                {
                    string responsePayload = System.Text.Encoding.UTF8.GetString(response.Payload.ToArray());
                    var lambdaResponse = JsonConvert.DeserializeObject<LambdaResponseWrapper>(responsePayload);

                    if (lambdaResponse != null && !string.IsNullOrEmpty(lambdaResponse.body))
                    {
                        var userData = JsonConvert.DeserializeObject<UserDataResponse>(lambdaResponse.body);
                        if (userData != null)
                        {
                            PlayerPrefs.SetString("Apodo", userData.nickname ?? "");
                            PlayerPrefs.SetString("Nombre", userData.Nombre ?? "");
                            PlayerPrefs.SetInt("PartidasGanadas", userData.PartidasGanadas);
                            PlayerPrefs.SetInt("Puntuacion", userData.Puntuacion);
                            PlayerPrefs.SetInt("PartidasJugadas", userData.PartidasJugadas);
                            PlayerPrefs.SetFloat("volumenMusica", PlayerPrefs.GetFloat("volumenMusica", 1f));
                            PlayerPrefs.SetFloat("volumenSFX", PlayerPrefs.GetFloat("volumenSFX", 1f));
                            PlayerPrefs.SetInt("modoHipocausia", PlayerPrefs.GetInt("modoHipocausia", 0));
                            PlayerPrefs.Save();
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            ShowErrorMessage("Error al obtener datos del usuario: " + e.Message);
            Debug.LogError("Error al obtener datos del usuario: " + e.Message);
        }
    }

    public bool SesionActiva()
    {
        bool tieneTokens = !string.IsNullOrEmpty(PlayerPrefs.GetString("CognitoIdToken")) &&
                         !string.IsNullOrEmpty(PlayerPrefs.GetString("AccessToken")) &&
                         !string.IsNullOrEmpty(PlayerPrefs.GetString("RefreshToken"));

        bool tieneDatosUsuario = !string.IsNullOrEmpty(PlayerPrefs.GetString("Apodo")) &&
                               !string.IsNullOrEmpty(PlayerPrefs.GetString("CognitoUserSub"));

        return tieneTokens && tieneDatosUsuario;
    }

    public void CerrarSesion()
    {
        PlayerPrefs.DeleteKey("CognitoIdToken");
        PlayerPrefs.DeleteKey("AccessToken");
        PlayerPrefs.DeleteKey("RefreshToken");
        PlayerPrefs.DeleteKey("LastAuthTime");
        PlayerPrefs.DeleteKey("Apodo");
        PlayerPrefs.DeleteKey("Nombre");
        PlayerPrefs.DeleteKey("PartidasGanadas");
        PlayerPrefs.DeleteKey("Puntuacion");
        PlayerPrefs.DeleteKey("PartidasJugadas");
        PlayerPrefs.DeleteKey("CognitoUserSub");
        PlayerPrefs.DeleteKey("UserEmail");
        PlayerPrefs.Save();

        ShowErrorMessage("Has cerrado sesión correctamente.");
        panelInicioSesion.SetActive(true);
    }

    public void CerrarMensaje()
    {
        messagePanel.SetActive(false);
    }

    public void Volver()
    {
        panelInicioSesion.SetActive(false);
        panelMenuInicio.SetActive(true);
    }

    public void SalirInicio()
    {
        messagePanel.SetActive(false);
        PanelInicioPrimario.SetActive(false);
        panelInicioSesion.SetActive(false);
        panelMenuInicio.SetActive(true);
    }

    public void Cancelar()
    {
        panelMenuInicio.SetActive(true);
    }
}