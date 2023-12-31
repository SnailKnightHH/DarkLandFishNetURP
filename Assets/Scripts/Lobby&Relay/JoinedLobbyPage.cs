using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Lobbies.Models;
using Unity.Services.Lobbies;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Unity.Services.Authentication;
using Unity.Services.Core;
using System.Threading.Tasks;

public class JoinedLobbyPage : MonoBehaviour
{    
    //private NetworkVariable<uint> NumOfPlayersAlreadyInLobby = new NetworkVariable<uint>(0);

    [SerializeField] private TMP_Text LobbyNameField;
    [SerializeField] private Button StartGameBtn;
    [SerializeField] private List<GameObject> PlayerList;

    public static JoinedLobbyPage Instance { get; private set; }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
            gameObject.SetActive(false);
        }
    }


    private async void OnEnable()
    {

        StartGameBtn.onClick.AddListener(async () =>
        {
            await LobbyManager.Instance.StartGame();            
            gameObject.GetComponent<Canvas>().enabled = false;
        }); 
        // Wait for lobby to be created by lobby manager 
        await Task.Run(async () => { while (LobbyManager.Instance.JoinedLobby == null) await Task.Delay(10); });
        if (!LobbyManager.Instance.IsLobbyHost())
        {
            StartGameBtn.gameObject.SetActive(false);
        }
        LobbyNameField.text = LobbyManager.Instance.JoinedLobby.Name;
        for (int i = 0; i <  LobbyManager.Instance.JoinedLobby.Players.Count(); i++)
        {
            setPlayerNameInLobby(i);
        }
        var callbacks = new LobbyEventCallbacks();
        callbacks.LobbyChanged += OnLobbyChanged;
        await Lobbies.Instance.SubscribeToLobbyEventsAsync(LobbyManager.Instance.JoinedLobby.Id, callbacks);        
    }

    // need to get how many players are already in the lobby and the newly joined player's name

    private async void OnLobbyChanged(ILobbyChanges changes)
    {           
        if (changes.AvailableSlots.Changed)
        {
            await LobbyManager.Instance.PollLobby(); // later update to poll every some seconds with corountine
            int newlyJoinedPlayerIdx = LobbyManager.Instance.JoinedLobby.MaxPlayers - changes.AvailableSlots.Value - 1; // since index is 0 based 
            await Task.Run(async () => { while (LobbyManager.Instance.JoinedLobby.Players.Count < newlyJoinedPlayerIdx + 1) await Task.Delay(10); });
            setPlayerNameInLobby(newlyJoinedPlayerIdx);
        }
    }

    private void setPlayerNameInLobby(int idx)
    {        
        PlayerList[idx].transform.GetChild(1).gameObject.GetComponent<TMP_Text>().text = LobbyManager.Instance.JoinedLobby.Players[idx].Id; // update to name later?
    }

    private void setPlayerIconInLobby()
    {
        throw new NotImplementedException();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
