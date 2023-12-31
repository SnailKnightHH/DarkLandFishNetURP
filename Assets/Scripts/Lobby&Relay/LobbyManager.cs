using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.VisualScripting;
using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    public const int MAX_NUM_OF_PLAYERS = 8;
    private const string RELAY_CODE = "RelayCode";

    public static LobbyManager Instance { get; private set; }

    private Lobby _joinedLobby;
    public Lobby JoinedLobby
    {
        get
        {
            return _joinedLobby;
        }
    }

    private async void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
            string playerName = "Tester" + Random.Range(0, 1000);
            InitializationOptions initializationOptions = new InitializationOptions();
            initializationOptions.SetProfile(playerName);
            await UnityServices.InitializeAsync(initializationOptions);

            AuthenticationService.Instance.SignedIn += () =>
            {
#if UNITY_EDITOR
                Debug.Log("Signed in " + AuthenticationService.Instance.PlayerId);
#endif
            };
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    async void Update()
    {
        await CheckCanEnterGame();
    }

    public async Task CreateLobby(string lobbyName, int maxPlayers, bool IsPrivate = false, string GameMode = "empty", string DefaultAgent = "empty")
    {
        if (_joinedLobby != null)
        {
            return;
        }
        CreateLobbyOptions options = new CreateLobbyOptions();
        options.IsPrivate = IsPrivate;
        options.Data = new Dictionary<string, DataObject>()
        {
            {
                "GameMode", new DataObject(
                    visibility: DataObject.VisibilityOptions.Public,
                    value: GameMode,
                    index: DataObject.IndexOptions.S1)
            },
            {
                RELAY_CODE, new DataObject(
                    visibility: DataObject.VisibilityOptions.Member,
                    value: ""
                )
            }
        };

        options.Player = new Unity.Services.Lobbies.Models.Player(
            id: AuthenticationService.Instance.PlayerId,
            data: new Dictionary<string, PlayerDataObject>()
            {
                {
                    "PlayerAgent", new PlayerDataObject(
                        PlayerDataObject.VisibilityOptions.Member, 
                        value: DefaultAgent)
                }
            }
        );

        try
        {
            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
            StartCoroutine(HeartBeatLobbyCoroutine(lobby.Id, 15));
            _joinedLobby = lobby;
#if UNITY_EDITOR
            Debug.Log("Lobby successfully created with name and id: " + lobby.Name + " " + lobby.Id);
#endif
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }        
    }

    IEnumerator HeartBeatLobbyCoroutine(string lobbyId, float waitTimeSeconds)
    {
        var delay = new WaitForSecondsRealtime(waitTimeSeconds);

        while (true)
        {
            LobbyService.Instance.SendHeartbeatPingAsync(lobbyId);
            yield return delay;
        }
    }

    public async void DeleteLobby()
    {
        if (_joinedLobby == null)
        {
#if UNITY_EDITOR
            Debug.Log("Player has not entered any lobby");
#endif
            return;
        }
        try
        {
            await LobbyService.Instance.DeleteLobbyAsync(_joinedLobby.Id);
        } catch (LobbyServiceException e)
        {
            Debug.Log(e);
        } 
    }

    public async Task<List<Lobby>> QueryLobbies(QueryLobbiesOptions options = null)
    {
#if UNITY_EDITOR
        Debug.Log("Entered query lobbies method");
#endif
        try
        {
            QueryResponse response = await Lobbies.Instance.QueryLobbiesAsync(options);
            List<Lobby> lobbies = response.Results;

            //// A continuation token will still be returned when the next page is empty,
            //// so continue paging until there are no new lobbies in the response
            //while (lobbies.Count > 0)
            //{
            //    // Do something here with the lobbies in the current page

            //    // Get the next page. Be careful not to modify the filter or order in the
            //    // query options, as this will return an error
            //    options.ContinuationToken = response.ContinuationToken;
            //    response = await LobbyService.Instance.QueryLobbiesAsync(options);
            //    lobbies = response.Results;
            //}

            return lobbies;
        } catch (LobbyServiceException e)
        {
            Debug.Log(e);
            return null;
        }
        
    }

    public QueryLobbiesOptions ConstructQueryOptions()
    { // UI canvas should be able to configure query options using this method, used in QueryLobbies()
        return null;
    }

    public async void JoinLobbyById(string LobbyId)
    {
        _joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(LobbyId);        
    }

    public async void JoinLobbyByCode(string LobbyCode)
    {
        _joinedLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(LobbyCode);
    }

    public async void QuickJoin(QuickJoinLobbyOptions options)
    {
        try
        {
            var lobby = await LobbyService.Instance.QuickJoinLobbyAsync(options);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
            // Can try to fall back to call this method again
        }
    }

    public async Task RemovePlayerFromLobby(string playerId)
    {
        try
        {
            await LobbyService.Instance.RemovePlayerAsync(_joinedLobby.Id, playerId);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }        
    }

    public async void LeaveLobby()
    {
        try
        {
            string playerId = AuthenticationService.Instance.PlayerId;
            await RemovePlayerFromLobby(playerId);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    public bool IsLobbyHost()
    {
        return AuthenticationService.Instance.PlayerId == JoinedLobby.HostId;
    }

    public async Task PollLobby()
    {
        try
        {
            _joinedLobby = await LobbyService.Instance.GetLobbyAsync(_joinedLobby.Id);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    private float lobbyUpdateTimer = 0;
    private bool isEntering = false;
    private async Task CheckCanEnterGame()
    {
        if (_joinedLobby != null && !isEntering)
        {
            lobbyUpdateTimer -= Time.deltaTime;
            if (lobbyUpdateTimer < 0f)
            {
                float lobbyUpdateTimerMax = 1.1f;
                lobbyUpdateTimer = lobbyUpdateTimerMax;
                await PollLobby();
            }
            if (_joinedLobby.Data[RELAY_CODE].Value != "" && !IsLobbyHost())
            {
                isEntering = true;
                TestRelay.Instance.JoinRelay(_joinedLobby.Data[RELAY_CODE].Value);
                JoinedLobbyPage.Instance.gameObject.SetActive(false);
            }
        }
    }

    public async Task StartGame()
    {
        if(IsLobbyHost())
        {
            try
            {
                string relayCode = await TestRelay.Instance.CreateRelay();
                Lobby lobby = await Lobbies.Instance.UpdateLobbyAsync(_joinedLobby.Id, new UpdateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject>
                    {
                        { RELAY_CODE, new DataObject(DataObject.VisibilityOptions.Member, relayCode)}
                    }
                });
                _joinedLobby = lobby;
            } catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }
    }

    //private void OnApplicationQuit()
    //{
    //    if (_joinedLobby != null)
    //    {
    //        LobbyService.Instance.DeleteLobbyAsync(_joinedLobby.Id);
    //        _joinedLobby = null;
    //    }        
    //}
}
