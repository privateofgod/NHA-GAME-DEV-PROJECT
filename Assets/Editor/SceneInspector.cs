using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class SceneInspector
{
    static SceneInspector()
    {
        Run();
    }

    [MenuItem("Tools/Inspect Scene")]
    public static void Run()
    {
        try
        {
            var sb = new StringBuilder();
            var scene = SceneManager.GetActiveScene();
            sb.AppendLine("TIME: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("IS_PLAYING: " + EditorApplication.isPlaying);
            sb.AppendLine("ACTIVE SCENE: " + scene.name + " (" + scene.path + ")");
            sb.AppendLine("SCENE IS DIRTY: " + scene.isDirty);

            sb.AppendLine("\n=== ALL CAMERAS ===");
            var cameras = UnityEngine.Object.FindObjectsOfType<Camera>();
            foreach (var cam in cameras)
            {
                sb.AppendLine($"Camera: '{cam.gameObject.name}' (Active: {cam.gameObject.activeInHierarchy}, Enabled: {cam.enabled})");
                sb.AppendLine($"  Pos: {cam.transform.position}, Rot: {cam.transform.eulerAngles}, Fwd: {cam.transform.forward}");
                sb.AppendLine($"  CullingMask: {cam.cullingMask} (Binary: {Convert.ToString(cam.cullingMask, 2)})");
                sb.AppendLine($"  Near: {cam.nearClipPlane}, Far: {cam.farClipPlane}, FOV: {cam.fieldOfView}, Depth: {cam.depth}");
                sb.AppendLine($"  TargetTexture: {(cam.targetTexture != null ? cam.targetTexture.name : "null")}");
                foreach (var c in cam.GetComponents<Component>())
                {
                    if (c != null)
                        sb.AppendLine($"  Component: {c.GetType().FullName}");
                }
            }

            sb.AppendLine("\n=== SEARCHING FOR CAR OBJECTS ===");
            var allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
            foreach (var t in allTransforms)
            {
                if (t.gameObject.name.ToLower().Contains("car"))
                {
                    // Check if it belongs to the current scene
                    bool inScene = t.gameObject.scene == scene;
                    sb.AppendLine($"Car Object: '{t.gameObject.name}', Scene: '{t.gameObject.scene.name}' (InCurrentScene: {inScene})");
                    sb.AppendLine($"  ActiveSelf: {t.gameObject.activeSelf}, ActiveInHierarchy: {t.gameObject.activeInHierarchy}");
                    sb.AppendLine($"  Layer: {t.gameObject.layer} ({LayerMask.LayerToName(t.gameObject.layer)})");
                    sb.AppendLine($"  Tag: '{t.gameObject.tag}'");
                    sb.AppendLine($"  Pos: {t.position}, LocalPos: {t.localPosition}");
                    sb.AppendLine($"  Rot: {t.eulerAngles}, LocalRot: {t.localEulerAngles}");
                    sb.AppendLine($"  Scale: {t.localScale}, LossyScale: {t.lossyScale}");

                    // Hierarchy path
                    string path = t.name;
                    Transform p = t.parent;
                    while (p != null)
                    {
                        path = p.name + "/" + path;
                        p = p.parent;
                    }
                    sb.AppendLine($"  Hierarchy Path: {path}");

                    // Components
                    foreach (var c in t.GetComponents<Component>())
                    {
                        if (c == null)
                        {
                            sb.AppendLine("  Component: NULL (Missing script?)");
                            continue;
                        }
                        sb.AppendLine($"  Component: {c.GetType().FullName}");
                        if (c is Renderer r)
                        {
                            sb.AppendLine($"    Renderer Enabled: {r.enabled}, isVisible: {r.isVisible}");
                            sb.AppendLine($"    Bounds: center={r.bounds.center}, extents={r.bounds.extents}");
                            sb.AppendLine($"    Materials count: {r.sharedMaterials.Length}");
                            for (int i = 0; i < r.sharedMaterials.Length; i++)
                            {
                                var m = r.sharedMaterials[i];
                                sb.AppendLine($"      Mat[{i}]: {(m != null ? m.name + " (Shader: " + (m.shader != null ? m.shader.name : "null") + ")" : "null")}");
                            }
                            sb.AppendLine($"    RenderingLayerMask: {r.renderingLayerMask}");
                        }
                        if (c is Animator anim)
                        {
                            sb.AppendLine($"    Animator Enabled: {anim.enabled}");
                            sb.AppendLine($"    Controller: {(anim.runtimeAnimatorController != null ? anim.runtimeAnimatorController.name : "null")}");
                            sb.AppendLine($"    Avatar: {(anim.avatar != null ? anim.avatar.name : "null")}");
                            sb.AppendLine($"    ApplyRootMotion: {anim.applyRootMotion}");
                            sb.AppendLine($"    CullingMode: {anim.cullingMode}");
                            sb.AppendLine($"    UpdateMode: {anim.updateMode}");
                            if (anim.runtimeAnimatorController is AnimatorController ac)
                            {
                                foreach (var layer in ac.layers)
                                {
                                    sb.AppendLine($"      AC Layer: {layer.name}, default state: {(layer.stateMachine.defaultState != null ? layer.stateMachine.defaultState.name : "null")}");
                                    foreach (var childState in layer.stateMachine.states)
                                    {
                                        var motion = childState.state.motion;
                                        sb.AppendLine($"        State: {childState.state.name}, Motion: {(motion != null ? motion.name : "null")}");
                                        if (motion is AnimationClip clip)
                                        {
                                            sb.AppendLine($"          Clip length: {clip.length}, loop: {clip.isLooping}, wrapMode: {clip.wrapMode}");
                                            var bindings = AnimationUtility.GetCurveBindings(clip);
                                            foreach (var b in bindings)
                                            {
                                                var curve = AnimationUtility.GetEditorCurve(clip, b);
                                                sb.AppendLine($"          Curve: {b.path} / {b.propertyName} (keys: {curve.length}, firstKey: {(curve.length > 0 ? curve[0].value.ToString() : "none")}, lastKey: {(curve.length > 0 ? curve[curve.length - 1].value.ToString() : "none")})");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // Frustum check with Main Camera
                    if (Camera.main != null)
                    {
                        var cam = Camera.main;
                        var rend = t.GetComponentInChildren<Renderer>();
                        if (rend != null)
                        {
                            var planes = GeometryUtility.CalculateFrustumPlanes(cam);
                            bool insidePlanes = GeometryUtility.TestPlanesAABB(planes, rend.bounds);
                            sb.AppendLine($"  Camera.main Frustum Test (AABB): {insidePlanes}");
                        }
                        var vpos = cam.WorldToViewportPoint(t.position);
                        sb.AppendLine($"  Camera.main WorldToViewportPoint: {vpos} (z > 0 is in front of camera)");
                        var spos = cam.WorldToScreenPoint(t.position);
                        sb.AppendLine($"  Camera.main WorldToScreenPoint: {spos}");
                        bool inScreen = vpos.z > 0 && vpos.x >= 0 && vpos.x <= 1 && vpos.y >= 0 && vpos.y <= 1;
                        sb.AppendLine($"  IsInsideViewportRect: {inScreen}");
                        // Check layer match
                        bool layerMatched = (cam.cullingMask & (1 << t.gameObject.layer)) != 0;
                        sb.AppendLine($"  Camera CullingMask includes layer ({t.gameObject.layer}): {layerMatched}");
                    }
                }
            }

            File.WriteAllText("Assets/scene_inspection_result.txt", sb.ToString());
            Debug.Log("[SceneInspector] Written detailed results to Assets/scene_inspection_result.txt");
        }
        catch (Exception ex)
        {
            File.WriteAllText("Assets/scene_inspection_result.txt", "ERROR: " + ex.ToString());
        }
    }
}
