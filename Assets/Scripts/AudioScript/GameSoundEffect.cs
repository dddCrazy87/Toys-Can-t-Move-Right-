using UnityEngine;

public class GameSoundEffect : MonoBehaviour
{
    public AudioSource getItemAudio, stealItemAudio, getPointAudio;
    public AudioSource playerCollisionAudio;

    public void PlayGetItemSound() {
        getItemAudio.Play();
    }
    public void PlayStealItemSound() {
        stealItemAudio.Play();
    }
    public void PlayGetPointSound() {
        getPointAudio.Play();
    }
    public void PlayPlayerCollisionSound() {
        if (playerCollisionAudio != null)
            playerCollisionAudio.Play();
    }
}
