using UnityEngine;
public class PlayerAudioManager : MonoBehaviour
{
    
    [SerializeField]
    private AudioSource _footstepSfx;
    [SerializeField]
    private AudioSource _glideSfx;
    [SerializeField]
    private AudioSource _punchSfx;
    [SerializeField]
    private AudioSource _landingSfx;

    private void PlayFootstepSfx()
    {
        _footstepSfx.volume = Random.Range(0.9f, 1f);
        _footstepSfx.pitch = Random.Range(0.9f, 1.2f);
        _footstepSfx.Play();
    }

    
    public void PlayGlideSfx()
    {
        _glideSfx.Play();
    }


    public void StopGlideSfx()
    {
        _glideSfx.Stop();
    }


    public void PlayPunchSfx()
    {
        _punchSfx.Play();
    }

    public void PlayLandingSfx()
    {
        _landingSfx.Play();
    }
}
