using UnityEngine;

public class DoorInteractable : MonoBehaviour, IInteractable
{
    public float openAngle = 90f;
    public float openSpeed = 4f;

    [Header("Audio")]
    public AudioClip openClip;
    [Range(0f, 1f)] public float volume = 0.35f;
    public float minDistance = 1f;
    public float maxDistance = 8f;

    Quaternion closedRot;
    Quaternion openRot;
    bool isOpen;

    void Awake()
    {
        closedRot = transform.localRotation;
        openRot = closedRot * Quaternion.Euler(0f, openAngle, 0f);
    }

    void Update()
    {
        var target = isOpen ? openRot : closedRot;
        transform.localRotation = Quaternion.Slerp(transform.localRotation, target, Time.deltaTime * openSpeed);
    }

    public void Interact()
    {
        isOpen = !isOpen;
        PlaySound();
    }

    void PlaySound()
    {
        if (openClip == null) return;
        var go = new GameObject("DoorSFX");
        go.transform.position = transform.position;
        var src = go.AddComponent<AudioSource>();
        src.clip = openClip;
        src.volume = volume;
        src.spatialBlend = 1f;
        src.minDistance = minDistance;
        src.maxDistance = maxDistance;
        src.rolloffMode = AudioRolloffMode.Linear;
        src.Play();
        Destroy(go, openClip.length + 0.1f);
    }
}
