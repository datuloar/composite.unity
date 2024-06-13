# if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEditor;
using UnityEngine;

namespace composite.unity.Core
{
    public class CompositeRootEditorWindow : EditorWindow
    {
        private List<CompositeRootBase> _order;
        private Dictionary<string, List<string>> _unresolvedDependencies;

        private GUIStyle _buttonStyle;
        private Vector2 _scrollPosition;

        public static void ShowWindow(List<CompositeRootBase> order)
        {
            var window = GetWindow<CompositeRootEditorWindow>("Composite Roots");
            window._order = order;
            window._unresolvedDependencies = new Dictionary<string, List<string>>();
            window.AnalyzeDependencies();
            window.Show();
        }

        private void OnEnable()
        {
            _buttonStyle = new GUIStyle(GUI.skin.button);
            _buttonStyle.alignment = TextAnchor.MiddleCenter;
            _buttonStyle.normal.textColor = Color.white;
            _buttonStyle.padding = new RectOffset(4, 4, 4, 4);
            _buttonStyle.margin = new RectOffset(7, 7, 7, 7);
            _buttonStyle.border = new RectOffset(7, 7, 7, 7);

            _buttonStyle.normal.background = MakeTexture((int)EditorGUIUtility.singleLineHeight,
                (int)EditorGUIUtility.singleLineHeight,
                HexToColor("#505050"));
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            if (_order == null)
            {
                EditorGUILayout.LabelField("No composite roots to display.");
                EditorGUILayout.EndScrollView();
                return;
            }

            EditorGUILayout.LabelField("Composite Roots Initialization Order:", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            var sortedOrder = _order.OrderBy(root => !_unresolvedDependencies.ContainsKey(root.GetType().Name) || _unresolvedDependencies[root.GetType().Name].Count == 0).ToList();

            foreach (var root in sortedOrder)
            {
                EditorGUILayout.BeginHorizontal();

                var buttonColor = _unresolvedDependencies.ContainsKey(root.GetType().Name) && _unresolvedDependencies[root.GetType().Name].Count > 0
                    ? Color.red
                    : Color.green;

                var buttonRect = GUILayoutUtility.GetRect(GUIContent.none, _buttonStyle, GUILayout.ExpandWidth(true));
                EditorGUI.DrawRect(buttonRect, buttonColor);

                _buttonStyle.normal.textColor = HexToColor("#D8D8D8");

                if (GUI.Button(buttonRect, root.GetType().Name, _buttonStyle))
                    ShowInScene(root);

                EditorGUILayout.EndHorizontal();

                if (_unresolvedDependencies.ContainsKey(root.GetType().Name) && _unresolvedDependencies[root.GetType().Name].Count > 0)
                {
                    foreach (var dependency in _unresolvedDependencies[root.GetType().Name])
                        EditorGUILayout.HelpBox($"Unresolved dependency: {dependency}", MessageType.Error);
                }
            }

            var hasUnresolvedDependencies = _order.Any(root => _unresolvedDependencies.ContainsKey(root.GetType().Name) && _unresolvedDependencies[root.GetType().Name].Count > 0);

            if (hasUnresolvedDependencies && GUILayout.Button("Auto Resolve Errors"))
            {
                _order.Sort((a, b) =>
                {
                    bool aHasErrors = _unresolvedDependencies.ContainsKey(a.GetType().Name) && _unresolvedDependencies[a.GetType().Name].Count > 0;
                    bool bHasErrors = _unresolvedDependencies.ContainsKey(b.GetType().Name) && _unresolvedDependencies[b.GetType().Name].Count > 0;

                    if (aHasErrors && !bHasErrors)
                        return 1;
                    else if (!aHasErrors && bHasErrors)
                        return -1;
                    else
                        return 0;
                });
            }

            RefreshWindow();

            EditorGUILayout.EndScrollView();
        }

        private void ShowInScene(CompositeRootBase root)
        {
            Selection.activeObject = root.gameObject;
            EditorGUIUtility.PingObject(root.gameObject);
        }

        private void AnalyzeDependencies()
        {
            _unresolvedDependencies.Clear();
            var createdDependencies = new HashSet<string>();

            foreach (var root in _order)
            {
                var methods = root.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);

                foreach (var method in methods)
                {
                    if (method.Name == "InstallBindings")
                    {
                        var il = method.GetMethodBody().GetILAsByteArray();
                        AnalyzeMethodIL(il, createdDependencies, root.GetType().Name);
                    }
                }
            }
        }

        private void AnalyzeMethodIL(byte[] il, HashSet<string> createdDependencies, string rootName)
        {
            for (int i = 0; i < il.Length; i++)
            {
                if (il[i] == OpCodes.Call.Value || il[i] == OpCodes.Callvirt.Value)
                {
                    int token = BitConverter.ToInt32(il, i + 1);
                    var method = typeof(CompositeRootBase).Module.ResolveMethod(token) as MethodInfo;

                    if (method != null)
                    {
                        if (method.Name.StartsWith("BindAsGlobal") || method.Name.StartsWith("BindAsLocal"))
                        {
                            var dependencyType = method.GetGenericArguments()[0];
                            createdDependencies.Add(dependencyType.Name);
                        }
                        else if (method.Name.StartsWith("GetGlobal") || method.Name.StartsWith("GetLocal"))
                        {
                            var dependencyType = method.GetGenericArguments()[0];
                            if (!createdDependencies.Contains(dependencyType.Name))
                            {
                                if (!_unresolvedDependencies.ContainsKey(rootName))
                                {
                                    _unresolvedDependencies[rootName] = new List<string>();
                                }
                                _unresolvedDependencies[rootName].Add(dependencyType.Name);
                            }
                        }
                    }
                    i += 4; // Skip the token bytes
                }
            }
        }

        private void RefreshWindow()
        {
            _unresolvedDependencies.Clear();
            AnalyzeDependencies();
            Repaint();
        }

        private Texture2D MakeTexture(int width, int height, Color color)
        {
            var pixel = new Color[width * height];

            for (int i = 0; i < pixel.Length; ++i)
                pixel[i] = color;

            var result = new Texture2D(width, height);
            result.SetPixels(pixel);
            result.Apply();

            return result;
        }

        private Color HexToColor(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var color);
            return color;
        }
    }
}
#endif