using System;
using UnityEngine;

namespace VectorWhitebox
{
    [DisallowMultipleComponent, RequireComponent(typeof(BoxCollider2D))]
    public class CollapseHazard : MonoBehaviour
    {
        [Min(0), Tooltip("玩家进入触发区后，到第一块顶板坠落的秒数。")]
        public float delaySeconds = 1.5f;
        [Min(0), Tooltip("相邻顶板依次坠落的间隔，按触发时的世界坐标从左到右排序。")]
        public float segmentInterval = .2f;
        [Min(0), Tooltip("每块顶板坠落前震动的时间；不会额外增加触发延迟。")]
        public float warningSeconds = .4f;
        [Min(.1f), Tooltip("顶板沿世界向下的加速度，不受技能和重力边界影响。")]
        public float fallAcceleration = 28f;
        [Min(.1f), Tooltip("顶板坠落的最大速度。")]
        public float maximumFallSpeed = 26f;
        [Min(1), Tooltip("顶板没有碰到地面时，坠落超过此距离也会停止并淡出。")]
        public float maximumFallDistance = 30f;
        [Min(0), Tooltip("顶板落地后保留的时间，然后淡出。")]
        public float settledLifetime = 1.5f;
        [Tooltip("玩家复活或脱离卡死时恢复此机关，方便重试。")]
        public bool resetOnRespawn = true;
        [Tooltip("可以增删或移动子物体；留空时自动收集所有 CollapseDebris。")]
        public CollapseDebris[] segments;

        public bool Triggered { get; private set; }
        public float ElapsedSeconds { get; private set; }
        CollapseDebris[] orderedSegments;
        BoxCollider2D triggerArea;

        void Awake()
        {
            triggerArea = GetComponent<BoxCollider2D>();
            triggerArea.isTrigger = true;
            CollectSegments();
        }

        void OnValidate()
        {
            var area = GetComponent<BoxCollider2D>();
            if (area) area.isTrigger = true;
        }

        void CollectSegments()
        {
            if (segments == null || segments.Length == 0)
                segments = GetComponentsInChildren<CollapseDebris>(true);
            foreach (var segment in segments)
                if (segment) segment.CaptureInitialPose();
        }

        void OnTriggerEnter2D(Collider2D other) { TryTrigger(other); }
        void OnTriggerStay2D(Collider2D other) { TryTrigger(other); }

        public void TryTrigger(Collider2D other)
        {
            var game = WhiteboxGame.Instance;
            if (game && (game.Dead || game.Completed)) return;
            if (!Triggered && other && other.GetComponentInParent<WhiteboxPlayer>()) Activate();
        }

        public void Activate()
        {
            if (Triggered || !isActiveAndEnabled) return;
            if (segments == null || segments.Length == 0) CollectSegments();
            orderedSegments = Array.FindAll(segments, segment => segment && segment.gameObject.activeInHierarchy);
            Array.Sort(orderedSegments, (left, right) =>
            {
                int xOrder = left.transform.position.x.CompareTo(right.transform.position.x);
                return xOrder != 0 ? xOrder : left.transform.GetSiblingIndex().CompareTo(right.transform.GetSiblingIndex());
            });
            Triggered = true;
            ElapsedSeconds = 0;
            AdvanceSequence();
        }

        void Update()
        {
            if (!Triggered) return;
            ElapsedSeconds += Time.deltaTime;
            AdvanceSequence();
        }

        void AdvanceSequence()
        {
            if (orderedSegments == null) return;
            float warning = Mathf.Max(0, warningSeconds);
            for (int i = 0; i < orderedSegments.Length; i++)
            {
                var segment = orderedSegments[i];
                if (!segment || segment.Released) continue;
                float releaseAt = Mathf.Max(0, delaySeconds) + i * Mathf.Max(0, segmentInterval);
                if (ElapsedSeconds >= releaseAt)
                    segment.Release(Mathf.Max(.1f, fallAcceleration), Mathf.Max(.1f, maximumFallSpeed), Mathf.Max(0, settledLifetime), Mathf.Max(1, maximumFallDistance));
                else if (ElapsedSeconds >= releaseAt - warning)
                    segment.AnimateWarning(ElapsedSeconds - (releaseAt - warning), i);
            }
        }

        public void ResetHazard()
        {
            Triggered = false;
            ElapsedSeconds = 0;
            orderedSegments = null;
            if (segments == null || segments.Length == 0) CollectSegments();
            foreach (var segment in segments)
                if (segment) segment.ResetDebris();
        }

        void OnDrawGizmos()
        {
            var area = GetComponent<BoxCollider2D>();
            if (!area) return;
            Matrix4x4 previous = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(1f, .35f, .17f, .12f);
            Gizmos.DrawCube(area.offset, new Vector3(area.size.x, area.size.y, .02f));
            Gizmos.color = new Color(1f, .4f, .2f, .85f);
            Gizmos.DrawWireCube(area.offset, new Vector3(area.size.x, area.size.y, .02f));
            Gizmos.matrix = previous;
        }
    }
}
