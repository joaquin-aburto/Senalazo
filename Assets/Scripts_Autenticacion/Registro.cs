using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.Runtime;
using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Amazon;
using Newtonsoft.Json;
using Amazon.CognitoIdentity;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.Networking;

public class AuthManager : MonoBehaviour
{
    public TMP_InputField email_input;
    public TMP_InputField password_input;
    public TMP_InputField password2_input;
    public TMP_InputField clave_input;
    public TMP_InputField nombre_input;
    public TMP_InputField nickname_input;

    public TextMeshProUGUI messageText;
    public GameObject messagePanel;
    public GameObject closeButton;

    public GameObject panelRegistro_fondo;
    public GameObject panelRegistro_email;
    public GameObject panelRegistro_clave;
    public GameObject panelRegistro_nickname;
    public GameObject panelMenuGeneral;

    private const string clientId = "";
    private const string userPoolId = "";
    private const string lambdaFunctionName = "";

    private AmazonCognitoIdentityProviderClient providerClient;
    private AmazonLambdaClient lambdaClient;

    private string accessToken;
    private string refreshToken;
    private string idToken;
    private string userSub;

    public Button togglePasswordButton; 
    public Button togglePassword2Button; 
    public Sprite eyeOpenSprite; 
    public Sprite eyeClosedSprite; 

    void Start()
    {

        LimpiarInputs();
        if (!TieneConexionInternet())
        {
            MostrarErrorConexion();
            return;
        }

        providerClient = new AmazonCognitoIdentityProviderClient(new AnonymousAWSCredentials(), RegionEndpoint.USEast1);
        lambdaClient = new AmazonLambdaClient(new AnonymousAWSCredentials(), RegionEndpoint.USEast1);

        if (!ValidarReferencias()) return;

        panelRegistro_fondo?.SetActive(true);
        panelRegistro_email?.SetActive(true);
        panelRegistro_clave?.SetActive(false);
        panelRegistro_nickname?.SetActive(false);
        panelMenuGeneral?.SetActive(false);
        messagePanel?.SetActive(false);

        if (password_input != null && togglePasswordButton != null)
        {
            togglePasswordButton.image.sprite = password_input.contentType == TMP_InputField.ContentType.Password ?
                eyeClosedSprite : eyeOpenSprite;
        }

        if (password2_input != null && togglePassword2Button != null)
        {
            togglePassword2Button.image.sprite = password2_input.contentType == TMP_InputField.ContentType.Password ?
                eyeClosedSprite : eyeOpenSprite;
        }
    }

    private void LimpiarInputs()
    {
        if (email_input != null) email_input.text = "";
        if (password_input != null) password_input.text = "";
        if (password2_input != null) password2_input.text = "";
        if (clave_input != null) clave_input.text = "";
        if (nombre_input != null) nombre_input.text = "";
        if (nickname_input != null) nickname_input.text = "";
    }

    public void TogglePasswordVisibility()
    {
        bool isPasswordVisible = password_input.contentType == TMP_InputField.ContentType.Password;

        // Cambiar el tipo de contenido
        password_input.contentType = isPasswordVisible ?
            TMP_InputField.ContentType.Standard : TMP_InputField.ContentType.Password;

        // Cambiar el sprite del botón
        togglePasswordButton.image.sprite = isPasswordVisible ? eyeOpenSprite : eyeClosedSprite;

        password_input.ForceLabelUpdate();
    }

    public void TogglePassword2Visibility()
    {
        bool isPasswordVisible = password2_input.contentType == TMP_InputField.ContentType.Password;

        // Cambiar el tipo de contenido
        password2_input.contentType = isPasswordVisible ?
            TMP_InputField.ContentType.Standard : TMP_InputField.ContentType.Password;

        // Cambiar el sprite del botón
        togglePassword2Button.image.sprite = isPasswordVisible ? eyeOpenSprite : eyeClosedSprite;

        password2_input.ForceLabelUpdate();
    }

    private void MostrarErrorConexion()
    {
        messageText.text = "No hay conexión a Internet. Por favor, conéctate e intenta nuevamente.";
        panelRegistro_fondo?.SetActive(false);
        panelRegistro_email?.SetActive(false);
        panelRegistro_clave?.SetActive(false);
        panelRegistro_nickname?.SetActive(false);
        panelMenuGeneral?.SetActive(true);
        messagePanel?.SetActive(true);
    }

    public void regresarTod()
    {
        if (!TieneConexionInternet())
        {
            MostrarErrorConexion();
            return;
        }
        LimpiarInputs();
        panelRegistro_fondo?.SetActive(false);
        panelRegistro_email?.SetActive(false);
        panelRegistro_clave?.SetActive(false);
        panelRegistro_nickname?.SetActive(false);
        panelMenuGeneral?.SetActive(true);
        messagePanel?.SetActive(false);
    }

    private bool ValidarReferencias()
    {
        if (email_input == null || password_input == null || clave_input == null || nombre_input == null || nickname_input == null)
        {
            messageText.text = "Error en la configuración de la aplicación. Por favor, reinstala el juego.";
            messagePanel.SetActive(true);
            return false;
        }
        return true;
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

   

    private async Task<bool> VerifyEmailExists(string email)
    {
        string apiKey = "8b198a29ea5b4670a3a29ee29a18920f";
        string apiUrl = $"https://api.zerobounce.net/v2/validate?api_key={apiKey}&email={email}&ip_address=";

        using (UnityWebRequest request = UnityWebRequest.Get(apiUrl))
        {
            var operation = request.SendWebRequest();

            while (!operation.isDone)
                await Task.Yield();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
                ZeroBounceResponse response = JsonUtility.FromJson<ZeroBounceResponse>(jsonResponse);
                return (response.status == "valid");
            }
            else
            {
                Debug.LogError("Error al verificar el correo: " + request.error);
                return false;
            }
        }
    }

    [System.Serializable]
    public class ZeroBounceResponse
    {
        public string status;
        public string sub_status;
        public bool did_you_mean;
    }

    public async void SignUpUser()
    {
        if (!TieneConexionInternet())
        {
            MostrarErrorConexion();
            return;
        }

        string email = email_input.text;
        string password = password_input.text;
        string password2 = password2_input.text;

        List<string> dominiosValidos = new List<string> { "gmail.com", "icloud.com", "ceti.mx", "hotmail.com", "outlook.com" };

        if (string.IsNullOrWhiteSpace(email))
        {
            messageText.text = "Por favor ingresa tu correo electrónico.";
            messagePanel.SetActive(true);
            return;
        }

        if (!IsValidEmail(email))
        {
            messageText.text = "El formato del correo electrónico no es válido. Verifica que esté escrito correctamente.";
            messagePanel.SetActive(true);
            return;
        }

        string dominio = email.Split('@').Last();
        if (!dominiosValidos.Contains(dominio))
        {
            messageText.text = "Solo aceptamos correos de los siguientes dominios: Gmail, iCloud, Hotmail, Outlook o CETI.";
            messagePanel.SetActive(true);
            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            messageText.text = "Por favor ingresa una contraseña.";
            messagePanel.SetActive(true);
            return;
        }

        if (!IsValidPassword(password))
        {
            messageText.text = "La contraseña no cumple con los requisitos:\n\n- Debe tener al menos 8 caracteres\n- Debe contener al menos una letra mayúscula";
            messagePanel.SetActive(true);
            return;
        }

        if (password != password2)
        {
            // Detectar específicamente qué no coincide
            if (password.Length != password2.Length)
            {
                messageText.text = "Las contraseñas no coinciden. La longitud es diferente.";
            }
            else
            {
                // Encontrar las diferencias
                var differences = new List<string>();
                for (int i = 0; i < Math.Min(password.Length, password2.Length); i++)
                {
                    if (password[i] != password2[i])
                    {
                        differences.Add($"Diferencia en posición {i + 1}");
                    }
                }

                if (differences.Count > 0)
                {
                    messageText.text = "Las contraseñas no coinciden en varios caracteres. Por favor verifica cuidadosamente.";
                }
                else
                {
                    messageText.text = "Las contraseñas no coinciden. Por favor escríbelas nuevamente.";
                }
            }
            messagePanel.SetActive(true);
            return;
        }

        try
        {
            bool isEmailValid = await VerifyEmailExists(email);
            if (!isEmailValid)
            {
                messageText.text = "No pudimos verificar tu correo electrónico. Asegúrate que esté correcto y que exista.";
                messagePanel.SetActive(true);
                return;
            }

            await providerClient.SignUpAsync(new SignUpRequest
            {
                ClientId = clientId,
                Password = password,
                Username = email,
                UserAttributes = new List<AttributeType> { new AttributeType { Name = "email", Value = email } }
            });



            messageText.text = "¡Registro exitoso! Hemos enviado un código de verificación a tu correo electrónico. Por favor revísalo.";
            messagePanel.SetActive(true);
            panelRegistro_email.SetActive(false);
            panelRegistro_clave.SetActive(true);
        }
        catch (Exception e)
        {
            if (e.Message.Contains("UserAlreadyConfirmedException"))
            {
                messageText.text = "Este correo electrónico ya ha sido verificado. Por favor inicia sesión.";
            }
            else
            {
                messageText.text = TraducirMensajeError(e.Message);
            }
            messagePanel.SetActive(true);
        }


    }


    private string TraducirMensajeError(string mensajeOriginal)
    {
        // Primero verificar las excepciones más específicas
        if (mensajeOriginal.Contains("UsernameExistsException") ||
         mensajeOriginal.Contains("User already exists"))
            return "Este correo electrónico ya está registrado.";

        if (mensajeOriginal.Contains("UserAlreadyConfirmedException"))
            return "Este correo ya ha sido verificado. Por favor inicia sesión.";

        if (mensajeOriginal.Contains("InvalidPasswordException") ||
            mensajeOriginal.Contains("InvalidParameterException") &&
            mensajeOriginal.Contains("password"))
            return "La contraseña no cumple con los requisitos:\n\n- Mínimo 8 caracteres\n- Al menos una letra mayúscula\n- Al menos un número o carácter especial";

        if (mensajeOriginal.Contains("NotAuthorizedException"))
            return "Credenciales incorrectas. Verifica tu correo y contraseña.";

        if (mensajeOriginal.Contains("UserNotConfirmedException"))
            return "Tu cuenta no ha sido confirmada. Por favor verifica tu correo electrónico.";

        if (mensajeOriginal.Contains("CodeMismatchException"))
            return "El código de verificación es incorrecto. Inténtalo de nuevo.";

        if (mensajeOriginal.Contains("ExpiredCodeException"))
            return "El código de verificación ha expirado. Solicita uno nuevo.";

        if (mensajeOriginal.Contains("LimitExceededException"))
            return "Demasiados intentos. Por favor espera unos minutos antes de volver a intentar.";

        if (mensajeOriginal.Contains("InvalidParameterException"))
            return "Datos incompletos o incorrectos. Por favor revisa la información.";

        if (mensajeOriginal.Contains("ResourceNotFoundException"))
            return "Servicio no disponible. Por favor intenta más tarde.";

        // Errores generales de Cognito
        if (mensajeOriginal.Contains("CognitoIdentityProvider"))
            return "Error en el servicio de autenticación. Por favor intenta más tarde.";

        return "Ocurrió un error inesperado. Por favor intenta de nuevo más tarde.";
    }

    public async void ConfirmSignUpUser()
    {
        if (!TieneConexionInternet())
        {
            MostrarErrorConexion();
            return;
        }

        string email = email_input.text;
        string code = clave_input.text;

        if (string.IsNullOrWhiteSpace(email))
        {
            messageText.text = "Por favor ingresa tu correo electrónico.";
            messagePanel.SetActive(true);
            return;
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            messageText.text = "Por favor ingresa el código de verificación que recibiste por correo.";
            messagePanel.SetActive(true);
            return;
        }

        try
        {
            await providerClient.ConfirmSignUpAsync(new ConfirmSignUpRequest
            {
                ClientId = clientId,
                Username = email,
                ConfirmationCode = code
            });

            messageText.text = "¡Cuenta verificada con éxito! Estamos iniciando tu sesión...";
            messagePanel.SetActive(true);

            if (await ObtenerAccessToken())
            {
                panelRegistro_clave.SetActive(false);
                panelRegistro_nickname.SetActive(true);
            }
        }
        catch (Exception e)
        {
            if (e.Message.Contains("UserAlreadyConfirmedException"))
            {
                messageText.text = "Este correo ya ha sido verificado anteriormente. Por favor inicia sesión.";
                // Opcional: redirigir directamente al login
                panelRegistro_clave.SetActive(false);
                panelRegistro_email.SetActive(true);
            }
            else
            {
                messageText.text = "Error al verificar tu cuenta: " + TraducirMensajeError(e.Message);
            }
            messagePanel.SetActive(true);
        }
    }

    public async Task<bool> ObtenerAccessToken()
    {
        try
        {
            var authResponse = await providerClient.InitiateAuthAsync(new InitiateAuthRequest
            {
                ClientId = clientId,
                AuthFlow = AuthFlowType.USER_PASSWORD_AUTH,
                AuthParameters = new Dictionary<string, string>
                {
                    { "USERNAME", email_input.text },
                    { "PASSWORD", password_input.text }
                }
            });

            accessToken = authResponse.AuthenticationResult.AccessToken;
            refreshToken = authResponse.AuthenticationResult.RefreshToken;
            idToken = authResponse.AuthenticationResult.IdToken;

            PlayerPrefs.SetString("AccessToken", accessToken);
            PlayerPrefs.SetString("RefreshToken", refreshToken);
            PlayerPrefs.SetString("CognitoIdToken", idToken);
            PlayerPrefs.Save();

            return true;
        }
        catch (Exception e)
        {
            messageText.text = "Error al iniciar sesión: " + TraducirMensajeError(e.Message);
            messagePanel.SetActive(true);
            return false;
        }
    }

    public async void AddNickname()
    {
        if (!TieneConexionInternet())
        {
            MostrarErrorConexion();
            return;
        }

        string nickname = nickname_input.text;
        string nombre = nombre_input.text;

        if (string.IsNullOrWhiteSpace(nickname))
        {
            messageText.text = "Por favor ingresa un apodo para mostrarlo en el juego.";
            messagePanel.SetActive(true);
            return;
        }

        if (string.IsNullOrWhiteSpace(nombre))
        {
            messageText.text = "Por favor ingresa tu nombre real.";
            messagePanel.SetActive(true);
            return;
        }

        if (nickname.Length > 15)
        {
            messageText.text = "El apodo no puede tener más de 15 caracteres.";
            messagePanel.SetActive(true);
            return;
        }

        string idToken = PlayerPrefs.GetString("CognitoIdToken", "");

        if (string.IsNullOrEmpty(idToken))
        {
            messageText.text = "Sesión expirada. Por favor inicia sesión nuevamente.";
            messagePanel.SetActive(true);
            return;
        }

        try
        {
            var userDetailsResponse = await providerClient.GetUserAsync(new GetUserRequest { AccessToken = accessToken });
            userSub = userDetailsResponse.Username;

            var lambdaPayload = new
            {
                idUser = userSub,
                nickname = nickname,
                nombre = nombre
            };

            string payloadJson = JsonConvert.SerializeObject(lambdaPayload);

            var credentials = new CognitoAWSCredentials(
                "us-east-1:e5ddddd4-07ab-4c8f-8c98-81a5d970a88a",
                RegionEndpoint.USEast1
            );

            credentials.AddLogin($"cognito-idp.us-east-1.amazonaws.com/{userPoolId}", idToken);

            var lambdaClient = new AmazonLambdaClient(credentials, RegionEndpoint.USEast1);

            var request = new InvokeRequest
            {
                FunctionName = lambdaFunctionName,
                Payload = payloadJson,
                InvocationType = InvocationType.RequestResponse
            };

            var response = await lambdaClient.InvokeAsync(request);

            if (response.StatusCode == 200)
            {
                // Limpiar cualquier dato previo
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

                // Guardar los nuevos datos
                PlayerPrefs.SetString("CognitoIdToken", idToken);
                PlayerPrefs.SetString("AccessToken", accessToken);
                PlayerPrefs.SetString("RefreshToken", refreshToken);
                PlayerPrefs.SetString("LastAuthTime", DateTime.UtcNow.ToString("o"));
                PlayerPrefs.SetString("Apodo", nickname);
                PlayerPrefs.SetString("Nombre", nombre);
                PlayerPrefs.SetInt("PartidasGanadas", 0);
                PlayerPrefs.SetInt("Puntuacion", 0);
                PlayerPrefs.SetInt("PartidasJugadas", 0);
                PlayerPrefs.SetString("CognitoUserSub", userSub);
                PlayerPrefs.SetString("UserEmail", email_input.text);
                PlayerPrefs.SetFloat("volumenMusica", 1f);
                PlayerPrefs.SetFloat("volumenSFX", 1f);
                PlayerPrefs.SetInt("modoHipocausia", 0);

                PlayerPrefs.Save();

                messageText.text = "¡Todo listo! Tu perfil ha sido creado con éxito.";
                messagePanel.SetActive(true);
                SceneManager.LoadScene("MenuPrincipal");
            }
            else
            {
                messageText.text = "Error al guardar tus datos. Por favor intenta nuevamente.";
                messagePanel.SetActive(true);
            }
        }
        catch (Exception e)
        {
            messageText.text = "Error al completar tu registro: " + TraducirMensajeError(e.Message);
            messagePanel.SetActive(true);
        }
    }

    public void CloseMessagePanel()
    {
        messagePanel?.SetActive(false);
    }

    private bool IsValidEmail(string email)
    {
        try { return new System.Net.Mail.MailAddress(email).Address == email; }
        catch { return false; }
    }

    private bool IsValidPassword(string password)
    {
        return password.Length >= 8 &&
               Regex.IsMatch(password, "[A-Z]");
    }
}