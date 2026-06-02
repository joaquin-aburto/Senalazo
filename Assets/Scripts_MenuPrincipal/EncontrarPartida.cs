using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;

public class EncontrarPartida : MonoBehaviourPunCallbacks
{
    public GameObject panelBuscando;
    public TextMeshProUGUI textoEstado;
    public Button botonCancelar;
    public Button botonMenuPrincipal;

    private bool cancelando = false;
    private float tiempoUltimaCancelacion = 0f;
    private const float TIEMPO_ESPERA_RECONEXION = 1.5f;

    void Start()
    {
        panelBuscando.SetActive(false);
        botonCancelar.gameObject.SetActive(false);
        botonCancelar.onClick.AddListener(CancelarBusqueda);
    }

    public void IniciarBusqueda()
    {
        if (panelBuscando == null || textoEstado == null || botonCancelar == null)
        {
            Debug.LogError("Faltan asignar referencias en el Inspector!");
            return;
        }

        panelBuscando.SetActive(true);
        botonCancelar.gameObject.SetActive(true);
        botonMenuPrincipal.interactable = false;

        textoEstado.text = "Buscando partida...";
        cancelando = false;

        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
            return;
        }

        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
        }
        else
        {
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public void CancelarBusqueda()
    {
        if (cancelando) return;

        cancelando = true;
        textoEstado.text = "Cancelando...";
        botonCancelar.interactable = false;
        botonMenuPrincipal.interactable = true;

        tiempoUltimaCancelacion = Time.time;

        if (PhotonNetwork.InRoom)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.CurrentRoom.IsVisible = false;
                PhotonNetwork.CurrentRoom.IsOpen = false;
            }
            PhotonNetwork.LeaveRoom();
        }
        else if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
        }
        else
        {
            CancelacionCompletada();
        }
    }

    public override void OnConnectedToMaster()
    {
        if (cancelando) return;

        if (!PhotonNetwork.InLobby)
        {
            PhotonNetwork.JoinLobby();
            return;
        }

        BuscarSalasExistentes();
    }

    private void BuscarSalasExistentes()
    {
        textoEstado.text = "Buscando partidas disponibles...";
        PhotonNetwork.GetCustomRoomList(PhotonNetwork.CurrentLobby, "IsOpen EQ true AND MaxPlayers EQ 2");
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        if (cancelando) return;

        foreach (RoomInfo room in roomList)
        {
            if (room.PlayerCount == 1 && room.IsOpen && room.MaxPlayers == 2)
            {
                PhotonNetwork.JoinRoom(room.Name);
                return;
            }
        }

        CrearNuevaSala();
    }

    private void CrearNuevaSala()
    {
        RoomOptions options = new RoomOptions
        {
            MaxPlayers = 2,
            IsVisible = true,
            IsOpen = true,
            CleanupCacheOnLeave = true,
            EmptyRoomTtl = 0
        };

        textoEstado.text = "Creando nueva partida...";
        PhotonNetwork.CreateRoom(null, options);
    }

    public override void OnJoinedLobby()
    {
        if (cancelando) return;
        BuscarSalasExistentes();
    }

    public override void OnJoinedRoom()
    {
        if (cancelando)
        {
            PhotonNetwork.LeaveRoom();
            return;
        }

        textoEstado.text = PhotonNetwork.IsMasterClient ?
            "Esperando otro jugador..." : "Uniéndose a partida...";

        if (PhotonNetwork.CurrentRoom.PlayerCount == 2)
        {
            IniciarJuego();
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (cancelando) return;

        if (PhotonNetwork.CurrentRoom.PlayerCount == 2)
        {
            PhotonNetwork.CurrentRoom.IsOpen = false;
            IniciarJuego();
        }
    }

    private void IniciarJuego()
    {
        textoEstado.text = "¡Partida encontrada! Iniciando juego...";
        SceneManager.LoadScene("Loteria");
    }

    public override void OnLeftRoom()
    {
        if (cancelando)
        {
            PhotonNetwork.Disconnect();
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        if (cancelando)
        {
            CancelacionCompletada();
        }
        else if (cause != DisconnectCause.DisconnectByClientLogic)
        {
            textoEstado.text = ObtenerMensajeErrorDesconexion(cause);
            StartCoroutine(ReconectarDespuesDeError());
        }
    }

    private string ObtenerMensajeErrorDesconexion(DisconnectCause cause)
    {
        switch (cause)
        {
            case DisconnectCause.Exception:
            case DisconnectCause.ExceptionOnConnect:
                return "Error de conexión con el servidor";
            case DisconnectCause.ServerTimeout:
                return "Tiempo de espera agotado. Reintentando...";
            case DisconnectCause.ClientTimeout:
                return "Conexión lenta. Verifica tu internet";
            case DisconnectCause.DisconnectByServerReasonUnknown:
                return "Desconectado por el servidor";
            case DisconnectCause.InvalidAuthentication:
                return "Error de autenticación";
            case DisconnectCause.MaxCcuReached:
                return "Servidor lleno. Intenta más tarde";
            case DisconnectCause.InvalidRegion:
                return "Región no válida";
            case DisconnectCause.OperationNotAllowedInCurrentState:
                return "Operación no permitida";
            default:
                return "Error de conexión. Reintentando...";
        }
    }

    private IEnumerator ReconectarDespuesDeError()
    {
        yield return new WaitForSeconds(2f);
        if (!cancelando && !PhotonNetwork.IsConnected)
        {
            textoEstado.text = "Reconectando...";
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    private void CancelacionCompletada()
    {
        textoEstado.text = "Búsqueda cancelada";
        StartCoroutine(OcultarPanel());
    }

    IEnumerator OcultarPanel()
    {
        yield return new WaitForSeconds(1.5f);
        panelBuscando.SetActive(false);
        botonCancelar.gameObject.SetActive(false);
        botonCancelar.interactable = true;
        cancelando = false;
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        if (cancelando)
        {
            PhotonNetwork.Disconnect();
        }
        else
        {
            textoEstado.text = "No se pudo crear la partida. Intenta nuevamente";
            StartCoroutine(ReconectarDespuesDeError());
        }
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        if (cancelando) return;
        textoEstado.text = "No se pudo unir a la partida. Buscando otra...";
        StartCoroutine(ReconectarDespuesDeError());
    }
}