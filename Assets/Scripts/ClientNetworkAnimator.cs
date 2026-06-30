using Unity.Netcode.Components;
using UnityEngine;

public class ClientNetworkAnimator : NetworkAnimator
{
    // Specifica daca animațiile sunt controlate de client sau de server
    // Suprascrisa pentru a permite clientului sa controleze animațiile
    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}