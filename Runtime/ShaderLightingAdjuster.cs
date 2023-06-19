using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AvatarLightingTuner
{
    public class ShaderLightingAdjuster : EditorWindow
    {
        private class MaterialItem
        {
            public Material Material;
            public bool MeshFoldFlag;
            public bool IsChecked;
            public HashSet<Renderer> MRList;
            public MaterialItem(Material Mat)
            {
                this.MeshFoldFlag = false;
                this.IsChecked = false;
                this.MRList = new HashSet<Renderer> { };
                this.Material = Mat;
            }
            public MaterialItem AddRenderer(Renderer R)
            {
                MRList.Add(R);
                return this;
            }
        }

        private HashSet<Material> MaterialList = new HashSet<Material>();
        private Dictionary<Material, MaterialItem> MaterialItems = new Dictionary<Material, MaterialItem>();
        private GameObject RootObject;
        private bool lilMinFlag = false;
        private bool lilMaxFlag = false;
        private float lilMin = 0.05f;
        private float lilMax = 1;
        private float utsVal = 1;

        private Vector2 _scrollPosition = Vector2.zero;
        [MenuItem("Tools/AvatarLightingTuner/ShaderLightingAdjuster")]
        public static void ShowWindow()
        {
            var window = EditorWindow.GetWindow(typeof(ShaderLightingAdjuster), false, "ShaderLightingAdjuster v2.0");
        }
        void OnGUI()
        {
            //親オブジェクト//
            RootObject = EditorGUILayout.ObjectField("オブジェクト/Object", RootObject, typeof(GameObject), true) as GameObject;
            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(2));
            //子要素を取得
            if (GUILayout.Button("リスト化/ListUp"))
            {
                MaterialList.Clear();
                MaterialItems.Clear();

                if (RootObject == null) return;

                var SMRList = RootObject.GetComponentsInChildren<MeshRenderer>(true).ToList();
                var MRList = RootObject.GetComponentsInChildren<SkinnedMeshRenderer>(true).ToList();

                MaterialList = new HashSet<Material>(MRList.SelectMany(SMR => SMR.sharedMaterials).Where(M => M != null));
                MaterialList.UnionWith(SMRList.SelectMany(SMR => SMR.sharedMaterials).Where(M => M != null));

                MaterialItems = MaterialList.ToDictionary(ML => ML, ML => new MaterialItem(ML));

                SMRList.ForEach(SMR => SMR.sharedMaterials.Where(M => M != null).ToList().ForEach(M => MaterialItems[M].AddRenderer(SMR)));
                MRList.ForEach(MR => MR.sharedMaterials.Where(M => M != null).ToList().ForEach(M => MaterialItems[M].AddRenderer(MR)));
            }

            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(2));
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            var groupListLIL = new SortedDictionary<float, List<Material>>();
            var groupListUTS = new SortedDictionary<float, List<Material>>();
            var groupListUSP = new List<Material>();

            foreach (var material in MaterialList)
            {
                if (material.HasProperty("_LightMinLimit"))
                {
                    float value = material.GetFloat("_LightMinLimit");
                    if (groupListLIL.ContainsKey(value))
                        groupListLIL[value].Add(material);
                    else
                        groupListLIL.Add(value, new List<Material>() { material });
                }
                else if (material.HasProperty("_Unlit_Intensity"))
                {
                    float value = material.GetFloat("_Unlit_Intensity");
                    if (groupListUTS.ContainsKey(value))
                        groupListUTS[value].Add(material);
                    else
                        groupListUTS.Add(value, new List<Material>() { material });
                }
                else
                    groupListUSP.Add(material);

            }

            if (groupListLIL.Any())
            {
                EditorGUILayout.LabelField("liltoon", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                using (new EditorGUILayout.HorizontalScope())
                {
                    lilMinFlag = EditorGUILayout.Toggle(lilMinFlag, GUILayout.MaxWidth(30f));
                    lilMin = EditorGUILayout.Slider("LightMinLimit", lilMin, 0, 1);
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    lilMaxFlag = EditorGUILayout.Toggle(lilMaxFlag, GUILayout.MaxWidth(30f));
                    lilMax = EditorGUILayout.Slider("LightMaxLimit", lilMax, 0, 1);
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("全選択/Select All"))
                        groupListLIL.ToList().ForEach(matGroup => matGroup.Value.ForEach(mat => MaterialItems[mat].IsChecked = true));

                    if (GUILayout.Button("全選択解除/UnSelect All"))
                        groupListLIL.ToList().ForEach(matGroup => matGroup.Value.ForEach(mat => MaterialItems[mat].IsChecked = false));
                }

                groupListLIL.ToList().ForEach(group =>
                {
                    EditorGUILayout.LabelField("LightMinLimit: " + group.Key.ToString(), EditorStyles.boldLabel);
                    group.Value.ForEach(mat => DrawMaterialInfo(MaterialItems[mat], true, true));
                    EditorGUILayout.Space(10);
                });

                if (GUILayout.Button("適用/Apply - liltoon"))
                {
                    groupListLIL.SelectMany(group => group.Value).Select(mat => MaterialItems[mat]).Where(MI => MI.IsChecked).ToList().ForEach(MI =>
                    {
                        if (lilMinFlag)
                            MI.Material.SetFloat("_LightMinLimit", lilMin);
                        if (lilMaxFlag)
                            MI.Material.SetFloat("_LightMaxLimit", lilMax);
                    });
                }
                EditorGUILayout.Space(20);
                EditorGUI.indentLevel--;
            }
            if (groupListUTS.Count != 0)
            {
                EditorGUILayout.LabelField("UTS", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("(UTS)Unlit_Intensity 1 ≒ (liltoon)LightMinLimit 0.05 ", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                using (new EditorGUILayout.HorizontalScope())
                {
                    utsVal = EditorGUILayout.Slider("Unlit_Intensity", utsVal, 0.001f, 4);
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("全選択/Select All"))
                        groupListUTS.ToList().ForEach(matGroup => matGroup.Value.ForEach(mat => MaterialItems[mat].IsChecked = true));

                    if (GUILayout.Button("全選択解除/UnSelect All"))
                        groupListUTS.ToList().ForEach(matGroup => matGroup.Value.ForEach(mat => MaterialItems[mat].IsChecked = false));
                }
                groupListUTS.ToList().ForEach(group =>
                {
                    EditorGUILayout.LabelField("_Unlit_Intensity: " + group.Key.ToString(), EditorStyles.boldLabel);
                    group.Value.ForEach(mat => DrawMaterialInfo(MaterialItems[mat], true, false));
                    EditorGUILayout.Space(10);
                });

                if (GUILayout.Button("適用/Apply - UTS"))
                    groupListUTS.SelectMany(group => group.Value).Select(mat => MaterialItems[mat]).Where(MI => MI.IsChecked).ToList().ForEach(MI =>
                        MI.Material.SetFloat("_Unlit_Intensity", utsVal));

                EditorGUILayout.Space(20);
                EditorGUI.indentLevel--;
            }
            if (groupListUSP.Count != 0)
            {
                EditorGUILayout.LabelField("非対応/Unsupported", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                groupListUSP.ForEach(mat => DrawMaterialInfo(MaterialItems[mat], false, false));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawMaterialInfo(MaterialItem matItem, bool showCheckBox, bool drawLightMaxLimit)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (showCheckBox)
                    matItem.IsChecked = EditorGUILayout.Toggle(matItem.IsChecked, GUILayout.MaxWidth(30f));

                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.ObjectField(matItem.Material, typeof(Material), false);

                if (drawLightMaxLimit && matItem.Material.HasProperty("_LightMaxLimit"))
                    EditorGUILayout.LabelField("LightMaxLimit: " + matItem.Material.GetFloat("_LightMaxLimit"));
                using (new GUILayout.VerticalScope())
                {
                    if (matItem.MeshFoldFlag = EditorGUILayout.Foldout(matItem.MeshFoldFlag, "Mesh"))
                    {
                        foreach (var mesh in matItem.MRList)
                            using (new EditorGUI.DisabledScope(true))
                                EditorGUILayout.ObjectField(mesh, typeof(Renderer), false);
                    }
                }
            }
        }
    }
}