using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VectorWhitebox
{
    // One persistent player keeps impacts and portal tails alive when their emitter disappears.
    public sealed class LaboratoryAudio : MonoBehaviour
    {
        public const string VolumePref = "VectorWhitebox.Audio.MasterVolume";
        const int VoiceCount = 24, MaxLoops = 24;
        static LaboratoryAudio instance;
        static float masterVolume = .8f;
        static bool volumeDirty;
        LaboratorySoundBank bank;
        AudioSource[] voices;
        int nextVoice;
        Camera view;
        AudioListener fallbackListener;
        float nextListenerCheck;
        sealed class Loop
        {
            public Object owner;
            public AudioSource source;
            public float targetVolume;
            public float fadeSpeed;
        }
        readonly Dictionary<(Object, LaboratorySound), Loop> loops = new Dictionary<(Object, LaboratorySound), Loop>();
        readonly Stack<AudioSource> spareLoops = new Stack<AudioSource>();
        readonly List<(Object, LaboratorySound)> expired = new List<(Object, LaboratorySound)>();

        public static float MasterVolume
        {
            get { Ensure(); return masterVolume; }
            set
            {
                Ensure();
                float next = Mathf.Clamp01(value);
                if (Mathf.Approximately(masterVolume, next)) return;
                masterVolume = next;
                AudioListener.volume = next;
                PlayerPrefs.SetFloat(VolumePref, next);
                volumeDirty = true;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { instance = null; volumeDirty = false; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize() { Ensure(); }

        static void Ensure()
        {
            if (instance || !Application.isPlaying) return;
            var host = new GameObject("Laboratory Audio");
            instance = host.AddComponent<LaboratoryAudio>();
        }

        void Awake()
        {
            if (instance && instance != this) { Destroy(gameObject); return; }
            instance = this;
            DontDestroyOnLoad(gameObject);
            bank = Resources.Load<LaboratorySoundBank>("LaboratoryAudio/SoundBank");
            masterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumePref, .8f));
            AudioListener.volume = masterVolume;
            voices = new AudioSource[VoiceCount];
            for (int i = 0; i < voices.Length; i++) voices[i] = CreateSource("Effect " + (i + 1));
            fallbackListener = gameObject.AddComponent<AudioListener>();
            fallbackListener.enabled = false;
            SceneManager.sceneLoaded += SceneLoaded;
            CheckListener();
        }

        AudioSource CreateSource(string label)
        {
            var child = new GameObject(label);
            child.transform.SetParent(transform, false);
            var source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0;
            // Slow motion changes physics, not the clarity/pitch of feedback.
            source.ignoreListenerPause = true;
            return source;
        }

        public static void SaveVolume()
        {
            if (!volumeDirty) return;
            PlayerPrefs.Save();
            volumeDirty = false;
        }

        static (Object, LaboratorySound) Key(Object owner, LaboratorySound cue) => (owner, cue);

        float DistanceGain(Vector3 position)
        {
            if (!view) view = Camera.main;
            if (!view) return 1;
            float distance = Vector2.Distance(position, view.transform.position);
            float near = view.orthographic ? Mathf.Max(8, view.orthographicSize * 1.4f) : 8;
            return 1f - Mathf.Clamp01((distance - near) / 24f);
        }

        bool Resolve(LaboratorySound cue, out LaboratorySoundBank.Entry entry, out AudioClip clip)
        {
            entry = bank ? bank.Find(cue) : null;
            clip = null;
            if (entry == null || entry.clips == null || entry.clips.Length == 0) return false;
            int offset = Random.Range(0, entry.clips.Length);
            for (int i = 0; i < entry.clips.Length; i++)
            {
                clip = entry.clips[(offset + i) % entry.clips.Length];
                if (clip) return true;
            }
            return false;
        }

        public static void Play(LaboratorySound cue, Vector3 worldPosition, float gain = 1f, float pitch = 1f)
        {
            Ensure();
            if (!instance || !instance.Resolve(cue, out var entry, out var clip)) return;
            float volume = Mathf.Clamp01(entry.volume * gain * instance.DistanceGain(worldPosition));
            if (volume <= .001f || masterVolume <= 0) return;
            AudioSource source = null;
            for (int i = 0; i < instance.voices.Length; i++)
            {
                int index = (instance.nextVoice + i) % instance.voices.Length;
                if (instance.voices[index].isPlaying) continue;
                source = instance.voices[index]; instance.nextVoice = (index + 1) % instance.voices.Length; break;
            }
            // At the voice limit preserve the sounds already playing, including the portal tail.
            if (!source) return;
            source.clip = clip;
            source.volume = volume;
            source.pitch = Mathf.Clamp(pitch + Random.Range(-entry.pitchVariation, entry.pitchVariation), .5f, 2f);
            source.transform.position = worldPosition;
            source.Play();
        }

        public static void SetLoop(Object owner, LaboratorySound cue, bool active, Vector3 position, float gain = 1f, float pitch = 1f)
        {
            if (!owner) return;
            if (!active && !instance) return;
            Ensure();
            if (!instance) return;
            var key = Key(owner, cue);
            if (Time.timeScale <= 0 || masterVolume <= 0)
            { instance.StopLoop(key); return; }
            if (!active) { instance.FadeLoop(key); return; }
            float distanceGain = instance.DistanceGain(position);
            if (distanceGain * gain <= .001f) { instance.FadeLoop(key); return; }
            if (!instance.loops.TryGetValue(key, out var loop))
            {
                if (instance.loops.Count >= MaxLoops || !instance.Resolve(cue, out var entry, out var clip)) return;
                var source = instance.spareLoops.Count > 0 ? instance.spareLoops.Pop() : instance.CreateSource("Loop " + cue);
                source.clip = clip; source.loop = true;
                source.volume = 0;
                source.pitch = Mathf.Clamp(pitch, .5f, 2f);
                source.transform.position = position;
                loop = new Loop { owner = owner, source = source };
                instance.loops.Add(key, loop);
                instance.SetLoopVolume(loop, Mathf.Clamp01(entry.volume * gain * distanceGain));
                // Continuous friction/aiming recordings need no fixed starting transient.
                // A different offset avoids repeating the same scrape on every short push.
                if ((cue == LaboratorySound.CrateScrape || cue == LaboratorySound.TurretAim) && clip.samples > 1)
                    source.timeSamples = Random.Range(0, clip.samples);
                source.Play();
            }
            else
            {
                var entry = instance.bank.Find(cue);
                instance.SetLoopVolume(loop, Mathf.Clamp01(entry.volume * gain * distanceGain));
                loop.source.pitch = Mathf.Clamp(pitch, .5f, 2f);
                loop.source.transform.position = position;
            }
        }

        void SetLoopVolume(Loop loop, float volume)
        {
            if (Mathf.Approximately(loop.targetVolume, volume)) return;
            loop.targetVolume = volume;
            // Keep ordinary starts and contact changes soft without smearing their timing.
            float seconds = volume > loop.source.volume ? .04f : .08f;
            loop.fadeSpeed = Mathf.Abs(volume - loop.source.volume) / seconds;
        }

        void FadeLoop((Object, LaboratorySound) key)
        {
            if (loops.TryGetValue(key, out var loop)) SetLoopVolume(loop, 0);
        }

        void StopLoop((Object, LaboratorySound) key)
        {
            if (!loops.TryGetValue(key, out var loop)) return;
            loop.source.Stop(); loop.source.clip = null; loop.source.volume = 0;
            spareLoops.Push(loop.source);
            loops.Remove(key);
        }

        public static void StopLoops(Object owner)
        {
            if (!instance || !owner) return;
            instance.expired.Clear();
            foreach (var pair in instance.loops) if (pair.Value.owner == owner) instance.expired.Add(pair.Key);
            foreach (var key in instance.expired) instance.StopLoop(key);
        }

        void StopAllLoops()
        {
            expired.Clear(); expired.AddRange(loops.Keys);
            foreach (var key in expired) StopLoop(key);
        }

        void SceneLoaded(Scene scene, LoadSceneMode mode)
        { StopAllLoops(); view = null; CheckListener(); }

        void CheckListener()
        {
            bool otherListener = false;
            foreach (var listener in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
                if (listener != fallbackListener && listener.isActiveAndEnabled) { otherListener = true; break; }
            fallbackListener.enabled = !otherListener;
        }

        void LateUpdate()
        {
            if (Time.timeScale <= 0 || (WhiteboxGame.Instance && (WhiteboxGame.Instance.Dead || WhiteboxGame.Instance.Completed))) StopAllLoops();
            else
            {
                expired.Clear();
                foreach (var pair in loops)
                {
                    var loop = pair.Value;
                    if (!loop.owner || (loop.owner is Behaviour behaviour && !behaviour.isActiveAndEnabled))
                    { expired.Add(pair.Key); continue; }
                    loop.source.volume = Mathf.MoveTowards(loop.source.volume, loop.targetVolume,
                        loop.fadeSpeed * Time.unscaledDeltaTime);
                    // A new active request during the fade keeps this same source and offset.
                    if (loop.targetVolume <= 0 && loop.source.volume <= 0) expired.Add(pair.Key);
                }
                foreach (var key in expired) StopLoop(key);
            }
            if (Time.unscaledTime >= nextListenerCheck)
            { nextListenerCheck = Time.unscaledTime + .5f; CheckListener(); }
        }

        void OnApplicationPause(bool paused) { if (paused) SaveVolume(); }
        void OnApplicationQuit() { SaveVolume(); }
        void OnDestroy()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            if (instance == this) { SaveVolume(); instance = null; }
        }
    }
}
