using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace VectorWhitebox.EditorTools
{
    public static class LaboratoryAudioSetup
    {
        public const string BankPath = "Assets/Whitebox/Resources/LaboratoryAudio/SoundBank.asset";
        const string ClipsFolder = "Assets/Whitebox/ArtAssets/Audio/SFX";

        [MenuItem("Tools/Rotcev/Audio/创建或补全音效库")]
        public static void Build()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(BankPath));
            AssetDatabase.Refresh();
            var bank = AssetDatabase.LoadAssetAtPath<LaboratorySoundBank>(BankPath);
            bool created = !bank;
            if (created) bank = ScriptableObject.CreateInstance<LaboratorySoundBank>();
            var entries = new List<LaboratorySoundBank.Entry>(bank.sounds ?? Array.Empty<LaboratorySoundBank.Entry>());
            foreach (LaboratorySound cue in Enum.GetValues(typeof(LaboratorySound)))
            {
                if (entries.Exists(entry => entry != null && entry.sound == cue)) continue;
                var clips = new List<AudioClip>();
                int variants = cue == LaboratorySound.Footstep ? 6 : 1;
                for (int i = 1; i <= variants; i++)
                {
                    string name = cue + (variants > 1 ? "_" + i.ToString("00") : "");
                    string path = ClipsFolder + "/" + name + ".wav";
                    var importer = AssetImporter.GetAtPath(path) as AudioImporter;
                    if (!importer) throw new InvalidOperationException("Missing sound: " + path);
                    var settings = importer.defaultSampleSettings;
                    settings.loadType = AudioClipLoadType.DecompressOnLoad;
                    settings.compressionFormat = AudioCompressionFormat.PCM;
                    settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
                    settings.preloadAudioData = true;
                    importer.defaultSampleSettings = settings;
                    importer.forceToMono = true;
                    importer.loadInBackground = false;
                    importer.SaveAndReimport();
                    clips.Add(AssetDatabase.LoadAssetAtPath<AudioClip>(path));
                }
                entries.Add(new LaboratorySoundBank.Entry
                {
                    sound = cue, clips = clips.ToArray(), volume = DefaultGain(cue),
                    pitchVariation = cue == LaboratorySound.Footstep ? .01f :
                        cue == LaboratorySound.TurretAim || cue == LaboratorySound.CrateScrape ||
                        cue == LaboratorySound.SkillVelocity || cue == LaboratorySound.SkillGravity ? 0 : .025f
                });
            }
            bank.sounds = entries.ToArray();
            if (created) AssetDatabase.CreateAsset(bank, BankPath);
            EditorUtility.SetDirty(bank);
            AssetDatabase.SaveAssets();
            Debug.Log("Laboratory sound bank ready: " + bank.sounds.Length + " cues. Scenes were not modified.");
        }

        static float DefaultGain(LaboratorySound cue)
        {
            switch (cue)
            {
                case LaboratorySound.Footstep: return .22f;
                case LaboratorySound.CrateScrape: return .42f;
                case LaboratorySound.TurretAim: return .12f;
                case LaboratorySound.Jump: return .30f;
                case LaboratorySound.SkillVelocity: return .46f;
                case LaboratorySound.SkillGravity: return .48f;
                case LaboratorySound.PlayerDeath: return .62f;
                case LaboratorySound.TurretFire: return .46f;
                case LaboratorySound.BulletHitPlayer: return .57f;
                case LaboratorySound.BulletHitWall: return .42f;
                case LaboratorySound.Checkpoint: return .55f;
                case LaboratorySound.PressurePlate: return .4f;
                case LaboratorySound.BulletSwitch: return .46f;
                case LaboratorySound.DoorOpen: return .4f;
                case LaboratorySound.DoorClose: return .38f;
                case LaboratorySound.Portal: return .64f;
                default: return .5f;
            }
        }
    }
}
