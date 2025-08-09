using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class FootstepAudio : MonoBehaviour
{
    [Header("Footstep Settings")]
    public AudioClip[] grassFootsteps;
    public AudioClip[] stoneFootsteps;
    public AudioClip[] dirtFootsteps;
    public AudioClip[] sandFootsteps;
    
    [Header("Audio Settings")]
    [Range(0f, 1f)]
    public float volume = 0.5f;
    [Range(0.8f, 1.2f)]
    public float minPitch = 0.9f;
    [Range(0.8f, 1.2f)]
    public float maxPitch = 1.1f;
    
    private AudioSource audioSource;
    
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.volume = volume;
        audioSource.spatialBlend = 0f; // 2D sound
    }
    
    public void PlayFootstep(BlockType groundType = BlockType.Grass)
    {
        AudioClip[] clips = GetFootstepClips(groundType);
        if (clips == null || clips.Length == 0) return;
        
        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clip != null)
        {
            audioSource.pitch = Random.Range(minPitch, maxPitch);
            audioSource.PlayOneShot(clip, volume);
        }
    }
    
    private AudioClip[] GetFootstepClips(BlockType groundType)
    {
        switch (groundType)
        {
            case BlockType.Grass:
                return grassFootsteps;
            case BlockType.Stone:
                return stoneFootsteps;
            case BlockType.Dirt:
                return dirtFootsteps;
            case BlockType.Sand:
                return sandFootsteps;
            case BlockType.Coal:
                return stoneFootsteps; // Use stone sounds for coal
            default:
                return grassFootsteps;
        }
    }
}