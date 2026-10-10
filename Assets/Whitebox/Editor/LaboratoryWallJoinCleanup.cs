using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VectorWhitebox
{
    /// <summary>One-time, audited cleanup of small protruding laboratory wall ends.</summary>
    public static class LaboratoryWallJoinCleanup
    {
        const string SceneFolder = "Assets/Whitebox/Scenes/";
        const string BackupFolder = "Temp/LaboratoryWallJoinBackup";
        const string ReportPath = "Assets/Whitebox/ArtAssets/Environment/Laboratory/WallJoinCleanupReport.md";
        const float AuditTolerance = 0.003f;
        const float ResultTolerance = 0.001f;

        sealed class Candidate
        {
            public readonly string ScenePath, RootName, AnchorRootName;
            public readonly int Axis;
            public readonly bool Minimum;
            public readonly float ExpectedOld, Target;

            public Candidate(string scene, string root, string anchorRoot, int axis, bool minimum, float expectedOld, float target)
            {
                ScenePath = SceneFolder + scene;
                RootName = root;
                AnchorRootName = anchorRoot;
                Axis = axis;
                Minimum = minimum;
                ExpectedOld = expectedOld;
                Target = target;
            }

            public string Edge { get { return (Minimum ? "min" : "max") + (Axis == 0 ? "X" : "Y"); } }
        }

        // Only the 14 audited long-axis trims at or below 0.15 world units.
        // Deliberately excludes wall-width changes, ceiling-height alignment and larger protrusions.
        static readonly Candidate[] Candidates =
        {
            new Candidate("Level02_Blank.unity", "Ground_Long",     "Wall_Left (1)",     0, false, 19.390000f, 19.360000f),
            new Candidate("Level02_Blank.unity", "Ground_Long (1)", "Wall_Left (1)",     0, true,  18.269453f, 18.360000f),
            new Candidate("Level02_Blank.unity", "Ground_Long (2)", "Wall_Right",        0, true,  27.851944f, 27.930000f),
            new Candidate("Level02_Blank.unity", "Ceiling (2)",     "Wall_Right (1)",    0, false, 73.940000f, 73.900000f),
            new Candidate("Level02_Blank.unity", "Ceiling (3)",     "Wall_Right (1)",    0, false, 74.035000f, 73.900000f),
            new Candidate("Level03_Blank.unity", "Ground_Long",     "Wall_Left",         0, true, -12.952500f,-12.885053f),
            new Candidate("Level03_Blank.unity", "Wall_Left",       "Ceiling",           1, false,  8.547500f,  8.491394f),
            new Candidate("Level03_Blank.unity", "Ground_Long (1)", "Wall_Left (1)",     0, false, 64.643800f, 64.590000f),
            new Candidate("Level03_Blank.unity", "Ground_Long (3)", "Wall_Right (1)",    0, true,  84.209990f, 84.229164f),
            new Candidate("Level03_Blank.unity", "Ground_Spawn",    "Wall_Left (1)",     0, true,  63.540000f, 63.589996f),
            new Candidate("Level04_Blank.unity", "Ground_Long",     "Ground_Long (2)",   0, false,  8.436632f,  8.410002f),
            new Candidate("Level04_Blank.unity", "Ceiling (2)",     "Wall_Right",        0, false, 91.257490f, 91.200000f),
            new Candidate("Level05_Blank.unity", "Ceiling",         "Wall_Right",        0, false, 32.575000f, 32.540000f),
            new Candidate("Level06_Blank.unity", "Ceiling (5)",     "Wall_Left (1)",     0, true, -46.411175f,-46.370000f)
        };

        sealed class SceneRecord
        {
            public string Path;
            public Scene Scene;
            public bool OpenedByTool;
            public bool SavedByTool;
        }

        sealed class ShapePlan
        {
            public Candidate Candidate;
            public SceneRecord Scene;
            public GameObject Root;
            public BoxCollider2D Shape;
            public Vector2 OriginalSize, OriginalOffset, NewSize, NewOffset;
            public Bounds OriginalBounds, NewBounds;
            public Vector3 LocalPosition, LocalScale, WorldPosition, WorldScale;
            public Quaternion LocalRotation, WorldRotation;
            public Transform Parent;
            public PhysicsMaterial2D PhysicsMaterial;
            public bool OriginalTrigger, OriginalEnabled, AlreadyAligned, SkippedChangedByDesigner;
            public float Before, Trim;
            public string SkipReason;

            public void CheckTransform()
            {
                var tr = Root.transform;
                Require(tr.parent == Parent && tr.localPosition.Equals(LocalPosition) &&
                        tr.localRotation.Equals(LocalRotation) && tr.localScale.Equals(LocalScale) &&
                        tr.position.Equals(WorldPosition) && tr.rotation.Equals(WorldRotation) &&
                        tr.lossyScale.Equals(WorldScale), Candidate, "Root Transform changed.");
                Require(Shape.sharedMaterial == PhysicsMaterial && Shape.isTrigger == OriginalTrigger &&
                        Shape.enabled == OriginalEnabled, Candidate, "Collider material/trigger/enabled changed.");
            }
        }

        [MenuItem("Tools/Whitebox/Art/Clean Audited Laboratory Wall Joins")]
        static void RunFromMenu() { Debug.Log(Run()); }

        public static string Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("Run wall cleanup only in idle Edit Mode.");

            var originalActive = SceneManager.GetActiveScene();
            var scenes = new List<SceneRecord>();
            var plans = new List<ShapePlan>();
            var changed = new List<ShapePlan>();
            int undoGroup = -1;
            try
            {
                // Check every already-loaded target before opening or changing anything.
                foreach (string path in Candidates.Select(c => c.ScenePath).Distinct())
                {
                    if (!File.Exists(path)) throw new FileNotFoundException("Missing audited scene.", path);
                    var loaded = SceneManager.GetSceneByPath(path);
                    if (loaded.IsValid() && loaded.isLoaded && loaded.isDirty)
                        throw new InvalidOperationException("Unsaved changes in " + path + "; wall cleanup stopped without changes.");
                }

                Directory.CreateDirectory(BackupFolder);
                foreach (string path in Candidates.Select(c => c.ScenePath).Distinct())
                {
                    // Preserve the first pre-cleanup disk scene forever; subsequent runs never replace it.
                    string backupPath = Path.Combine(BackupFolder, Path.GetFileName(path));
                    if (!File.Exists(backupPath)) File.Copy(path, backupPath, false);
                    var scene = SceneManager.GetSceneByPath(path);
                    bool opened = !scene.IsValid() || !scene.isLoaded;
                    if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                    var record = new SceneRecord { Path = path, Scene = scene, OpenedByTool = opened };
                    scenes.Add(record);
                    if (scene.isDirty)
                        throw new InvalidOperationException("Scene became dirty while opening: " + path + ". No wall cleanup was applied.");
                }

                Physics2D.SyncTransforms();
                // Validate the entire batch, including desired local shapes, before the first mutation.
                foreach (Candidate candidate in Candidates)
                {
                    var record = scenes.Single(s => s.Path == candidate.ScenePath);
                    plans.Add(ValidateAndPlan(candidate, record));
                }

                Undo.IncrementCurrentGroup();
                undoGroup = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName("Clean audited laboratory wall joins");
                foreach (ShapePlan plan in plans.Where(p => !p.AlreadyAligned && !p.SkippedChangedByDesigner))
                {
                    Undo.RecordObject(plan.Shape, "Trim laboratory wall collider end");
                    changed.Add(plan);
                    plan.Shape.size = plan.NewSize;
                    plan.Shape.offset = plan.NewOffset;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(plan.Shape);
                }

                Physics2D.SyncTransforms();
                foreach (ShapePlan plan in changed) RefreshVisual(plan);
                Physics2D.SyncTransforms();
                foreach (ShapePlan plan in plans) ValidateResult(plan);

                // No scene is saved until all 14 shapes and all root transforms pass verification.
                foreach (SceneRecord record in scenes.Where(s => changed.Any(p => p.Scene == s)))
                {
                    foreach (ShapePlan plan in plans.Where(p => p.Scene == record)) ValidateResult(plan);
                    EditorSceneManager.MarkSceneDirty(record.Scene);
                    if (!EditorSceneManager.SaveScene(record.Scene))
                        throw new IOException("Could not save " + record.Path);
                    record.SavedByTool = true;
                }

                WriteReport(plans);
                Undo.CollapseUndoOperations(undoGroup);
                return "Laboratory wall join cleanup: " + changed.Count + " trimmed, " +
                       plans.Count(p => p.AlreadyAligned) + " already aligned, " +
                       plans.Count(p => p.SkippedChangedByDesigner) + " skipped because designer changed the audited edge. Report: " + ReportPath;
            }
            catch
            {
                // A validation/save failure restores the whole batch, including any scenes already saved.
                foreach (ShapePlan plan in changed)
                {
                    plan.Shape.size = plan.OriginalSize;
                    plan.Shape.offset = plan.OriginalOffset;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(plan.Shape);
                }
                Physics2D.SyncTransforms();
                foreach (ShapePlan plan in changed)
                {
                    try { RefreshVisual(plan); }
                    catch (Exception rollbackError) { Debug.LogError("Rollback art refresh failed: " + rollbackError.Message); }
                }
                Physics2D.SyncTransforms();
                foreach (SceneRecord record in scenes.Where(s => s.SavedByTool))
                {
                    EditorSceneManager.MarkSceneDirty(record.Scene);
                    if (!EditorSceneManager.SaveScene(record.Scene))
                        Debug.LogError("Rollback save failed for " + record.Path + ". Original backup: " + BackupFolder);
                }
                if (undoGroup >= 0) Undo.CollapseUndoOperations(undoGroup);
                throw;
            }
            finally
            {
                foreach (SceneRecord record in scenes.Where(s => s.OpenedByTool).Reverse())
                    if (record.Scene.IsValid() && record.Scene.isLoaded)
                        EditorSceneManager.CloseScene(record.Scene, true);
                if (originalActive.IsValid() && originalActive.isLoaded)
                    SceneManager.SetActiveScene(originalActive);
            }
        }

        static ShapePlan ValidateAndPlan(Candidate candidate, SceneRecord record)
        {
            var matches = record.Scene.GetRootGameObjects().Where(g => g.name == candidate.RootName).ToArray();
            Require(matches.Length == 1, candidate, "Expected exactly one root object.");
            var root = matches[0];
            var shapes = root.GetComponents<BoxCollider2D>();
            Require(shapes.Length == 1, candidate, "Expected exactly one root BoxCollider2D.");
            var shape = shapes[0];
            Require(root.activeInHierarchy && shape.enabled && !shape.isTrigger, candidate, "Solid collider is inactive or a trigger.");
            Require(!root.GetComponent<Rigidbody2D>(), candidate, "Unexpected Rigidbody2D on a static wall.");
            Require(shape.edgeRadius == 0f, candidate, "Rounded collider bounds are not supported by this audited cleanup.");
            string prefab = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root);
            Require(prefab.StartsWith("Assets/Whitebox/Prefabs/Environment/", StringComparison.Ordinal) &&
                    !prefab.Contains("OneWay") && !prefab.Contains("Spike"), candidate, "Object is no longer an audited environment prefab.");
            Require(root.GetComponentInChildren<LaboratorySurfaceVisual>(true), candidate, "Laboratory art component is missing.");

            var tr = root.transform;
            var bounds = shape.bounds;
            float before = candidate.Minimum ? bounds.min[candidate.Axis] : bounds.max[candidate.Axis];
            bool aligned = Mathf.Abs(before - candidate.Target) <= ResultTolerance;
            bool skipped = !aligned && Mathf.Abs(before - candidate.ExpectedOld) >= AuditTolerance;
            string skipReason = skipped ? "Candidate edge changed: current " + F(before) + ", audited " + F(candidate.ExpectedOld) + "." : "";

            // The target is only valid while the adjoining wall still has its audited edge.
            // Checking both sides protects a designer who moved the neighbour after this audit.
            var anchors = record.Scene.GetRootGameObjects().Where(g => g.name == candidate.AnchorRootName).ToArray();
            string anchorReason = "";
            if (anchors.Length != 1)
                anchorReason = "Anchor root missing or ambiguous: " + candidate.AnchorRootName + ".";
            else
            {
                var anchorShapes = anchors[0].GetComponents<BoxCollider2D>();
                if (anchorShapes.Length != 1 || !anchorShapes[0].enabled || !anchors[0].activeInHierarchy)
                    anchorReason = "Anchor solid collider missing or inactive: " + candidate.AnchorRootName + ".";
                else
                {
                    var anchorBounds = anchorShapes[0].bounds;
                    float anchorEdge = candidate.Minimum ? anchorBounds.min[candidate.Axis] : anchorBounds.max[candidate.Axis];
                    if (Mathf.Abs(anchorEdge - candidate.Target) >= AuditTolerance)
                        anchorReason = "Anchor edge changed: " + candidate.AnchorRootName + " " + candidate.Edge +
                                       " is " + F(anchorEdge) + ", audited target " + F(candidate.Target) + ".";
                }
            }
            if (!string.IsNullOrEmpty(anchorReason))
            {
                skipped = true;
                skipReason = string.IsNullOrEmpty(skipReason) ? anchorReason : skipReason + " " + anchorReason;
            }

            var minimum = bounds.min;
            var maximum = bounds.max;
            if (!aligned && !skipped)
            {
                if (candidate.Minimum) minimum[candidate.Axis] = candidate.Target;
                else maximum[candidate.Axis] = candidate.Target;
            }
            float trim = aligned || skipped ? 0f : candidate.Minimum ? candidate.Target - before : before - candidate.Target;
            float oldLength = bounds.size[candidate.Axis];
            float newLength = maximum[candidate.Axis] - minimum[candidate.Axis];
            if (!aligned && !skipped)
            {
                Require(trim >= 0.01f && trim <= 0.15f, candidate, "Trim is outside 0.01..0.15 world units.");
                Require(oldLength >= bounds.size[1 - candidate.Axis], candidate, "Requested trim is on the short axis.");
                Require(newLength >= oldLength * 0.9f, candidate, "Trim would remove over 10% of the long axis.");
            }

            // Transform all four desired world corners; this also handles negative scale and 90-degree rotation.
            var localMin = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            var localMax = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            for (int x = 0; x < 2; ++x)
            for (int y = 0; y < 2; ++y)
            {
                var world = new Vector3(x == 0 ? minimum.x : maximum.x, y == 0 ? minimum.y : maximum.y, bounds.center.z);
                Vector2 local = tr.InverseTransformPoint(world);
                localMin = Vector2.Min(localMin, local);
                localMax = Vector2.Max(localMax, local);
            }
            var localSize = localMax - localMin;
            Require(localSize.x > 0f && localSize.y > 0f && !float.IsInfinity(localSize.x) && !float.IsInfinity(localSize.y),
                    candidate, "Invalid inverse-transformed collider size.");
            var targetBounds = new Bounds((minimum + maximum) * 0.5f, maximum - minimum);
            var plannedWorldMin = new Vector3(float.PositiveInfinity, float.PositiveInfinity, bounds.min.z);
            var plannedWorldMax = new Vector3(float.NegativeInfinity, float.NegativeInfinity, bounds.max.z);
            for (int x = 0; x < 2; ++x)
            for (int y = 0; y < 2; ++y)
            {
                Vector3 world = tr.TransformPoint(new Vector3(x == 0 ? localMin.x : localMax.x, y == 0 ? localMin.y : localMax.y, 0f));
                plannedWorldMin.x = Mathf.Min(plannedWorldMin.x, world.x);
                plannedWorldMin.y = Mathf.Min(plannedWorldMin.y, world.y);
                plannedWorldMax.x = Mathf.Max(plannedWorldMax.x, world.x);
                plannedWorldMax.y = Mathf.Max(plannedWorldMax.y, world.y);
            }
            Require(CloseXY(plannedWorldMin, targetBounds.min) && CloseXY(plannedWorldMax, targetBounds.max), candidate,
                    "World rectangle cannot be represented by this collider's transform.");

            return new ShapePlan
            {
                Candidate = candidate, Scene = record, Root = root, Shape = shape,
                OriginalSize = shape.size, OriginalOffset = shape.offset,
                NewSize = localSize, NewOffset = (localMin + localMax) * 0.5f,
                OriginalBounds = bounds, NewBounds = aligned || skipped ? bounds : targetBounds,
                LocalPosition = tr.localPosition, LocalRotation = tr.localRotation, LocalScale = tr.localScale,
                WorldPosition = tr.position, WorldRotation = tr.rotation, WorldScale = tr.lossyScale, Parent = tr.parent,
                PhysicsMaterial = shape.sharedMaterial, OriginalTrigger = shape.isTrigger, OriginalEnabled = shape.enabled,
                AlreadyAligned = aligned && !skipped, SkippedChangedByDesigner = skipped, Before = before, Trim = trim,
                SkipReason = skipReason
            };
        }

        static void ValidateResult(ShapePlan plan)
        {
            plan.CheckTransform();
            var bounds = plan.Shape.bounds;
            Require(CloseXY(bounds.min, plan.NewBounds.min) && CloseXY(bounds.max, plan.NewBounds.max), plan.Candidate,
                    "Final collider bounds do not match the planned rectangle.");
            if (plan.SkippedChangedByDesigner) return;
            float edge = plan.Candidate.Minimum ? bounds.min[plan.Candidate.Axis] : bounds.max[plan.Candidate.Axis];
            Require(Mathf.Abs(edge - plan.Candidate.Target) <= ResultTolerance, plan.Candidate, "Final edge missed its target.");
        }

        static void RefreshVisual(ShapePlan plan)
        {
            var visual = plan.Root.GetComponentInChildren<LaboratorySurfaceVisual>(true);
            if (visual) visual.RefreshVisual();
            plan.CheckTransform();
        }

        static bool CloseXY(Vector3 a, Vector3 b)
        {
            return Mathf.Abs(a.x - b.x) <= ResultTolerance && Mathf.Abs(a.y - b.y) <= ResultTolerance;
        }

        static void Require(bool condition, Candidate candidate, string message)
        {
            if (!condition) throw new InvalidOperationException(candidate.ScenePath + " / " + candidate.RootName + ": " + message);
        }

        static string F(float value) { return value.ToString("F6", CultureInfo.InvariantCulture); }

        static void WriteReport(IEnumerable<ShapePlan> plans)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Laboratory wall join cleanup");
            sb.AppendLine();
            sb.AppendLine("仅修整审计名单中 14 个墙地块碰撞体的长轴端头；只修改 BoxCollider2D.size / offset，并同步刷新实验室美术。Root Transform 经逐项精确校验保持不变，不修改物理材质、机关、通道高度或其他对象。首次运行前的原场景备份位于 `Temp/LaboratoryWallJoinBackup`，再次运行不会覆盖备份。已对齐的对象仅验证并跳过；候选边界与审计旧值偏差达到 0.003，或邻接锚点缺失、锚点对应边与审计目标偏差达到 0.003 时，记录 SkippedChangedByDesigner 及原因并保留现状。所有候选在整批预检及修改后验证通过后才保存各自场景。");
            sb.AppendLine();
            sb.AppendLine("| Scene | Root object | Anchor | Edge | Before | After | Trim | Result | Reason |");
            sb.AppendLine("| --- | --- | --- | --- | ---: | ---: | ---: | --- | --- |");
            foreach (ShapePlan plan in plans)
            {
                var bounds = plan.Shape.bounds;
                float after = plan.Candidate.Minimum ? bounds.min[plan.Candidate.Axis] : bounds.max[plan.Candidate.Axis];
                sb.AppendLine("| " + plan.Candidate.ScenePath + " | " + plan.Candidate.RootName + " | " + plan.Candidate.AnchorRootName + " | " + plan.Candidate.Edge +
                              " | " + F(plan.Before) + " | " + F(after) + " | " + F(plan.Trim) + " | " +
                              (plan.SkippedChangedByDesigner ? "SkippedChangedByDesigner" : plan.AlreadyAligned ? "AlreadyAligned" : "Trimmed") +
                              " | " + (plan.SkipReason ?? "").Replace("|", "\\|") + " |");
            }
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            File.WriteAllText(ReportPath, sb.ToString(), new UTF8Encoding(false));
            AssetDatabase.ImportAsset(ReportPath, ImportAssetOptions.ForceUpdate);
        }
    }
}
