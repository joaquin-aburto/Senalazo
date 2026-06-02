using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;
using UnityEngine.UI;
using System.Collections;
using System.Text;
using System;
using System.Net.Sockets;
using System.Threading;
using UnityEngine.SceneManagement;
using DG.Tweening;
using Photon.Realtime;

public class CardManager : MonoBehaviourPunCallbacks
{
    public static CardManager Instance;

    // UI Elements
    public Image cardPrefab;
    public Transform cardContainer;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI myScoreText;
    public TextMeshProUGUI enemyScoreText;
    public TextMeshProUGUI gloveLetterText;
    public TextMeshProUGUI feedbackText;
    public TextMeshProUGUI myNicknameText;
    public TextMeshProUGUI enemyNicknameText;
    public TextMeshProUGUI myCardsProgressText;
    public TextMeshProUGUI enemyCardsProgressText;
    public Sprite[] allCards;
    public Sprite backSprite;
    public GameObject endGamePanel;
    public TextMeshProUGUI endGameText;
    public GameObject disconnectPanel;
    public TextMeshProUGUI disconnectText;
    public Button returnToMenuButton;

    // Game Data
    private List<Sprite> selectedCards = new List<Sprite>();
    private List<Image> cardInstances = new List<Image>();
    private List<string> usedLetters = new List<string>();
    private List<string> playerCardLetters = new List<string>();
    private HashSet<string> letrasAcertadas = new HashSet<string>();

    // Game Settings
    public float moveDuration = 0.5f;
    public float flipDuration = 0.5f;
    public float timeBetweenCards = 3.5f;
    public float feedbackDisplayTime = 1.5f;
    private string[] abecedario = { "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "�", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z" };

    // Game State
    private float timerCountdown;
    private bool isTimerRunning = false;
    private string currentLetter = "";
    private int myScore = 0;
    private int enemyScore = 0;
    private bool gameEnded = false;
    private int myCardsMatched = 0;
    private int enemyCardsMatched = 0;
    private bool isInitialized = false;

    // Network Connections
    private TcpClient tcpClient;
    private NetworkStream networkStream;
    private string serverIP = "127.0.0.1";
    private int serverPort = 25001;
    private Thread listenerThread;
    private bool isListening = false;
    private float reconnectionDelay = 3.0f;
    private bool isReconnecting = false;

    // Coroutines
    private Coroutine letterSelectionCoroutine;
    private Coroutine gameStatusCheckCoroutine;
    private Coroutine endGameTimeoutCoroutine;

    [Header("Audio")]
    public AudioSource backgroundMusicSource;  
    public AudioSource sfxSource;              
    public AudioClip correctCardSound;         
    public AudioClip[] backgroundMusicClips;   


    private PartidaRegistroGuardar registroGuardar;

    // Agregar estas variables como campos de clase
    private string opponentCognitoId;
    private int opponentHighScore;
    private int opponentFinalScore;


    [Header("Hipocausia Settings")]
    public GameObject hipocausiaPanel;  
    public Image feedbackImage;         
    public Sprite correctSprite;        
    public Sprite incorrectSprite;      
    public float feedbackDuration = 1.5f;

    public LoteriaCard loteriaCard;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {

        InitializeGame();
        PlayRandomBackgroundMusic();
        registroGuardar = gameObject.AddComponent<PartidaRegistroGuardar>();

        if (!PhotonNetwork.IsMasterClient)
        {
            string myId = PlayerPrefs.GetString("CognitoUserSub", "");
            if (string.IsNullOrEmpty(myId))
            {
                myId = "INVITADO_" + DateTime.Now.Ticks.ToString().Substring(10);
                Debug.LogWarning($"Usando ID temporal: {myId}");
            }

            photonView.RPC("SendOpponentData",
                          RpcTarget.MasterClient,
                          myId,
                          0,  // Puntos iniciales
                          partidaIdUnico);
        }
    }
    private void PlayRandomBackgroundMusic()
    {
        if (backgroundMusicClips.Length == 0 || backgroundMusicSource == null) return;

        int randomIndex = UnityEngine.Random.Range(0, backgroundMusicClips.Length);
        AudioClip selectedClip = backgroundMusicClips[randomIndex];

        backgroundMusicSource.clip = selectedClip;
        backgroundMusicSource.loop = true;
        backgroundMusicSource.Play();
    }

    void CheckLetterMatch(string detectedLetter)
    {
        if (string.IsNullOrEmpty(currentLetter)) return;

        detectedLetter = detectedLetter.ToLower();
        string currentLower = currentLetter.ToLower();

        if (detectedLetter == currentLower)
        {
            RegisterCorrectLetter(currentLower);
            ShowHipocausiaFeedback(true); // Mostrar feedback visual de acierto
        }
        else
        {
            ShowFeedback("Incorrecto: " + detectedLetter.ToUpper() + " / " + currentLetter.ToUpper(), Color.red);
            ShowHipocausiaFeedback(false); // Mostrar feedback visual de error
        }
    }

    void InitializeGame()
    {
        PhotonNetwork.AddCallbackTarget(this);
        if (isInitialized) return;

        // Setup UI - Verificar que los elementos UI est�n asignados
        if (endGamePanel == null || disconnectPanel == null || myNicknameText == null ||
            enemyNicknameText == null || timerText == null || feedbackText == null)
        {
            Debug.LogError("Faltan asignar componentes UI en el Inspector!");
            return;
        }

        endGamePanel.SetActive(false);
        disconnectPanel.SetActive(false);

        // Solo configurar el bot�n si est� asignado
        if (returnToMenuButton != null)
        {
            returnToMenuButton.onClick.AddListener(ReturnToMenu);
        }
        else
        {
            Debug.LogWarning("returnToMenuButton no est� asignado en el Inspector");
        }

        // Resto de la inicializaci�n...
        if (UnityMainThreadDispatcher.Instance() == null)
        {
            new GameObject("UnityMainThreadDispatcher").AddComponent<UnityMainThreadDispatcher>();
        }

        LoadPlayerCards();
        string myNickname = PlayerPrefs.GetString("Apodo", "Jugador");
        myNicknameText.text = myNickname;

        if (photonView != null)
        {
            photonView.RPC("SendNickname", RpcTarget.OthersBuffered, myNickname);
        }

        ConnectToServer();

        if (PhotonNetwork.IsMasterClient)
        {
            letterSelectionCoroutine = StartCoroutine(SelectLetterCoroutine());
            gameStatusCheckCoroutine = StartCoroutine(GameStatusCheck());
        }

        UpdateScoresUI();
        UpdateCardsProgressUI();
        isInitialized = true;

        if (hipocausiaPanel != null)
        {
            bool hipocausiaActiva = PlayerPrefs.GetInt("modoHipocausia", 0) == 1;
            hipocausiaPanel.SetActive(hipocausiaActiva);

            if (hipocausiaActiva && feedbackImage != null)
            {
                feedbackImage.gameObject.SetActive(false);
            }
        }
    }

    private void ShowHipocausiaFeedback(bool isCorrect)
    {
        if (hipocausiaPanel == null || !hipocausiaPanel.activeSelf) return;

        if (feedbackImage != null)
        {
            feedbackImage.sprite = isCorrect ? correctSprite : incorrectSprite;
            feedbackImage.gameObject.SetActive(true);

            // Ocultar despu�s de un tiempo
            StartCoroutine(HideFeedbackAfterDelay());
        }
    }

    private IEnumerator HideFeedbackAfterDelay()
    {
        yield return new WaitForSeconds(feedbackDuration);
        if (feedbackImage != null)
        {
            feedbackImage.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (isTimerRunning && !gameEnded && !endGameRequested)
        {
            timerCountdown -= Time.deltaTime;
            timerCountdown = Mathf.Max(timerCountdown, 0);
            timerText.text = "Tiempo restante: " + Mathf.Ceil(timerCountdown).ToString() + "s";

            if (timerCountdown <= 0)
            {
                isTimerRunning = false;
                timerText.text = "Tiempo restante: 0s";
            }
        }
    }

    #region Game Initialization
    void LoadPlayerCards()
    {
        string savedCards = PlayerPrefs.GetString("CartasSeleccionadas", "");
        if (!string.IsNullOrEmpty(savedCards))
        {
            string[] cardNames = savedCards.Split(',');
            foreach (string cardName in cardNames)
            {
                if (!string.IsNullOrEmpty(cardName))
                {
                    playerCardLetters.Add(cardName.Trim().ToLower());
                }
            }
            Debug.Log("Player cards loaded: " + string.Join(", ", playerCardLetters));
        }
        else
        {
            Debug.LogWarning("No saved cards found in PlayerPrefs");
        }
    }

    public bool HasCard(string letter)
    {
        return playerCardLetters.Contains(letter.ToLower());
    }
    #endregion

    #region Game Logic
  

    public void RegisterCorrectLetter(string letra)
    {
        if (letrasAcertadas.Contains(letra)) return;

        letrasAcertadas.Add(letra);
        bool hasCard = loteriaCard != null && loteriaCard.HasCard(letra);
        int points = hasCard ? 100 : 50;

        myScore += points;
        if (hasCard)
        {
            myCardsMatched++;
            loteriaCard.CheckCardInSlots(letra); // Esto activar� la animaci�n

            if (correctCardSound != null && sfxSource != null)
            {
                sfxSource.PlayOneShot(correctCardSound);
            }
        }

        UpdateScoresUI();
        UpdateCardsProgressUI();

        string message = hasCard ?
            "�Correcto! +100 (Carta en tu tablero)" :
            "�Correcto! +50 (Carta no est� en tu tablero)";
        Color color = hasCard ? Color.green : new Color(0.4f, 0.8f, 0.4f);
        ShowFeedback(message, color);

        photonView.RPC("UpdateEnemyScore", RpcTarget.Others, myScore, hasCard);
        CheckGameEndCondition();
    }

    private float lastLetterShowTime;
    private bool endGameRequested = false;

    

    private IEnumerator DelayedEndGame(float delay, bool byCompleteCards)
    {
        endGameRequested = true;

        // Mostrar feedback sobre el tiempo restante
        float remainingTime = delay;
        while (remainingTime > 0)
        {
            timerText.text = "Tiempo final: " + Mathf.Ceil(remainingTime).ToString() + "s";
            remainingTime -= Time.deltaTime;
            yield return null;
        }

        TriggerEndGame(byCompleteCards);
    }

    private void TriggerEndGame(bool byCompleteCards)
    {
        isTimerRunning = false; // Detener el temporizador
        bool isMaster = PhotonNetwork.IsMasterClient;
        int masterScore = isMaster ? myScore : enemyScore;
        int otherScore = isMaster ? enemyScore : myScore;

        photonView.RPC("EndGameRPC", RpcTarget.All, masterScore, otherScore, byCompleteCards);
    }

    [PunRPC]
    private void UpdateLetter(string letter)
    {
        lastLetterShowTime = Time.time;
        currentLetter = letter;
        gloveLetterText.text = "Letra actual: " + letter.ToUpper();

        // Reiniciar el temporizador para esta nueva carta
        timerCountdown = timeBetweenCards;
        isTimerRunning = true;
        photonView.RPC("UpdateTimer", RpcTarget.All, timerCountdown);

        // Find and display corresponding card
        foreach (Sprite card in allCards)
        {
            if (card.name.ToLower() == letter.ToLower())
            {
                CreateCard(card);
                break;
            }
        }
    }




    [PunRPC]
    private void UpdateEnemyScore(int score, bool wasCardMatch)
    {
        enemyScore = score;
        if (wasCardMatch) enemyCardsMatched++;

        UpdateScoresUI();
        UpdateCardsProgressUI();
        CheckGameEndCondition();
    }
  


    private string partidaIdUnico = "";
    private bool partidaGuardada = false;

    [PunRPC]
    private IEnumerator EndGame(int masterScore, int otherScore, bool byCompleteCards)
    {
        if (gameEnded) yield break;

        gameEnded = true;
        StopAllCoroutines();

        // Detener y ocultar el temporizador
        isTimerRunning = false;
        timerText.text = "Juego terminado";

        // Esperar a que termine cualquier animaci�n de carta en curso
        yield return new WaitForSeconds(moveDuration + flipDuration + 0.1f);
        // Determinar puntajes seg�n si es master client o no
        if (PhotonNetwork.IsMasterClient)
        {
            myScore = masterScore;
            enemyScore = otherScore;
            partidaIdUnico = $"partida_{PhotonNetwork.CurrentRoom.Name}_{DateTime.UtcNow:yyyyMMddHHmmss}";
        }
        else
        {
            myScore = otherScore;
            enemyScore = masterScore;

            // Enviar datos al master client
            string myId = PlayerPrefs.GetString("CognitoUserSub", "DESCONOCIDO");
            photonView.RPC("SendPlayerDataToMaster", RpcTarget.MasterClient,
                          myId, myScore, partidaIdUnico);
        }

        // Actualizar UI
        UpdateEndGameUI(byCompleteCards);

        // Solo el master client guarda la partida
        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(GuardarPartidaCoroutine(byCompleteCards));
        }
    }
    [PunRPC]
    private IEnumerator EndGameRPC(int masterScore, int otherScore, bool byCompleteCards)
    {
        if (gameEnded) yield break;

        gameEnded = true;
        StopAllCoroutines();

        // Mostrar cuenta regresiva de 7 segundos para la �ltima carta
        float endGameDelay = 7f;
        float timer = endGameDelay;

        while (timer > 0)
        {
            timerText.text = "Tiempo final: " + Mathf.Ceil(timer).ToString() + "s";
            timer -= Time.deltaTime;
            yield return null;
        }

        // Esperar a que termine cualquier animaci�n de carta pendiente
        yield return new WaitForSeconds(moveDuration + flipDuration);

        // Actualizar puntajes finales
        if (PhotonNetwork.IsMasterClient)
        {
            myScore = masterScore;
            enemyScore = otherScore;
            partidaIdUnico = $"partida_{PhotonNetwork.CurrentRoom.Name}_{DateTime.UtcNow:yyyyMMddHHmmss}";
        }
        else
        {
            myScore = otherScore;
            enemyScore = masterScore;
        }

        // Mostrar resultados finales
        timerText.text = "Juego terminado";
        UpdateEndGameUI(byCompleteCards);

        // Solo el master guarda los resultados
        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(GuardarPartidaCoroutine(byCompleteCards));
        }
    }

    [PunRPC]
    private void SendOpponentData(string cognitoId, int score, string matchId)
    {
        // Solo el master debe procesar esto
        if (!PhotonNetwork.IsMasterClient || matchId != partidaIdUnico) return;

        opponentCognitoId = cognitoId;
        opponentFinalScore = score;
        Debug.Log($"Datos recibidos del oponente: ID={cognitoId}, Puntos={score}");
    }

    private IEnumerator GuardarPartidaUnaSolaVez(bool byCompleteCards)
    {
        // Esperar por datos del oponente con timeout
        float timeout = 5f;
        float elapsed = 0f;

        while (string.IsNullOrEmpty(opponentCognitoId) && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (string.IsNullOrEmpty(opponentCognitoId))
        {
            opponentCognitoId = "DESCONOCIDO_" + DateTime.UtcNow.Ticks;
            opponentFinalScore = enemyScore;
        }

        var datosPartida = new PartidaData
        {
            idPartida = partidaIdUnico,
            idJugador1 = PlayerPrefs.GetString("CognitoUserSub", ""),
            idJugador2 = opponentCognitoId,
            idGanador = DetermineWinner(),
            maxPuntajeJugador1 = myScore,
            maxPuntajeJugador2 = opponentFinalScore,
            fechaPartida = DateTime.UtcNow.ToString("o"),
            terminacion = byCompleteCards ? "COMPLETO_CARTAS" : "TIEMPO_AGOTADO",
            esEmpate = (myScore == opponentFinalScore)
        };

        bool exito = false;
        int intentos = 0;

        while (!exito && intentos < 3)
        {
            var tarea = registroGuardar.GuardarPartida(datosPartida);
            yield return new WaitUntil(() => tarea.IsCompleted);

            if (tarea.Result)
            {
                exito = true;
                Debug.Log("Partida guardada exitosamente");
                break;
            }

            intentos++;
            yield return new WaitForSeconds(1f);
        }

        if (!exito)
        {
            Debug.LogError("No se pudo guardar la partida despu�s de 3 intentos");
        }
    }

    [PunRPC]
    private void SendPlayerDataToMaster(string playerId, int playerScore, string partidaId)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        // Verificar que el ID de partida coincida
        if (partidaId != partidaIdUnico)
        {
            Debug.LogWarning($"[GUARDADO] ID de partida no coincide: recibido {partidaId}, esperado {partidaIdUnico}");
            return;
        }

        opponentCognitoId = playerId;
        opponentFinalScore = playerScore;

        Debug.Log($"[GUARDADO] Datos del oponente recibidos - ID: {playerId}, Puntaje: {playerScore}");
    }

    private IEnumerator GuardarPartidaCoroutine(bool byCompleteCards)
    {
        // Esperar m�ximo 3 segundos por datos del oponente
        float timeout = 3f;
        float elapsed = 0f;

        while (string.IsNullOrEmpty(opponentCognitoId) && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Si no lleg� el ID del oponente, usar valor por defecto
        if (string.IsNullOrEmpty(opponentCognitoId))
        {
            opponentCognitoId = "DESCONOCIDO_" + DateTime.UtcNow.Ticks;
            Debug.LogWarning("[GUARDADO] Usando ID de oponente por defecto");
        }

        Debug.Log($"Guardando - Master: {myScore} pts, Invitado: {enemyScore} pts");

        var datosPartida = new PartidaData
        {
            idPartida = partidaIdUnico,
            idJugador1 = PlayerPrefs.GetString("CognitoUserSub", "MASTER_ANONIMO"),
            idJugador2 = opponentCognitoId,
            maxPuntajeJugador1 = myScore,
            maxPuntajeJugador2 = enemyScore,
            idGanador = DetermineWinner(), // Usamos la funci�n mejorada
            fechaPartida = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
            terminacion = byCompleteCards ? "CARTAS_COMPLETAS" : "TIEMPO_AGOTADO",
            esEmpate = (myScore == enemyScore) || (myScore == 0 && enemyScore == 0)
        };

        // Intentar guardar hasta 3 veces
        bool exito = false;
        int intentos = 0;

        while (!exito && intentos < 3)
        {
            var tarea = registroGuardar.GuardarPartida(datosPartida);
            yield return new WaitUntil(() => tarea.IsCompleted);

            exito = tarea.Result;
            if (!exito)
            {
                intentos++;
                yield return new WaitForSeconds(1f);
            }
        }

        if (!exito)
        {
            Debug.LogError("No se pudo guardar la partida despu�s de 3 intentos");
        }
    }

    private string DetermineWinner()
    {
        // Caso especial: ambos con 0 puntos
        if (myScore == 0 && enemyScore == 0)
        {
            return "EMPATE_CERO";
        }

        // Empate normal
        if (myScore == enemyScore)
        {
            return "EMPATE";
        }

        // Determinar ganador normal
        return myScore > enemyScore ?
               PlayerPrefs.GetString("CognitoUserSub", "MASTER") :
               opponentCognitoId;
    }



    void CheckGameEndCondition()
    {
        if (gameEnded) return;

        bool cardsCompleted = (myCardsMatched >= 16) || (enemyCardsMatched >= 16);
        bool allLettersUsed = (usedLetters.Count >= abecedario.Length);

        if (cardsCompleted || allLettersUsed)
        {
            bool isMaster = PhotonNetwork.IsMasterClient;
            int masterScore = isMaster ? myScore : enemyScore;
            int otherScore = isMaster ? enemyScore : myScore;

            photonView.RPC("EndGameRPC", RpcTarget.All, masterScore, otherScore, cardsCompleted);
        }
    }



    private IEnumerator EsperarYGuardarPartida(bool byCompleteCards)
    {
        // Esperar m�ximo 5 segundos por los datos del oponente
        float tiempoEspera = 5f;
        float tiempoTranscurrido = 0f;

        while (string.IsNullOrEmpty(opponentCognitoId) && tiempoTranscurrido < tiempoEspera)
        {
            tiempoTranscurrido += Time.deltaTime;
            yield return null;
        }

        if (string.IsNullOrEmpty(opponentCognitoId))
        {
            Debug.LogWarning("[GUARDADO] No se recibieron datos del oponente, usando valores por defecto");
            opponentCognitoId = "DESCONOCIDO_" + DateTime.UtcNow.Ticks.ToString();
            opponentFinalScore = enemyScore;
        }

        yield return GuardarRegistroPartidaUnaVez(byCompleteCards, opponentFinalScore);
    }

    private IEnumerator GuardarRegistroPartidaUnaVez(bool porCartasCompletas, int puntuacionOponente)
    {
        string miId = PlayerPrefs.GetString("CognitoUserSub", "");
        if (string.IsNullOrEmpty(miId))
        {
            Debug.LogError("[GUARDADO] ID de jugador local no encontrado");
            yield break;
        }

        // Determinar resultado final
        string idGanador;
        if (myScore == 0 && puntuacionOponente == 0)
        {
            idGanador = "EMPATE_CERO";
        }
        else if (myScore == puntuacionOponente)
        {
            idGanador = "EMPATE";
        }
        else
        {
            idGanador = myScore > puntuacionOponente ? miId : opponentCognitoId;
        }

        var datosPartida = new PartidaData
        {
            idPartida = partidaIdUnico, // Usamos el ID �nico generado
            idJugador1 = miId,
            idJugador2 = opponentCognitoId,
            idGanador = idGanador,
            maxPuntajeJugador1 = myScore,
            maxPuntajeJugador2 = puntuacionOponente,
            fechaPartida = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            terminacion = porCartasCompletas ? "COMPLETO_CARTAS" : "TIEMPO_AGOTADO",
            esEmpate = (idGanador == "EMPATE" || idGanador == "EMPATE_CERO")
        };

        bool exito = false;
        int intentos = 0;
        const int maxIntentos = 2;

        while (!exito && intentos < maxIntentos)
        {
            intentos++;
            Debug.Log($"[GUARDADO] Intento {intentos} de guardar partida {partidaIdUnico}");

            var tarea = registroGuardar.GuardarPartida(datosPartida);
            yield return new WaitUntil(() => tarea.IsCompleted);

            if (tarea.Result)
            {
                exito = true;
                Debug.Log($"[GUARDADO] Partida {partidaIdUnico} guardada exitosamente");
            }
            else if (intentos < maxIntentos)
            {
                Debug.LogWarning($"[GUARDADO] Fall� intento {intentos}, reintentando...");
                yield return new WaitForSeconds(1f);
            }
        }

        if (!exito)
        {
            Debug.LogError($"[GUARDADO ERROR] No se pudo guardar la partida {partidaIdUnico} despu�s de {maxIntentos} intentos");
        }
    }

    [PunRPC]
    private void SendPlayerDataToMaster(string playerId, int playerScore, bool completedByCards, string idPartida)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        // Verificar que el ID de partida coincida
        if (idPartida != partidaIdUnico)
        {
            Debug.LogWarning($"[GUARDADO] ID de partida no coincide: recibido {idPartida}, esperado {partidaIdUnico}");
            return;
        }

        opponentCognitoId = playerId;
        opponentFinalScore = playerScore;
        Debug.Log($"[GUARDADO] Datos del oponente recibidos para partida {partidaIdUnico}");
    }

    private IEnumerator DelayedSaveGame(bool byCompleteCards)
    {
        yield return new WaitForSeconds(1f);

        if (!PhotonNetwork.IsConnected)
        {
            Debug.LogError("[GUARDADO ERROR] Perdimos conexi�n durante el guardado");
            yield break;
        }

        if (string.IsNullOrEmpty(opponentCognitoId))
        {
            Debug.Log("[GUARDADO] Esperando datos de oponente...");
            yield return new WaitForSeconds(2f);

            if (string.IsNullOrEmpty(opponentCognitoId))
            {
                Debug.LogError("[GUARDADO ERROR] Nunca recibimos ID de oponente");
                yield break;
            }
        }

        Debug.Log($"[GUARDADO] Guardando partida: {myScore} vs {enemyScore}");
        yield return StartCoroutine(GuardarRegistroPartidaCoroutine(byCompleteCards, enemyScore));
    }

    private IEnumerator GuardarRegistroPartidaCoroutine(bool porCartasCompletas, int puntuacionOponente)
    {
        bool guardadoExitoso = false;
        int intentos = 0;
        const int maxIntentos = 3;

        // Esperar por datos del oponente con timeout m�s corto para abandonos
        float timeout = porCartasCompletas ? 5f : 2f; // Menos tiempo si fue abandono
        float elapsed = 0f;

        while (string.IsNullOrEmpty(opponentCognitoId) && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (string.IsNullOrEmpty(opponentCognitoId))
        {
            opponentCognitoId = "ABANDONO_" + DateTime.UtcNow.Ticks;
            opponentFinalScore = puntuacionOponente;
        }

        var datosPartida = new PartidaData
        {
            idPartida = partidaIdUnico,
            idJugador1 = PlayerPrefs.GetString("CognitoUserSub", "MASTER_ANONIMO"),
            idJugador2 = opponentCognitoId,
            maxPuntajeJugador1 = myScore,
            maxPuntajeJugador2 = opponentFinalScore,
            idGanador = DetermineWinner(),
            fechaPartida = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
            terminacion = porCartasCompletas ? "CARTAS_COMPLETAS" : opponentCognitoId.StartsWith("ABANDONO_") ? "ABANDONO" : "TIEMPO_AGOTADO",
            esEmpate = (myScore == opponentFinalScore) && !opponentCognitoId.StartsWith("ABANDONO_") // Solo empate si no fue abandono
        };

        while (!guardadoExitoso && intentos < maxIntentos)
        {
            Debug.Log($"[GUARDADO] Intento {intentos + 1} de guardar partida");

            var tarea = registroGuardar.GuardarPartida(datosPartida);
            yield return new WaitUntil(() => tarea.IsCompleted);

            if (tarea.Result)
            {
                guardadoExitoso = true;
                Debug.Log("[GUARDADO] Partida guardada exitosamente");
            }
            else
            {
                intentos++;
                if (intentos < maxIntentos)
                {
                    Debug.LogWarning($"[GUARDADO] Reintentando en 1 segundo...");
                    yield return new WaitForSeconds(1f);
                }
                else
                {
                    Debug.LogError("[GUARDADO ERROR] Fall� el guardado despu�s de varios intentos");
                }
            }
        }
    }



    private void UpdateEndGameUI(bool byCompleteCards)
    {
        // Solo mostrar resultados si no fue por desconexi�n
        if (!disconnectPanel.activeSelf)
        {
            UpdatePlayerStats();
            string resultText = GenerateEndGameText(byCompleteCards);

            UnityMainThreadDispatcher.Instance().Enqueue(() => {
                endGameText.text = resultText;
                endGamePanel.SetActive(true);
                timerText.text = "Juego terminado";
                UpdateScoresUI();
                UpdateCardsProgressUI();
            });
        }
        else
        {
            // En caso de desconexi�n, solo actualizar stats sin mostrar UI
            UpdatePlayerStats();
        }
    }

    

    [PunRPC]
    private void SendPlayerDataToMaster(string playerId, int playerScore, bool completedByCards)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            opponentCognitoId = playerId;
            opponentFinalScore = playerScore;
            GuardarRegistroPartida(completedByCards, playerScore);
        }
    }






    private async void GuardarRegistroPartida(bool porCartasCompletas, int puntuacionOponente)
    {
        Debug.Log("[GUARDADO] Iniciando proceso de guardado...");

        try
        {
            string miId = PlayerPrefs.GetString("CognitoUserSub", "");
            if (string.IsNullOrEmpty(miId))
            {
                Debug.LogError("[GUARDADO ERROR] ID de jugador local no encontrado");
                return;
            }

            if (string.IsNullOrEmpty(opponentCognitoId))
            {
                Debug.LogError("[GUARDADO ERROR] ID de oponente no recibido");
                return;
            }

            // Determinar el ganador (o empate)
            string idGanador;
            if (myScore == 0 && puntuacionOponente == 0)
            {
                idGanador = "EMPATE_CERO"; // Caso especial cuando ambos tienen 0 puntos
            }
            else if (myScore == puntuacionOponente)
            {
                idGanador = "EMPATE"; // Empate normal
            }
            else
            {
                idGanador = myScore > puntuacionOponente ? miId : opponentCognitoId;
            }

            var datosPartida = new PartidaData
            {
                idJugador1 = miId,
                idJugador2 = opponentCognitoId,
                idGanador = idGanador,
                maxPuntajeJugador1 = myScore,
                maxPuntajeJugador2 = puntuacionOponente,
                fechaPartida = DateTime.UtcNow.ToString("o"),
                terminacion = porCartasCompletas ? "COMPLETO_CARTAS" : "TIEMPO_AGOTADO",
                esEmpate = (myScore == puntuacionOponente)
            };

            bool exito = await registroGuardar.GuardarPartida(datosPartida);

            if (exito)
            {
                Debug.Log("[GUARDADO] Partida guardada exitosamente en la base de datos");
            }
            else
            {
                Debug.LogError("[GUARDADO ERROR] Fall� el guardado en la base de datos");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GUARDADO ERROR] Excepci�n inesperada: {ex.Message}");
            Debug.LogError($"Stack Trace: {ex.StackTrace}");
        }
    }

    private void UpdatePlayerStats()
    {
        // 1. Obtener los valores actuales
        int currentHighScore = PlayerPrefs.GetInt("Puntuacion", 0);
        int totalGamesPlayed = PlayerPrefs.GetInt("PartidasJugadas", 0);
        int totalGamesWon = PlayerPrefs.GetInt("PartidasGanadas", 0);

        // 2. Incrementar partidas jugadas
        totalGamesPlayed++;

        // 3. Determinar si el jugador gan�
        bool isWinner = (myScore > enemyScore);

        // 4. Incrementar partidas ganadas si es victoria
        if (isWinner)
        {
            totalGamesWon++;
        }

        // 5. Actualizar puntuaci�n m�xima
        bool newHighScore = false;
        if (myScore > currentHighScore)
        {
            currentHighScore = myScore;
            newHighScore = true;
        }

        // 6. Guardar localmente
        PlayerPrefs.SetInt("Puntuacion", currentHighScore);
        PlayerPrefs.SetInt("PartidasJugadas", totalGamesPlayed);
        PlayerPrefs.SetInt("PartidasGanadas", totalGamesWon);
        PlayerPrefs.Save();

        // 7. Actualizar en DynamoDB (solo si hay cambios importantes)
        ActualizarDatoJugador actualizador = FindObjectOfType<ActualizarDatoJugador>();
        if (actualizador != null)
        {
            actualizador.ActualizarEstadisticasJugador(
                totalGamesPlayed,
                totalGamesWon,
                currentHighScore
            );
        }
        else
        {
            Debug.LogError("No se encontr� el componente ActualizarDatoJugador en la escena");
        }

        Debug.Log($"Estad�sticas actualizadas - Jugadas: {totalGamesPlayed}, Ganadas: {totalGamesWon}, Puntuaci�n M�x: {currentHighScore}");
    }

    

    private string GenerateEndGameText(bool byCompleteCards)
    {
        if (byCompleteCards)
        {
            if (myCardsMatched >= 16 && enemyCardsMatched >= 16)
            {
                return $"�Empate!\nAmbos completaron todas sus cartas.\n{myNicknameText.text}: {myScore} pts\n{enemyNicknameText.text}: {enemyScore} pts";
            }
            else if (myCardsMatched >= 16)
            {
                return $"�Ganaste!\nCompletaste todas tus cartas primero!\n{myNicknameText.text}: {myScore} pts\n{enemyNicknameText.text}: {enemyScore} pts";
            }
            else
            {
                return $"�Perdiste!\n{enemyNicknameText.text} complet� sus cartas primero.\n{myNicknameText.text}: {myScore} pts\n{enemyNicknameText.text}: {enemyScore} pts";
            }
        }
        else
        {
            bool isTie = (myScore == enemyScore);
            bool bothZero = (myScore == 0 && enemyScore == 0);
            bool isWinner = (myScore > enemyScore);

            if (bothZero)
            {
                return $"�Nadie gan�!\nAmbos jugadores tienen 0 puntos.\n{myNicknameText.text}: 0 pts\n{enemyNicknameText.text}: 0 pts";
            }
            else if (isTie)
            {
                return $"�Empate!\n{myNicknameText.text}: {myScore} pts\n{enemyNicknameText.text}: {enemyScore} pts";
            }
            else
            {
                return isWinner ?
                    $"�Ganaste!\n{myNicknameText.text}: {myScore} pts\n{enemyNicknameText.text}: {enemyScore} pts" :
                    $"�Perdiste!\n{myNicknameText.text}: {myScore} pts\n{enemyNicknameText.text}: {enemyScore} pts";
            }
        }
    }

    [PunRPC]
    private void UpdatePlayerScore(int playerNumber, int score, bool isCardMatch)
    {
        if (playerNumber == 1) // Jugador Master
        {
            myScore = score;
            if (isCardMatch) myCardsMatched++;
        }
        else // Jugador Invitado
        {
            enemyScore = score;
            if (isCardMatch) enemyCardsMatched++;
        }

        Debug.Log($"Puntos actualizados - J{playerNumber}: {score} (Cartas: {(isCardMatch ? "S�" : "No")})");

        // Sincronizaci�n inmediata con el master
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("SyncAllScores", RpcTarget.Others, myScore, enemyScore, myCardsMatched, enemyCardsMatched);
        }
    }

    [PunRPC]
    private void SyncAllScores(int masterScore, int guestScore, int masterCards, int guestCards)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            myScore = guestScore;
            enemyScore = masterScore;
            myCardsMatched = guestCards;
            enemyCardsMatched = masterCards;
        }
    }

    [PunRPC]
    private void AcknowledgeEndGame()
    {
        // Master client receives confirmation
        Debug.Log("End game acknowledged by player");
    }

    void UpdateScoresUI()
    {
        myScoreText.text = "Mis puntos: " + myScore;
        enemyScoreText.text = "Puntos: " + enemyScore.ToString();
    }

    void UpdateCardsProgressUI()
    {
        myCardsProgressText.text = $"Mis cartas: {myCardsMatched}/16";
        enemyCardsProgressText.text = $"Cartas rival: {enemyCardsMatched}/16";
    }

    void ShowFeedback(string message, Color color)
    {
        feedbackText.text = message;
        feedbackText.color = color;
        StartCoroutine(HideFeedbackAfterDelay());
    }


    #endregion

    #region Card Animation
    void CreateCard(Sprite faceSprite)
    {
        // Crear contenedor para la carta
        GameObject cardGO = new GameObject("Card", typeof(RectTransform));
        cardGO.transform.SetParent(cardContainer, false);

        RectTransform cardRect = cardGO.GetComponent<RectTransform>();
        cardRect.anchoredPosition = Vector2.zero;
        cardRect.localScale = Vector3.one;
        cardRect.sizeDelta = cardPrefab.rectTransform.sizeDelta;
        cardRect.pivot = new Vector2(0.5f, 0.5f);

        // Crear cara trasera
        Image backHalf = Instantiate(cardPrefab, cardRect);
        backHalf.sprite = backSprite;
        backHalf.rectTransform.anchoredPosition = Vector2.zero;

        // Crear cara frontal
        Image frontHalf = Instantiate(cardPrefab, cardRect);
        frontHalf.sprite = faceSprite;
        frontHalf.rectTransform.anchoredPosition = Vector2.zero;
        frontHalf.rectTransform.localRotation = Quaternion.Euler(0, 180, 0); // Correcci�n de espejo
        frontHalf.gameObject.SetActive(false); // oculta frontal al inicio

        // Asegurar rotaci�n inicial
        cardGO.transform.localRotation = Quaternion.identity;

        // Animaci�n realista de flip
        Sequence cardAnimation = DOTween.Sequence();

        // Movimiento inicial del contenedor
        cardAnimation.Append(cardRect.DOAnchorPos(new Vector2(300, 150), moveDuration))
            .Join(cardRect.DOScale(1.2f, moveDuration).SetEase(Ease.OutBack));

        // Giro realista con rotaci�n Y
        cardAnimation.Append(cardGO.transform.DORotate(new Vector3(0, 90, 0), flipDuration / 2, RotateMode.LocalAxisAdd)
            .SetEase(Ease.InOutQuad))
            .AppendCallback(() => {
                backHalf.gameObject.SetActive(false);
                frontHalf.gameObject.SetActive(true);
            })
            .Append(cardGO.transform.DORotate(new Vector3(0, 90, 0), flipDuration / 2, RotateMode.LocalAxisAdd)
            .SetEase(Ease.InOutQuad));

        // Limpieza
        cardAnimation.OnComplete(() => {
            Destroy(backHalf.gameObject); // dejamos solo la cara visible
            frontHalf.transform.SetParent(cardContainer, true); // liberar del contenedor si quieres
            Destroy(cardGO); // eliminar contenedor si ya no se necesita
            cardInstances.Add(frontHalf); // guardar referencia si lo usas despu�s
        });
    }







    #endregion

    #region Letter Selection
    private IEnumerator SelectLetterCoroutine()
    {
        List<string> availableLetters = new List<string>(abecedario);

        while (!gameEnded && availableLetters.Count > 0)
        {
            yield return new WaitForSeconds(timeBetweenCards);

            if (gameEnded || endGameRequested) yield break;

            int randomIndex = UnityEngine.Random.Range(0, availableLetters.Count);
            string selectedLetter = availableLetters[randomIndex];
            availableLetters.RemoveAt(randomIndex);

            usedLetters.Add(selectedLetter);
            photonView.RPC("UpdateLetter", RpcTarget.All, selectedLetter);
            CheckGameEndCondition();
        }

        if (!gameEnded && !endGameRequested)
        {
            TriggerEndGame(false);
        }
    }



    [PunRPC]
    private void UpdateTimer(float countdown)
    {
        timerCountdown = countdown;
        isTimerRunning = true;
    }
    #endregion

    #region Network Synchronization
    private IEnumerator GameStatusCheck()
    {
        while (!gameEnded)
        {
            yield return new WaitForSeconds(5f);
            if (PhotonNetwork.IsMasterClient && !gameEnded)
            {
                photonView.RPC("VerifyGameStatus", RpcTarget.All,
                    usedLetters.Count,
                    myScore,
                    enemyScore,
                    myCardsMatched,
                    enemyCardsMatched);
            }
        }
    }

    [PunRPC]
    private void VerifyGameStatus(int lettersUsed, int masterScore, int otherScore, int masterCards, int otherCards)
    {
        if (gameEnded) return;

        bool needsResync =
            Math.Abs(lettersUsed - usedLetters.Count) > 1 ||
            Math.Abs(masterScore - (PhotonNetwork.IsMasterClient ? myScore : enemyScore)) > 50 ||
            Math.Abs(masterCards - (PhotonNetwork.IsMasterClient ? myCardsMatched : enemyCardsMatched)) > 1;

        if (needsResync)
        {
            Debug.LogWarning("Game state mismatch detected, requesting resync");
            photonView.RPC("RequestResync", RpcTarget.MasterClient);
        }
    }

    [PunRPC]
    private void RequestResync()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("ForceResync", RpcTarget.All,
                usedLetters.Count,
                myScore,
                enemyScore,
                myCardsMatched,
                enemyCardsMatched,
                currentLetter);
        }
    }

    [PunRPC]
    private void ForceResync(int lettersUsed, int masterScore, int otherScore, int masterCards, int otherCards, string currentLtr)
    {
        usedLetters = new List<string>(lettersUsed);

        if (PhotonNetwork.IsMasterClient)
        {
            myScore = masterScore;
            enemyScore = otherScore;
            myCardsMatched = masterCards;
            enemyCardsMatched = otherCards;
        }
        else
        {
            myScore = otherScore;
            enemyScore = masterScore;
            myCardsMatched = otherCards;
            enemyCardsMatched = masterCards;
        }

        currentLetter = currentLtr;
        gloveLetterText.text = "Letra actual: " + currentLetter.ToUpper();

        UpdateScoresUI();
        UpdateCardsProgressUI();
    }

    [PunRPC]
    private void SendNickname(string nickname)
    {
        enemyNicknameText.text = nickname;
        UpdateScoresUI();
    }

   
    #endregion

    #region Connection Management
    void ConnectToServer()
    {
        try
        {
            tcpClient = new TcpClient(serverIP, serverPort);
            networkStream = tcpClient.GetStream();
            Debug.Log("Connected to glove server");

            isListening = true;
            listenerThread = new Thread(ListenForMessages);
            listenerThread.IsBackground = true;
            listenerThread.Start();
        }
        catch (Exception e)
        {
            Debug.LogError("Connection error: " + e.Message);
            StartReconnection();
        }
    }

    void StartReconnection()
    {
        if (!isReconnecting)
        {
            isReconnecting = true;
            StartCoroutine(Reconnect());
        }
    }

    IEnumerator Reconnect()
    {
        Debug.Log("Reconnecting in " + reconnectionDelay + "s...");
        yield return new WaitForSeconds(reconnectionDelay);
        ConnectToServer();
        isReconnecting = false;
    }

    void ListenForMessages()
    {
        byte[] buffer = new byte[1024];

        while (isListening)
        {
            try
            {
                int bytesRead = networkStream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;

                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Debug.Log("Mensaje del guante: " + message);

                try
                {
                    GloveData data = JsonUtility.FromJson<GloveData>(message);
                    if (data != null && !string.IsNullOrEmpty(data.letra) && data.confianza > 0.80f)
                    {
                        UnityMainThreadDispatcher.Instance().Enqueue(() => {
                            gloveLetterText.text = "Guante: " + data.letra.ToUpper();
                            CheckLetterMatch(data.letra.ToLower());
                        });
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("Error al parsear: " + e.Message);
                }
            }
            catch (Exception e)
            {
                if (isListening) Debug.LogError("Error en recepci�n: " + e.Message);
                break;
            }
        }

        if (isListening)
        {
            StartReconnection();
        }
    }
 

    #endregion

    #region UI Management
    public void ReturnToMenu()
    {
        if (!gameEnded)
        {
            // Si el juego no ha terminado oficialmente, forzar el fin sin UI
            gameEnded = true;
            StopAllCoroutines();

            if (PhotonNetwork.IsMasterClient)
            {
                StartCoroutine(GuardarPartidaCoroutine(false));
            }
        }

        CloseAllConnections();

        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.LeaveRoom();
        }

        StartCoroutine(LoadMenuAfterDelay());
    }

    private IEnumerator LoadMenuAfterDelay()
    {
        // Peque�o delay para asegurar que todo se cierre correctamente
        yield return new WaitForSeconds(0.5f);
        SceneManager.LoadScene("MenuPrincipal");
    }
    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        if (!gameEnded)
        {
            // Mostrar panel de desconexi�n sin mostrar resultados
            disconnectText.text = $"{enemyNicknameText.text} se ha desconectado";
            disconnectPanel.SetActive(true);

            // Forzar fin del juego pero sin mostrar panel de resultados
            gameEnded = true;
            StopAllCoroutines();

            // Solo el master guarda los resultados autom�ticamente
            if (PhotonNetwork.IsMasterClient)
            {
                StartCoroutine(GuardarPartidaCoroutine(false));
            }

            // Deshabilitar el temporizador
            isTimerRunning = false;
            timerText.text = "Partida terminada";
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        // Si nos desconectamos nosotros
        if (!gameEnded)
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() => {
                disconnectText.text = "Te has desconectado del juego";
                disconnectPanel.SetActive(true);
                returnToMenuButton.gameObject.SetActive(true);
            });
        }
    }

    void ShowDisconnectPanel(string message)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() => {
            // Ocultar panel de resultados si est� visible
            endGamePanel.SetActive(false);

            // Mostrar panel de desconexi�n
            disconnectText.text = message;
            disconnectPanel.SetActive(true);

            returnToMenuButton.onClick.RemoveAllListeners();
            returnToMenuButton.onClick.AddListener(ReturnToMenu);
            returnToMenuButton.gameObject.SetActive(true);
        });
    }

    void CloseAllConnections()
    {
        Debug.Log("Cerrando todas las conexiones...");

        // 1. Detener todas las corrutinas
        StopAllCoroutines();

        // 2. Cerrar conexi�n con el guante
        try
        {
            isListening = false;

            if (networkStream != null)
            {
                networkStream.Close();
                networkStream = null;
            }

            if (tcpClient != null)
            {
                tcpClient.Close();
                tcpClient = null;
            }

            if (listenerThread != null && listenerThread.IsAlive)
            {
                listenerThread.Abort();
                listenerThread = null;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("Error al cerrar conexi�n con el guante: " + e.Message);
        }

        // 3. Cerrar conexi�n Photon si est� activa
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
        }

        Debug.Log("Todas las conexiones cerradas");
    }
    #endregion
}



[System.Serializable]
public class GloveData
{
    public string letra;
    public float confianza;
    public double timestamp;
}