using Unity.Netcode;
using UnityEngine;

public enum TipCamp
{
    Viteza,
    Damage,
    Bani,
    Lemn,
    Obisnuit
}

public class CampReward : NetworkBehaviour
{
    [Header("Setari Recompensa Tabara")]
    [Tooltip("Ce tip de buff/resursa ofera acest inamic cand moare?")]
    public TipCamp camp = TipCamp.Viteza;
    
    [Header("Setari Viteza")]
    public int bonusViteza = 1;      
    public float durataViteza = 10f;  

    [Header("Setari Damage")]
    public int bonusDamage = 10;      
    public float durataDamage = 10f;  

    [Header("Setari Resurse")]
    public int cantitateBani = 100;   
    public int cantitateLemn = 30;
    public int cantitateBaniObisnuit = 10;
    
    public void OferaRecompensa(BasePlayer jucator)
    {
        if(!IsServer || jucator == null)
        {
            return;
        }
        switch (camp)
        {
            case TipCamp.Viteza:
                jucator.PrimesteRecompensa(camp, bonusViteza, durataViteza);
                break;
            case TipCamp.Damage:
                jucator.PrimesteRecompensa(camp, bonusDamage, durataDamage);
                break;
            case TipCamp.Bani:
                jucator.PrimesteRecompensa(camp, cantitateBani, 0f);
                break;
            case TipCamp.Lemn:
                jucator.PrimesteRecompensa(camp, cantitateLemn, 0f);
                break;
            case TipCamp.Obisnuit:
                jucator.PrimesteRecompensa(camp, cantitateBaniObisnuit, 0f);
                break;
        }
    }
}
