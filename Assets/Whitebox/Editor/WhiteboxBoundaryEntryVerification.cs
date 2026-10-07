using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VectorWhitebox.Editor
{
    // Runs without Play Mode in a disposable scene, leaving authored levels untouched.
    public static class WhiteboxBoundaryEntryVerification
    {
        [MenuItem("Whitebox/Verify Orange Boundary Entry")]
        public static void Verify() { Debug.Log(Run()); }

        public static string Run()
        {
            if (EditorApplication.isPlaying)
                throw new InvalidOperationException("Stop Play Mode before running boundary entry verification.");

            var preview = EditorSceneManager.NewPreviewScene();
            var roots = new List<GameObject>();
            var results = new List<string>();
            try
            {
                var cube = CreateCube(preview, roots, new Vector2(3, 0));
                var boundaryRoot = CreateRoot(preview, roots, "TEST / orange boundary");
                var boundary = boundaryRoot.AddComponent<MomentumResetBoundary>();
                boundary.showBoundary = false;
                boundary.size = new Vector2(4, 4);
                boundary.cube = cube;
                boundaryRoot.SetActive(true);
                Enable(boundary);

                for (int i = 0; i < 3; i++)
                    Check(!boundary.CheckAndReturn() && Near(cube.Body.position, new Vector2(3, 0)),
                        "Initially outside cube remains untouched on check " + (i + 1), results);
                Check(MomentumResetBoundary.Controls(cube),
                    "Waiting for entry still overrides the legacy cyan boundary", results);

                SetPosition(cube, Vector2.zero);
                Check(!boundary.CheckAndReturn() && Near(cube.Body.position, Vector2.zero),
                    "Entering the region arms it without teleporting", results);
                cube.gravityDirection = Vector2.right;
                SetPosition(cube, new Vector2(4, 0));
                cube.Body.linearVelocity = new Vector2(8, -6);
                cube.Body.angularVelocity = 7;
                Check(boundary.CheckAndReturn() && Near(cube.Body.position, new Vector2(3, 0)) &&
                    Near(cube.Body.linearVelocity, new Vector2(8, -6)) && Near(cube.Body.angularVelocity, 7) &&
                    cube.gravityDirection == Vector2.right,
                    "Leaving after entry returns to the outside spawn and preserves motion and gravity", results);
                for (int i = 0; i < 3; i++)
                    Check(!boundary.CheckAndReturn() && Near(cube.Body.position, new Vector2(3, 0)),
                        "Outside return destination does not loop on check " + (i + 1), results);

                SetPosition(cube, Vector2.zero);
                boundary.CheckAndReturn();
                SetPosition(cube, new Vector2(4, 0));
                Check(boundary.CheckAndReturn() && Near(cube.Body.position, new Vector2(3, 0)),
                    "Reentering enables another exit return", results);

                SetPosition(cube, Vector2.zero);
                boundary.CheckAndReturn();
                boundary.enabled = false;
                SetPosition(cube, new Vector2(4, 0));
                boundary.enabled = true;
                Enable(boundary);
                Check(!boundary.CheckAndReturn() && Near(cube.Body.position, new Vector2(4, 0)) &&
                    Near(boundary.SpawnPosition, new Vector2(3, 0)),
                    "Reenable outside waits for entry without recapturing the original spawn", results);

                var insideCube = CreateCube(preview, roots, new Vector2(0, .25f));
                boundary.cube = insideCube;
                Enable(boundary);
                SetPosition(insideCube, new Vector2(3, .25f));
                Check(boundary.CheckAndReturn() && Near(insideCube.Body.position, new Vector2(0, .25f)),
                    "An initially inside cube retains immediate exit-return behavior", results);

                var marker = CreateRoot(preview, roots, "TEST / outside return marker");
                marker.transform.position = new Vector3(5, 6, 0);
                boundary.returnPoint = marker.transform;
                SetPosition(insideCube, new Vector2(3, 0));
                Check(boundary.CheckAndReturn() && Near(insideCube.Body.position, new Vector2(5, 6)) &&
                    !boundary.CheckAndReturn(),
                    "An independent outside ReturnPoint returns once without looping", results);
                SetPosition(insideCube, Vector2.zero);
                boundary.CheckAndReturn();

                var replacement = CreateCube(preview, roots, new Vector2(6, 1));
                boundary.cube = replacement;
                boundary.returnPoint = null;
                Check(!boundary.CheckAndReturn() && Near(replacement.Body.position, new Vector2(6, 1)) &&
                    !MomentumResetBoundary.Controls(insideCube) && MomentumResetBoundary.Controls(replacement),
                    "Changing Cube discards the previous armed state and changes legacy-reset ownership", results);
                SetPosition(replacement, Vector2.zero);
                boundary.CheckAndReturn();
                SetPosition(replacement, new Vector2(7, 1));
                Check(boundary.CheckAndReturn() && Near(replacement.Body.position, new Vector2(6, 1)) &&
                    !boundary.CheckAndReturn(),
                    "Replacement cube captures its own spawn and requires entry before return", results);

                boundary.transform.position = new Vector3(10, 20, 0);
                boundary.transform.rotation = Quaternion.Euler(0, 0, 90);
                boundary.transform.localScale = new Vector3(2, .5f, 1);
                Vector2 inside = boundary.transform.TransformPoint(new Vector3(1.5f, 1.5f, 0));
                Vector2 outside = boundary.transform.TransformPoint(new Vector3(2.2f, 0, 0));
                var rotatedCube = CreateCube(preview, roots, inside);
                rotatedCube.maxSpeed = 9;
                boundary.cube = rotatedCube;
                Enable(boundary);
                Check(boundary.Contains(inside) && !boundary.Contains(outside) && !boundary.CheckAndReturn(),
                    "Containment and entry state follow rotated and scaled boundaries", results);
                SetPosition(rotatedCube, outside);
                rotatedCube.Body.linearVelocity = new Vector2(30, -40);
                rotatedCube.Body.angularVelocity = 11;
                Check(boundary.CheckAndReturn() && Near(rotatedCube.Body.position, inside) &&
                    Near(rotatedCube.Body.linearVelocity.magnitude, 9) &&
                    Near(rotatedCube.Body.linearVelocity.normalized, new Vector2(.6f, -.8f)) &&
                    Near(rotatedCube.Body.angularVelocity, 11),
                    "Rotated exit-return respects the speed cap while retaining direction and angular velocity", results);
                rotatedCube.Body.linearVelocity = new Vector2(3, -4);
                Check(boundary.ReturnCube() && Near(rotatedCube.Body.linearVelocity, new Vector2(3, -4)),
                    "Explicit return retains unchanged velocity below the configured cap", results);

                results.Add("ALL CHECKS PASSED");
                return string.Join("\n", results.ToArray());
            }
            finally
            {
                for (int i = roots.Count - 1; i >= 0; i--)
                    if (roots[i])
                    {
                        var boundary = roots[i].GetComponent<MomentumResetBoundary>();
                        if (boundary) InvokeLifecycle(boundary, "OnDisable");
                        UnityEngine.Object.DestroyImmediate(roots[i]);
                    }
                EditorSceneManager.ClosePreviewScene(preview);
            }
        }

        static GameObject CreateRoot(Scene scene, List<GameObject> roots, string name)
        {
            var root = new GameObject(name) { hideFlags = HideFlags.HideAndDontSave };
            root.SetActive(false);
            SceneManager.MoveGameObjectToScene(root, scene);
            roots.Add(root);
            return root;
        }

        static DirectionTarget CreateCube(Scene scene, List<GameObject> roots, Vector2 position)
        {
            var root = CreateRoot(scene, roots, "TEST / cube");
            var body = root.AddComponent<Rigidbody2D>();
            body.gravityScale = 0;
            var cube = root.AddComponent<DirectionTarget>();
            cube.enabled = false; // Public methods work without running its Play Mode coroutine.
            root.SetActive(true);
            SetPosition(cube, position);
            return cube;
        }

        static void Enable(MomentumResetBoundary boundary)
        { InvokeLifecycle(boundary, "OnEnable"); }

        // Invoke directly because Unity does not dispatch runtime lifecycle messages in a preview scene.
        static void InvokeLifecycle(MomentumResetBoundary boundary, string method)
        {
            typeof(MomentumResetBoundary).GetMethod(method,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(boundary, null);
        }

        static void SetPosition(DirectionTarget cube, Vector2 position)
        {
            cube.transform.position = new Vector3(position.x, position.y, cube.transform.position.z);
            cube.Body.position = position;
        }

        static bool Near(Vector2 actual, Vector2 expected) { return (actual - expected).sqrMagnitude < .000001f; }
        static bool Near(float actual, float expected) { return Mathf.Abs(actual - expected) < .001f; }
        static void Check(bool condition, string message, List<string> results)
        {
            if (!condition) throw new Exception("FAIL: " + message);
            results.Add("PASS: " + message);
        }
    }
}
