using UnityEngine;
using Unity.Netcode.Components;

public class ClientNetworkTransform : NetworkTransform
{
    // Specifica daca transform-ul este controlat de client sau de server
    // Suprascrisa pentru a permite clientului sa controleze transform-ul
    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}

