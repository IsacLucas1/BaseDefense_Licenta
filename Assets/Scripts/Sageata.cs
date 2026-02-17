using System;
using Unity.Netcode;
using UnityEngine;
using Unity.Netcode.Components;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NetworkTransform))]
public class Sageata : NetworkBehaviour
{
    [Header("Setari Sageata")]
    public float speed = 20f;
    public int damage = 10;
    public float lifetime = 3f;
    
    private ulong ownerId;
    private Rigidbody rb;
    public void Initialize(ulong shooterId)
    {
        ownerId = shooterId;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            return;
        }
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.sleepThreshold = -1;
            rb.linearVelocity = Vector3.zero; 
            rb.angularVelocity = Vector3.zero;
            rb.linearVelocity = transform.forward * speed;
        }
        
        Invoke(nameof(DistrugeSageata), lifetime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer)
        {
            return;
        }
        
        if (other.TryGetComponent<NetworkObject>(out var netObj))
        {
            if (netObj.OwnerClientId == ownerId && other.CompareTag("Player")) 
            {
                return; 
            }
        }
        
        Health targetHealth = other.GetComponent<Health>();
        if (other.CompareTag("Enemy") && targetHealth != null)
        {
            targetHealth.TakeDamage(damage);
            DistrugeSageata();
            return;
        }
        
        if(other.gameObject.layer == LayerMask.NameToLayer("Default"))
        {
            DistrugeSageata();
        }
    }
    void DistrugeSageata()
    {
        if (IsSpawned)
        {
            GetComponent<NetworkObject>().Despawn();
        }
    }
}
