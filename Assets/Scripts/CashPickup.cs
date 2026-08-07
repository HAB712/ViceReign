using System.Collections;
using UnityEngine;

public class CashPickup : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource pickupAudioSource;
    [SerializeField] private AudioClip pickupSound;


    [SerializeField] private int cashAmount = 50;
    [SerializeField] private float respawnTime = 60f;

    private Collider pickupCollider;
    private Renderer[] renderers;

    private void Awake()
    {
        pickupCollider = GetComponent<Collider>();

        // Parent aur child sab Renderers automatically mil jayenge
        renderers = GetComponentsInChildren<Renderer>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        pickupCollider.enabled = false;

        foreach (Renderer r in renderers)
        {
            r.enabled = false;
        }

        if (pickupAudioSource != null && pickupSound != null)
        {
            pickupAudioSource.PlayOneShot(pickupSound);
        }

        MoneyManager.instance.AddMoney(cashAmount);

        RewardPopup.Instance.Show("+Rs. " + cashAmount);      

        StartCoroutine(Respawn());
    }

    private IEnumerator Respawn()
    {
        yield return new WaitForSeconds(respawnTime);

        foreach (Renderer r in renderers)
        {
            r.enabled = true;
        }

        pickupCollider.enabled = true;
    }
}