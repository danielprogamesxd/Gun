using UnityEngine;
using Photon.Pun;
using Photon.Realtime; // Necesario para RoomOptions

public class NetworkManager : MonoBehaviourPunCallbacks // Importante heredar de MonoBehaviourPunCallbacks
{
    public string playerPrefabName = "Player"; // Nombre de tu prefab de jugador en la carpeta Resources
    public Transform spawnPoint; // Punto opcional donde instanciar al jugador

    void Start()
    {
        Debug.Log("Connecting to Master...");
        PhotonNetwork.ConnectUsingSettings(); // Conecta a Photon usando la configuración de PhotonServerSettings
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Master!");
        Debug.Log("Joining Lobby...");
        PhotonNetwork.JoinLobby(); // Unirse al lobby para poder listar salas o unirse a una aleatoria
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("Joined Lobby!");
        Debug.Log("Joining or Creating Room...");
        // Opciones de la sala (puedes personalizarlas más adelante)
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 4; // Ejemplo: máximo 4 jugadores por sala
        roomOptions.IsVisible = true;
        roomOptions.IsOpen = true;

        PhotonNetwork.JoinOrCreateRoom("DefaultRoom", roomOptions, TypedLobby.Default);
        // "DefaultRoom" es el nombre de la sala. Si no existe, se crea.
        // Si existe y no está llena, se une.
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined Room: " + PhotonNetwork.CurrentRoom.Name);
        Debug.Log("Players in room: " + PhotonNetwork.CurrentRoom.PlayerCount);

        // Instanciar el prefab del jugador
        // Asegúrate de que tu prefab "Player" esté en una carpeta llamada "Resources"
        // dentro de tu carpeta "Assets".
        // Ejemplo: Assets/Resources/Player.prefab

        if (string.IsNullOrEmpty(playerPrefabName))
        {
            Debug.LogError("Player Prefab Name is not set in NetworkManager.");
            return;
        }

        Vector3 positionToSpawn = Vector3.zero; // Posición por defecto
        Quaternion rotationToSpawn = Quaternion.identity; // Rotación por defecto

        if (spawnPoint != null)
        {
            positionToSpawn = spawnPoint.position;
            rotationToSpawn = spawnPoint.rotation;
        }
        else
        {
            // Si no hay spawn point, instanciar en una posición aleatoria simple o fija
            // para evitar que todos aparezcan exactamente en el mismo sitio.
            positionToSpawn = new Vector3(Random.Range(-5f, 5f), 0.5f, Random.Range(-5f, 5f));
        }

        PhotonNetwork.Instantiate(playerPrefabName, positionToSpawn, rotationToSpawn);
        Debug.Log("Player instantiated.");
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError("Join Room Failed: " + message + " (Return Code: " + returnCode + ")");
        // Podrías intentar crear una sala con un nombre diferente o reintentar
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError("Create Room Failed: " + message + " (Return Code: " + returnCode + ")");
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarningFormat("Disconnected from Photon: {0}", cause);
    }

    // Opcional: Callbacks para cuando otros jugadores entran o salen
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.LogFormat("Player {0} entered room", newPlayer.NickName);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.LogFormat("Player {0} left room", otherPlayer.NickName);
    }
}
