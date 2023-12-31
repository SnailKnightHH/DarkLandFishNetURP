//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.UI;
//using Unity.Netcode;
//using TMPro;

//public class NetworkManagerUI : MonoBehaviour
//{
//    [SerializeField] private Button serverBtn;
//    [SerializeField] private Button hostBtn;
//    [SerializeField] private Button clientBtn;
//    [SerializeField] private TMP_Text joinCodeText;
//    [SerializeField] private TMP_InputField joinCodeInput;

//    private TestRelay testRelay;

//    private void Awake()
//    {
//        testRelay = FindObjectOfType<TestRelay>();

//        serverBtn.onClick.AddListener(() =>
//        {
//            NetworkManager.Singleton.StartServer();
//        });
//        hostBtn.onClick.AddListener(async () =>
//        {
//            string joinCode = await testRelay.CreateRelay();
//            joinCodeText.text = joinCode;
//        });
//        clientBtn.onClick.AddListener(() =>
//        {
//            string joinCode = joinCodeInput.text;
//            Debug.Log("Joining room with code: " + joinCode);
//            testRelay.JoinRelay(joinCode);
//        });
//    }
//}
