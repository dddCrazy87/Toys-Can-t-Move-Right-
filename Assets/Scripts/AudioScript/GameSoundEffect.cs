using UnityEngine;

public class GameSoundEffect : MonoBehaviour
{
    public AudioSource getItemAudio, stealItemAudio, getPointAudio;
    public AudioSource getSpecialAudio, useSpectialAudio, freezerAudio, blockerAudio, fingerAudio;

    public void PlayGetItemSound()
    {
        getItemAudio.Play();
    }
    public void PlayStealItemSound()
    {
        stealItemAudio.Play();
    }
    public void PlayGetPointSound()
    {
        getPointAudio.Play();
    }
    public void PlayGetSpecialSound()
    {
        if (getSpecialAudio != null)
            getSpecialAudio.Play();
    }
    public void PlayUseSpecialSound()
    {
        if (useSpectialAudio != null)
            useSpectialAudio.Play();
    }
    public void PlayBlockerSound()
    {
        if (blockerAudio != null)
            blockerAudio.Play();
    }
    public void PlayFreezerSound()
    {
        if (freezerAudio != null)
            freezerAudio.Play();
    }
    public void PlayFingerSound()
    {
        if (fingerAudio != null)
            fingerAudio.Play();
    }
}
