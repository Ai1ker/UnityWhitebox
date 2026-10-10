using UnityEngine;

namespace VectorWhitebox
{
    public enum LaboratorySound
    {
        Footstep, Jump, SkillVelocity, SkillGravity, PlayerDeath,
        TurretAim, TurretFire, BulletHitPlayer, BulletHitWall,
        Checkpoint, PressurePlate, BulletSwitch, DoorOpen, DoorClose, Portal, CrateScrape
    }

    [CreateAssetMenu(menuName = "Rotcev/实验室音效库", fileName = "SoundBank")]
    public class LaboratorySoundBank : ScriptableObject
    {
        [System.Serializable]
        public class Entry
        {
            public LaboratorySound sound;
            public AudioClip[] clips;
            [Range(0, 1)] public float volume = .5f;
            [Range(0, .15f)] public float pitchVariation = .03f;
        }

        [Tooltip("替换对应 WAV 或在这里指定新 AudioClip。循环声请使用首尾衔接的素材。")]
        public Entry[] sounds;

        public Entry Find(LaboratorySound sound)
        {
            if (sounds != null)
                foreach (var entry in sounds)
                    if (entry != null && entry.sound == sound) return entry;
            return null;
        }
    }
}
