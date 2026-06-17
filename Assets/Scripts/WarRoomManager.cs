using System;
using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WarRoomManager : NetworkBehaviour
{
    public static WarRoomManager Instance;

    [Header("Setari Vot")] 
    public float durataVot = 30f;

    public NetworkVariable<bool> votInCurs = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> butonActiv = new NetworkVariable<bool>(false);

    private List<ulong> jucatoriCareAuVotat = new List<ulong>();
    private int voturiDa = 0;
    private int voturiNu = 0;

    [Header("Efecte Vizuale Buton")] public Renderer rendererButon;
    public Color culoareInactiv = new Color(0.3f, 0f, 0f, 1f);

    [ColorUsage(true, true)] public Color culoareActiv = Color.red;

    private float timpRamasVot;
    private int ultimulTimpTrimis;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void Start()
    {
        if (rendererButon != null && !butonActiv.Value)
        {
            rendererButon.material.SetColor("_BaseColor", culoareInactiv);
            rendererButon.material.SetColor("_EmissionColor", Color.black);
        }
    }

    private void Update()
    {
        if (!IsServer || !votInCurs.Value)
        {
            return;
        }

        timpRamasVot -= Time.unscaledDeltaTime;
        
        int secundeRamas = Mathf.CeilToInt(timpRamasVot);
        if (secundeRamas != ultimulTimpTrimis && secundeRamas >= 0)
        {
            ultimulTimpTrimis = secundeRamas;
            ActualizeazaTimpRamasClientRpc(timpRamasVot);
        }
        
        if(secundeRamas <= 0)
        {
            EvalueazaRezultatVot();
        }
    }

    public void ActiveazaButon()
    {
        if (IsServer)
        {
            butonActiv.Value = true;
            SchimbaCuloareButonClientRpc(true);
        }
    }

    public void IncepeVotul()
    {
        if (!butonActiv.Value || votInCurs.Value)
        {
            return;
        }

        IncepeVotServerRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void IncepeVotServerRpc()
    {
        if(votInCurs.Value)
        {
            return;
        }
        
        votInCurs.Value = true;
        jucatoriCareAuVotat.Clear();
        voturiDa = 0;
        voturiNu = 0;

        timpRamasVot = durataVot;
        ultimulTimpTrimis = Mathf.FloorToInt(durataVot);
        DeschideMeniuVotClientRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void InregistreazaVotServerRpc(bool votDa, RpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        
        if (!votInCurs.Value || jucatoriCareAuVotat.Contains(clientId))
        {
            return;
        }
        
        jucatoriCareAuVotat.Add(clientId);

        if (votDa)
        {
            voturiDa++;
        }
        else
        {
            voturiNu++;
        }
        
        ActualizeazaScorVotClientRpc(voturiDa, voturiNu);
        
        if (GameSessionManager.Instance != null)
        {
            int maxJucatori = GameSessionManager.Instance.nrMaxJucatori.Value;
            
            if(voturiDa >= (maxJucatori + 1) / 2 || (voturiDa + voturiNu) >= maxJucatori)
            {
                EvalueazaRezultatVot();
            }
        }
    }
    
    private void EvalueazaRezultatVot()
    {
        votInCurs.Value = false;
        InchideMeniuVotClientRpc();
        
        int voturiNecesare = 5; // Default de siguranță
        if (GameSessionManager.Instance != null)
        {
            voturiNecesare = (GameSessionManager.Instance.nrMaxJucatori.Value + 1) / 2;
        }
        
        if (voturiDa >= voturiNecesare)
        {
            Debug.Log("Vot aprobat!");
            
            if (FinalAttackManager.Instance != null)
            {
                FinalAttackManager.Instance.DeclanseazaFazaFinala();
            }
        }
        else
        {
            Debug.Log("Vot respins!");
        }
    }

    [ClientRpc]
    private void SchimbaCuloareButonClientRpc(bool activ)
    {
        if (rendererButon != null)
        {
            Color culoareaAleasa = activ ? culoareActiv : culoareInactiv;
            
            rendererButon.material.SetColor("_BaseColor", culoareaAleasa);
            
            if (activ)
            {
                rendererButon.material.EnableKeyword("_EMISSION");
                rendererButon.material.SetColor("_EmissionColor", culoareaAleasa);
            }
            else
            {
                rendererButon.material.SetColor("_EmissionColor", Color.black);
            }
        }
    }
    
    [ClientRpc]
    private void DeschideMeniuVotClientRpc()
    {
        Time.timeScale = 0f;
        UIManager.Instance.ArataPanouVot(true);
    }
    
    [ClientRpc]
    private void InchideMeniuVotClientRpc()
    {
        Time.timeScale = 1f;
        UIManager.Instance.ArataPanouVot(false);
    }

    [ClientRpc]
    private void ActualizeazaTimpRamasClientRpc(float timpRamas)
    {
        UIManager.Instance.ActualizeazaTextTimpVot(timpRamas);
    }
    
    [ClientRpc]
    private void ActualizeazaScorVotClientRpc(int voturiDa, int voturiNu)
    {       
        UIManager.Instance.ActualizeazaScorVot(voturiDa, voturiNu);
    }
    

}
