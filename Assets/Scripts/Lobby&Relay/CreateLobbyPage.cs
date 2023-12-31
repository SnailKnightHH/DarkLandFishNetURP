using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CreateLobbyPage : MonoBehaviour
{
    [SerializeField] private TMP_InputField lobbyNameField;
    [SerializeField] private TMP_Dropdown numOfPlayersDropDown;
    [SerializeField] private Button createLobbyBtn;
    [SerializeField] private Button backBtn;

    public static CreateLobbyPage Instance { get; private set; }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
            gameObject.GetComponent<Canvas>().enabled = false;
        }
    }


    // Start is called before the first frame update
    void OnEnable()
    {
        List<string> options = new List<string> { "1", "2", "3", "4", "5", "6", "7", "8" };
        numOfPlayersDropDown.AddOptions(options);
        createLobbyBtn.onClick.AddListener(async () =>
        {
            if (lobbyNameField != null 
            && lobbyNameField.text != "" 
            && numOfPlayersDropDown != null 
            && numOfPlayersDropDown.value >= 0  // drop down value is 0 indexed
            && numOfPlayersDropDown.value + 1 <= LobbyManager.MAX_NUM_OF_PLAYERS) // drop down value is 0 indexed
            {
                await LobbyManager.Instance.CreateLobby(lobbyNameField.text, numOfPlayersDropDown.value + 1);
                
                JoinedLobbyPage.Instance.gameObject.SetActive(true);
                gameObject.GetComponent<Canvas>().enabled = false;
            }            
        });
        backBtn.onClick.AddListener(() =>
        {
            LobbiesList.Instance.gameObject.GetComponent<Canvas>().enabled = true;
            gameObject.GetComponent<Canvas>().enabled = false;
        });
    }

    // Update is called once per frame
    void Update()
    {

    }
}
