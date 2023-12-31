using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbiesList : MonoBehaviour
{
    private Transform LobbyListContainer;
    private Transform LobbyEntryTemplate;
    [SerializeField] private Button RefreshBtn;
    [SerializeField] private Button CreateLobbyBtn;
    [SerializeField] private float templateHeight = 1;
    private List<string> joinCodes;

    public static LobbiesList Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        } else
        {
            Instance = this;
        }
    }

    // Start is called before the first frame update
    void OnEnable()
    {
        LobbyListContainer = transform.Find("LobbyListContainer");
        LobbyEntryTemplate = transform.Find("LobbyEntryTemplate");
        RefreshBtn.onClick.AddListener(() =>
        {
            RefreshLobbies();
        });
        CreateLobbyBtn.onClick.AddListener(() =>
        {
            gameObject.GetComponent<Canvas>().enabled = false;
            CreateLobbyPage.Instance.gameObject.GetComponent<Canvas>().enabled = true;
        });        
    }

    // Update is called once per frame
    void Update()
    {

        
    }

    private async void RefreshLobbies()
    {
        List<Lobby> lobbies = await LobbyManager.Instance.QueryLobbies();
        if (lobbies == null) return;
        for (int i = 0; i < LobbyListContainer.childCount; i++)
        {
            Destroy(LobbyListContainer.GetChild(i).gameObject);
        }
        joinCodes = new List<string>();
        for (int i = 0; i < lobbies.Count; i++)
        {
            Transform newEntry = Instantiate(LobbyEntryTemplate, LobbyListContainer);
            RectTransform newEntryRectTransform = newEntry.GetComponent<RectTransform>();
            newEntryRectTransform.anchoredPosition = new Vector2(0, -templateHeight * i);
            joinCodes.Add(lobbies[i].Id);
            newEntryRectTransform.Find("Index").GetComponent<TMP_Text>().text = (i + 1).ToString();
            newEntryRectTransform.Find("LobbyName").GetComponent<TMP_Text>().text = lobbies[i].Name;            
            newEntryRectTransform.Find("Players").GetComponent<TMP_Text>().text = lobbies[i].Players.Count + " / " + lobbies[i].MaxPlayers;
            int idx = i;
            newEntryRectTransform.Find("EnterBtn").GetComponent<Button>().onClick.AddListener(() =>
            {            
                LobbyManager.Instance.JoinLobbyById(joinCodes[idx]);
                JoinedLobbyPage.Instance.gameObject.SetActive(true);
                gameObject.GetComponent<Canvas>().enabled = false;
            });
            newEntry.gameObject.SetActive(true);
        }
    }
}
