using UnityEngine;

public class GameSoundEffect : MonoBehaviour
{
    public AudioSource getItemAudio, stealItemAudio, getPointAudio;
    public void PlayGetItemSound() {
        getItemAudio.Play();
    }
    public void PlayStealItemSound() {
        stealItemAudio.Play();
    }
    public void PlayGetPointSound() {
        getPointAudio.Play();
    }
}
