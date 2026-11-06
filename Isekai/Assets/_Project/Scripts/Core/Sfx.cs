using UnityEngine;

namespace Isekai.Core
{
    public static class Sfx
    {
        // A random clip at ±8% pitch, so repeated sounds don't grate.
        public static void PlayRandom(AudioSource source, AudioClip[] clips, float volume = 1f)
        {
            if (clips.Length == 0) return;
            source.pitch = Random.Range(0.92f, 1.08f);
            source.PlayOneShot(clips[Random.Range(0, clips.Length)], volume);
        }
    }
}
