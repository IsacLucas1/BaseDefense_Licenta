using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;

public class BazaInamicaManager : NetworkBehaviour
{
    public static BazaInamicaManager Instance { get; private set; }
    
    [Header("Inamici (pre-plasati in scena)")]
    public List<InamiciBazaFinala> inamiciInterior = new List<InamiciBazaFinala>();
    public List<InamiciBazaFinala> inamiciExterior = new List<InamiciBazaFinala>();
    
    [Header("Poarta")]
    public Health poartaHealth;
    public Renderer poartaRenderer;
    public Collider poartaCollider;
    public NavMeshObstacle poartaObstacle;
    public int viataPoarta = 300;
    public Canvas poartaHealthCanvas;
    
    private bool poartaSparta = false;
    private bool poartaInitializata = false;
    
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
    
    public void AparitieInamiciInBaza()
    {
        Debug.Log($"[Baza] AparitieInamiciInBaza, interiori in lista={inamiciInterior.Count}");
        
        if (!IsServer)
        {
            return;
        }
        
        foreach (var inamic in inamiciInterior)
        {
            if (inamic != null)
            {
                inamic.Aparitie();
            }
        }
        AparitiePoartaClientRpc();
    }

    public void AparitieInamiciExteriori()
    {
        if (!IsServer)
        {
            return;
        }
        
        foreach (var inamic in inamiciExterior)
        {
            if (inamic != null)
            {
                inamic.Aparitie();
            }
        }
    }
    
    public void TrezesteExteriori()
    {
        if (!IsServer)
        {
            return;
        }

        foreach (var inamic in inamiciExterior)
        {
            if (inamic != null)
            {
                inamic.Trezeste();
            }
        }
    }

    public void TrezesteInteriori()
    {
        if (!IsServer)
        {
            return;
        }

        foreach (var inamic in inamiciInterior)
        {
            if (inamic != null)
            {
                inamic.Trezeste();
            }
        }
    }
    
    private void Update()
    {
        if (!IsServer)
        {
            return;
        }

        if (!poartaInitializata && poartaHealth != null && poartaHealth.IsSpawned)
        {
            poartaHealth.maxHealth.Value = viataPoarta;
            poartaHealth.currentHealth.Value = viataPoarta;
            poartaInitializata = true;
        }

        // Cand poarta e doborata
        if (poartaInitializata && !poartaSparta && poartaHealth.currentHealth.Value <= 0)
        {
            poartaSparta = true;
            DoboaraPoartaClientRpc();
            TrezesteInteriori();
        }
    }
    
    [ClientRpc]
    private void AparitiePoartaClientRpc()
    {
        SeteazaVizibilitatePoarta(true);
    }

    [ClientRpc]
    private void DoboaraPoartaClientRpc()
    {
        SeteazaVizibilitatePoarta(false);
    }
    
    private void SeteazaVizibilitatePoarta(bool stare)
    {
        if (poartaRenderer != null)
        {
            poartaRenderer.enabled = stare;
        }

        if (poartaCollider != null)
        {
            poartaCollider.enabled = stare;
        }
        
        if (poartaObstacle != null)
        {
            poartaObstacle.enabled = stare;
        }

        if (poartaHealthCanvas != null)
        {
            poartaHealthCanvas.enabled = stare;
        }
    }
    
    public void VerificaVictorie()
    {
        if (!IsServer)
        {
            return;
        }
        
        if (FinalAttackManager.Instance == null || !FinalAttackManager.Instance.aInceputAtacul.Value)
        {
            return;
        }

        foreach (var inamic in inamiciInterior)
        {
            if (inamic != null && !inamic.EsteMort)
            {
                return;
            }
        }
        
        foreach (var inamic in inamiciExterior)
        {
            if (inamic != null && !inamic.EsteMort)
            {
                return;
            }
        }
        
        FinalAttackManager.Instance.Victorie();
    }
    
    public int NumarInamiciInBaza()
    {
        int count = 0;

        foreach (var inamic in inamiciInterior)
        {
            if (inamic != null && !inamic.EsteMort)
            {
                count++;
            }
        }

        foreach (var inamic in inamiciExterior)
        {
            if (inamic != null && !inamic.EsteMort)
            {
                count++;
            }
        }

        return count;
    }
}
