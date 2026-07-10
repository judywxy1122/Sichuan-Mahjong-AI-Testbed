using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace SichuanMahjong.UnityApp
{
    /// <summary>
    /// Sound output: a synthesized beep standing in for Toolkit.beep(), plus
    /// the reward WAV loaded from StreamingAssets (with the same triple-beep
    /// fallback the Java version used when the clip cannot be played).
    /// </summary>
    public class BeepPlayer : MonoBehaviour
    {
        private AudioSource audioSource;
        private AudioClip beepClip;
        private AudioClip rewardClip;

        private void Awake()
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            beepClip = CreateBeepClip();
            StartCoroutine(LoadRewardClip());
        }

        public void Beep()
        {
            if (beepClip != null)
            {
                audioSource.PlayOneShot(beepClip, 0.5f);
            }
        }

        public void PlayRewardSound()
        {
            if (rewardClip != null)
            {
                audioSource.PlayOneShot(rewardClip);
            }
            else
            {
                StartCoroutine(FallbackRewardBeeps());
            }
        }

        private IEnumerator FallbackRewardBeeps()
        {
            for (int i = 0; i < 3; i++)
            {
                Beep();
                yield return new WaitForSeconds(0.14f);
            }
        }

        private static AudioClip CreateBeepClip()
        {
            const int sampleRate = 44100;
            const float duration = 0.12f;
            const float frequency = 880f;
            int sampleCount = (int)(sampleRate * duration);
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float envelope = 1f - (float)i / sampleCount;
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * i / sampleRate) * 0.4f * envelope;
            }
            AudioClip clip = AudioClip.Create("beep", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private IEnumerator LoadRewardClip()
        {
            string path = System.IO.Path.Combine(Application.streamingAssetsPath, "sound", "slot_win_01.wav");
            // Uri normalizes both "/home/..." and "C:\..." into a proper file:/// URL.
            string url = new System.Uri(path).AbsoluteUri;
            using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.WAV))
            {
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success)
                {
                    rewardClip = DownloadHandlerAudioClip.GetContent(request);
                }
                else
                {
                    Debug.LogWarning("Could not load reward sound: " + request.error);
                }
            }
        }
    }
}
