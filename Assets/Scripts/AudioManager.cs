using FishNet.Managing;
using FishNet.Object;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
class SoundEnumToClipMapping
{
    public AudioManager.SoundName SoundName;
    public AudioClip[] audioClip;
}

public class AudioManager : NetworkBehaviour
{
    public static AudioManager Instance { get; private set; }

    public enum SoundName
    {
        Walk,
        Run,
        Jump,
        pistol
    }

    public enum SoundType
    {
        Discrete,
        Continuous
    }

    [SerializeField] private List<SoundEnumToClipMapping> SoundNameToAudioClipMapping;
    private Dictionary<SoundName, AudioClip[]> soundNameToAudioClipDict = new Dictionary<SoundName, AudioClip[]>();
    private Dictionary<int, Dictionary<SoundName, float>> soundTimerDict = new Dictionary<int, Dictionary<SoundName, float>>();
    private Dictionary<int, Dictionary<SoundName, bool>> keepPlayingSoundDict = new Dictionary<int, Dictionary<SoundName, bool>>();

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

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        foreach (SoundEnumToClipMapping kvp in SoundNameToAudioClipMapping)
        {
            soundNameToAudioClipDict.Add(kvp.SoundName, kvp.audioClip);
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        foreach (var kvp in NetworkManager.ClientManager.Clients)
        {
            int ClientId = kvp.Value.ClientId;
            SoundDictInitialize(ClientId);
        }
        int myClientId = NetworkManager.ClientManager.Connection.ClientId;
        SoundDictInitialize(myClientId);
        SoundDictInitializationServerRpc(myClientId);
    }

    private void SoundDictInitialize(int ClientId)
    {
        soundTimerDict[ClientId] = new Dictionary<SoundName, float>();
        soundTimerDict[ClientId][SoundName.Walk] = 0;
        soundTimerDict[ClientId][SoundName.Run] = 0;
        keepPlayingSoundDict[ClientId] = new Dictionary<SoundName, bool>();
        keepPlayingSoundDict[ClientId][SoundName.Walk] = false;
        keepPlayingSoundDict[ClientId][SoundName.Run] = false;
    }

    [ServerRpc(RequireOwnership = false)]
    private void SoundDictInitializationServerRpc(int ClientId)
    {
        SoundDictInitializationClientRpc(ClientId);
    }

    [ObserversRpc(ExcludeOwner = true)]
    private void SoundDictInitializationClientRpc(int ClientId)
    {
        SoundDictInitialize(ClientId);
    }

    private AudioClip GetRandomAudioClip(SoundName soundName)
    {
        return soundNameToAudioClipDict[soundName][UnityEngine.Random.Range(0, soundNameToAudioClipDict[soundName].Length)];
    }

    public void PlayAudioContinuousLocal(AudioSource audioSource, SoundName soundName, int clientId)
    {
        if (CanPlaySound(soundName, clientId))
        {
            audioSource.PlayOneShot(GetRandomAudioClip(soundName));
        }
    }

    /// <summary>
    /// Only takes care of syncing the sound track through the network. Call PlayAudioContinuousLocal() to play for local player.
    /// </summary>
    /// <param name="networkObject">NetworkObject component attached to this GO.</param>
    /// <param name="soundName">Enum sound name of the clip.</param>
    /// <param name="isPlaying">True to play, false to stop.</param>
    /// <param name="clientId">This connection's id</param>
    public void PlayAudioContinuousNetwork(NetworkObject networkObject, SoundName soundName, bool isPlaying, int clientId)
    {
        if (IsServer || IsHost)
        {
            keepPlayingSoundDict[clientId][soundName] = isPlaying;
#if UNITY_EDITOR
            Debug.Log(networkObject.LocalConnection.ClientId + "executed continuous sound " + soundName + " , bool: " + isPlaying);
#endif
            UpdatePlayerIsPlayingSoundStatusClientRpc(isPlaying, soundName, networkObject, clientId);
        }
        else
        {
            UpdatePlayerIsPlayingSoundStatusServerRpc(isPlaying, soundName, networkObject, clientId);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void UpdatePlayerIsPlayingSoundStatusServerRpc(bool isPlaying, SoundName soundName, NetworkObject networkObject, int clientId)
    {
        keepPlayingSoundDict[clientId][soundName] = isPlaying;
#if UNITY_EDITOR
        Debug.Log(networkObject.LocalConnection.ClientId + "executed continuous sound " + soundName + " , bool: " + isPlaying);
#endif
        StartCoroutine(KeepPlayingSound(networkObject.GetComponentInChildren<AudioSource>(), soundName, clientId, () => keepPlayingSoundDict[clientId][soundName]));
        UpdatePlayerIsPlayingSoundStatusClientRpc(isPlaying, soundName, networkObject, clientId);
    }

    [ObserversRpc(ExcludeServer = true)]
    private void UpdatePlayerIsPlayingSoundStatusClientRpc(bool isPlaying, SoundName soundName, NetworkObject networkObject, int clientId)
    {
        if (clientId == NetworkManager.ClientManager.Connection.ClientId) { return; } // exclude the client who initiates the call since the player would have called PlayAudioContinuousLocal()
        keepPlayingSoundDict[clientId][soundName] = isPlaying;
#if UNITY_EDITOR
        Debug.Log(networkObject.LocalConnection.ClientId + "executed continuous sound " + soundName + " , bool: " + isPlaying);
#endif

        StartCoroutine(KeepPlayingSound(networkObject.GetComponentInChildren<AudioSource>(), soundName, clientId, () => keepPlayingSoundDict[clientId][soundName]));
    }

    // A coroutine is used instead of a plain function since otherwise this thread will just hang. Coroutine spawns a new thread, and will be automatically destroyed after coroutine function exits (I think).
    private IEnumerator KeepPlayingSound(AudioSource audioSource, SoundName soundName, int clientId, Func<bool> ifKeepPlaying)
    {
        while (ifKeepPlaying())
        {
            if (CanPlaySound(soundName, clientId))
            {
                audioSource.PlayOneShot(GetRandomAudioClip(soundName));
            }
            yield return null;
        }
    }


    public void PlayAudioDiscrete(NetworkObject networkObject, SoundName soundName)
    {
        if (IsServer || IsHost)
        {
            networkObject.GetComponentInChildren<AudioSource>().PlayOneShot(GetRandomAudioClip(soundName));
            PlayDiscreteSoundClientRpc(soundName, networkObject);
        }
        else
        {
            PlayDiscreteSoundServerRpc(soundName, networkObject);
        }   
    }

    private bool CanPlaySound(SoundName soundName, int clientId)
    {
        float lastTimePlayed;
        if (soundName == SoundName.Walk)
        {
            if (soundTimerDict[clientId].TryGetValue(soundName, out lastTimePlayed))
            {
                float playerMoveTimeInterval = 0.5f;
                if (lastTimePlayed + playerMoveTimeInterval < Time.time)
                {
                    soundTimerDict[clientId][SoundName.Walk] = Time.time;
                    return true;
                }
                else
                {
                    return false;
                }
            }
#if UNITY_EDITOR
            Debug.LogError("Sound Enum not found. This should not happen");
#endif
            return false;
        } else if (soundName == SoundName.Run)
        {
            if (soundTimerDict[clientId].TryGetValue(soundName, out lastTimePlayed))
            {
                float playerMoveTimeInterval = 0.2f;
                if (lastTimePlayed + playerMoveTimeInterval < Time.time)
                {
                    soundTimerDict[clientId][SoundName.Run] = Time.time;
                    return true;
                }
                else
                {
                    return false;
                }
            }
#if UNITY_EDITOR
            Debug.LogError("Sound Enum not found. This should not happen");
#endif
            return false;
        } else
        {
            return true;
        }
    }

    [ServerRpc(RequireOwnership = false, RunLocally = true)]
    private void PlayDiscreteSoundServerRpc(SoundName soundName, NetworkObject networkObject)
    {
#if UNITY_EDITOR
        Debug.Log(networkObject.LocalConnection.ClientId + "executed discrete sound " + soundName);
#endif
        networkObject.GetComponentInChildren<AudioSource>().PlayOneShot(GetRandomAudioClip(soundName));
        PlayDiscreteSoundClientRpc(soundName, networkObject);
    }

    [ObserversRpc(ExcludeServer = true, ExcludeOwner = true, BufferLast = true)]
    private void PlayDiscreteSoundClientRpc(SoundName soundName, NetworkObject networkObject) // Todo: I think initiating client still runs this, so sound played twice
    {
#if UNITY_EDITOR
        Debug.Log(networkObject.LocalConnection.ClientId + "executed discrete sound " + soundName);
#endif
        networkObject.GetComponentInChildren<AudioSource>().PlayOneShot(GetRandomAudioClip(soundName));
    }

    void Update()
    {

    }
}
