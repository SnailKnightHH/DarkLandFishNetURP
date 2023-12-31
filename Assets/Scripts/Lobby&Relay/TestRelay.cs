using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Networking.Transport.Relay;
using UnityEngine;
using System.Threading.Tasks;
using FishNet.Managing;
using FishNet.Transporting.UTP;

public class TestRelay : MonoBehaviour
{
    [SerializeField] private NetworkManager _networkManager;

    public static TestRelay Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    public async Task<string> CreateRelay()
    {
        try
        {
            var utp = (FishyUnityTransport)_networkManager.TransportManager.Transport;

            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(8);

            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

#if UNITY_EDITOR
            Debug.Log(joinCode);
#endif
            RelayServerData relayServerData = new RelayServerData(allocation, "dtls");
            utp.SetRelayServerData(relayServerData);

            _networkManager.ServerManager.StartConnection();
            _networkManager.ClientManager.StartConnection();
            return joinCode;
        }
        catch (RelayServiceException e) { Debug.Log(e); }
        return null;
    }

    public async void JoinRelay(string joinCode)
    {
        try
        {
            var utp = (FishyUnityTransport)_networkManager.TransportManager.Transport;
#if UNITY_EDITOR
            Debug.Log("Joining Relay with " + joinCode);
#endif
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            RelayServerData relayServerData = new RelayServerData(joinAllocation, "dtls");

            utp.SetRelayServerData(relayServerData);

            _networkManager.ClientManager.StartConnection();
        }
        catch (RelayServiceException ex) { Debug.Log(ex); }
    }
}
