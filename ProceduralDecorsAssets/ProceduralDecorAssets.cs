using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Procedural Decor Assets Tool
/// </summary>
[System.Serializable]
public class ProceduralDecorAssets : EditorWindow
{
    //==================================================================
    //Fields
    //==================================================================

    //Bool
    private bool enableRotateVariation = false;
    private bool enableSizeVariation = false;
    private bool groupeInstantiatedObject = false;
    private bool destroyTransformToPlace = false;
    //Int
    private int transformToPlaceCount = 10;
    private const int TRANSFORMTOPLACECOUNTDELFAULT = 10;
    private int maxAttempts = 30;
    private const int MAXATTEMPSDELFAULT = 30;
    private const int LAYERDECOR = 12;
    //Float
    private float seed = 2;
    private const float SEEDDELFAULT = 2;
    private float seedDevide = 2;
    private const float SEEDDEVIDEDEFAULT = 2;
    //String
    [SerializeField] private string transformToPlacePrefab = "Assets/Editor/ProceduralDecorsAssets/TransformToPlace.prefab";
    private const string TAGTOFIND = "Finish";
    private string groupName = "Default";
    private string rapport;
    //Vector
    private Vector2 scroll;
    [SerializeField] private Vector2 rotationVariation = new Vector2(0, 0);
    [SerializeField] private Vector2 sizeVariation = new Vector2(1, 1);
    //GameObject
    private GameObject group;
    private List<GameObject> prefabs = new List<GameObject>();
    private List<GameObject> transforms = new List<GameObject>();

    //==================================================================
    //Methods
    //==================================================================

    /// <summary>
    /// Find all transforms to place in the current scene.
    /// </summary>
    /// <returns>Founded transforms to place.</returns>
    int FindTransforms()
    {
        transforms.Clear();
        foreach (GameObject _transforms in GameObject.FindGameObjectsWithTag(TAGTOFIND))
        {
            transforms.Add(_transforms);
        }
        rapport = transforms.Count.ToString() + " transform(s) finded.";
        return transforms.Count;
    }

    /// <summary>
    /// Add a new transforms to place in the current scene.
    /// </summary>
    void AddTransformManual()
    {
        GameObject _transformToPlace = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(transformToPlacePrefab)) as GameObject;
        if (_transformToPlace == null)
        {
            rapport = "Failed to load Transform to Place prefab.";
            return;
        }
        SceneView _vue = SceneView.lastActiveSceneView;
        _transformToPlace.transform.position = _vue ? _vue.pivot : Vector3.zero;
        StageUtility.PlaceGameObjectInCurrentStage(_transformToPlace);
        GameObjectUtility.EnsureUniqueNameForSibling(_transformToPlace);
        Selection.activeGameObject = _transformToPlace;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    /// <summary>
    /// Add transforms to place on a Nav Mesh.
    /// </summary>
    void AddTransformByNavMesh()
    {
        for (int _i = 0; _i < transformToPlaceCount; _i++)
        {
            GameObject _transformToPlace = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(transformToPlacePrefab)) as GameObject;
            if (_transformToPlace == null)
            {
                rapport = "Failed to load Transform to Place prefab.";
                return;
            }
            Vector3 _randomPosition;
            if (TryGetRandomPointOnNavMesh(out _randomPosition))
            {
                _transformToPlace.transform.position = _randomPosition;
            }
            else
            {
                rapport = "Failed to find a valid position on the NavMesh.";
                DestroyImmediate(_transformToPlace);
                return;
            }
            StageUtility.PlaceGameObjectInCurrentStage(_transformToPlace);
            GameObjectUtility.EnsureUniqueNameForSibling(_transformToPlace);
            Selection.activeGameObject = _transformToPlace;
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }
    }

    /// <summary>
    /// Try to get random point on the Nav Mesh.
    /// </summary>
    /// <param name="result">Randomized point on Nav Mesh.</param>
    /// <returns>The randomized point is valid.</returns>
    bool TryGetRandomPointOnNavMesh(out Vector3 result)
    {
        NavMeshTriangulation _navMeshData = NavMesh.CalculateTriangulation();
        int _maxTries = maxAttempts;
        for (int _i = 0; _i < _maxTries; _i++)
        {
            int _t = Random.Range(0, _navMeshData.indices.Length / 3);
            Vector3 _vertex1 = _navMeshData.vertices[_navMeshData.indices[_t * 3]];
            Vector3 _vertex2 = _navMeshData.vertices[_navMeshData.indices[_t * 3 + 1]];
            Vector3 _vertex3 = _navMeshData.vertices[_navMeshData.indices[_t * 3 + 2]];
            Vector3 _randomPoint = RandomPointInTriangle(_vertex1, _vertex2, _vertex3);
            NavMeshHit _hit;
            if (NavMesh.SamplePosition(_randomPoint, out _hit, seed, NavMesh.AllAreas))
            {
                float _distanceToEdge = Mathf.Min(
                    Vector3.Distance(_hit.position, _vertex1),
                    Vector3.Distance(_hit.position, _vertex2),
                    Vector3.Distance(_hit.position, _vertex3)
                );
                if (_distanceToEdge > seed / seedDevide)
                {
                    result = _hit.position;
                    return true;
                }
            }
        }
        result = Vector3.zero;
        return false;
    }

    /// <summary>
    /// Get random point on a triangle.
    /// </summary>
    /// <param name="v1">First oint on a Nav Mesh.</param>
    /// <param name="v2">Second point on a Nav Mesh.</param>
    /// <param name="v3">Third point on a Nav Mesh.</param>
    /// <returns>Randomized point.</returns>
    Vector3 RandomPointInTriangle(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        float _r1 = Random.value;
        float _r2 = Random.value;
        if (_r1 + _r2 >= 1)
        {
            _r1 = 1 - _r1;
            _r2 = 1 - _r2;
        }
        return v1 + _r1 * (v2 - v1) + _r2 * (v3 - v1);
    }

    /// <summary>
    /// Delete all transforms to place in the current scene.
    /// </summary>
    void DeleteTransforms()
    {
        foreach (GameObject _transforms in GameObject.FindGameObjectsWithTag(TAGTOFIND))
        {
            DestroyImmediate(_transforms);
        }
        transforms.Clear();
        rapport = "All transforms are deleted.";
    }

    /// <summary>
    /// Error gestion (transforms to place not found, no prefab in the prefab list or an empty element is detected in the prefab list).
    /// </summary>
    /// <returns>A error was encountered.</returns>
    bool ErrorDetected()
    {
        FindTransforms();
        if (transforms.Count == 0)
        {
            rapport = "Transforms not founded.";
            return true;
        }
        if (prefabs.Count == 0)
        {
            rapport = "Prefabs list is empty.";
            return true;
        }
        else
        {
            for (int _i = 0; _i < prefabs.Count; _i++)
            {
                if (prefabs[_i] == null)
                {
                    rapport = "An empty element in prefabs list is detected.";
                    return true;
                }
            }
        }
        rapport = "Ready to instance.";
        return false;
    }

    /// <summary>
    /// Reset the Procedural Decor Assets Tool.
    /// </summary>
    void ResetAll()
    {
        DeleteTransforms();
        prefabs.Clear();
        transformToPlaceCount = TRANSFORMTOPLACECOUNTDELFAULT;
        enableRotateVariation = false;
        enableSizeVariation = false;
        destroyTransformToPlace = false;
        maxAttempts = MAXATTEMPSDELFAULT;
        seed = SEEDDELFAULT;
        seedDevide = SEEDDEVIDEDEFAULT;
        rotationVariation = Vector2.zero;
        sizeVariation = Vector2.zero;
        groupeInstantiatedObject = false;
        groupName = "Default";
        rapport = "Tool reinitialized.";
    }

    /// <summary>
    /// Begin the instantiation of the prefab on transforms to place.
    /// </summary>
    void InstanceRandomly()
    {
        if (ErrorDetected() == true) return;
        else
        {
            if (groupeInstantiatedObject)
            {
                group = new GameObject();
                group.name = groupName;
                Instantiate(group, new Vector3(0, 0, 0), Quaternion.identity);
                DestroyImmediate(GameObject.Find(group.name + "(Clone)"));
            }
            int _transformFinded = FindTransforms();
            for (int _i = 0; _i < _transformFinded; _i++)
            {
                int _randomPrefab = UnityEngine.Random.Range(0, prefabs.Count);
                GameObject _preparationPrefab = Instantiate(prefabs[_randomPrefab], transforms[_i].transform.position, Quaternion.identity);
                if (enableRotateVariation == true)
                {
                    float _temporaryRotate = UnityEngine.Random.Range(rotationVariation.x, rotationVariation.y);
                    _preparationPrefab.transform.Rotate(0, _temporaryRotate, 0, 0);
                }
                if (enableSizeVariation == true)
                {
                    float _tempoarySize = UnityEngine.Random.Range(sizeVariation.x, sizeVariation.y);
                    _preparationPrefab.transform.localScale = new Vector3(_tempoarySize, _tempoarySize, _tempoarySize);
                }
                _preparationPrefab.layer = LAYERDECOR;
                if (groupeInstantiatedObject) _preparationPrefab.transform.parent = group.transform;
                rapport += ("\r\nPrefab " + _randomPrefab.ToString() + " placed in " + transforms[_i].transform.position.ToString() + ".");
            }
        }
        if (destroyTransformToPlace) DeleteTransforms();
    }

    /// <summary>
    /// Save preferences as a JSON file.
    /// </summary>
    void SavePreferences()
    {
        if (prefabs.Count == 0) rapport = "No prefabs list to save.";
        else
        {
            //Initialize save
            List<string> _prefabPaths = new List<string>();
            foreach (GameObject _prefab in prefabs)
            {
                string _prefabPath = AssetDatabase.GetAssetPath(_prefab);
                _prefabPaths.Add(_prefabPath);
            }
            string _filePath = EditorUtility.SaveFilePanel("Save prefabs list as JSON", "", "PrefabsData", "json");
            if (string.IsNullOrEmpty(_filePath)) return;
            //Execution save
            PrefabListData _prefabListData = new PrefabListData();
            _prefabListData.PrefabPaths = _prefabPaths;
            _prefabListData.TransformToPlace = transformToPlacePrefab;
            _prefabListData.EnableRotateVariation = enableRotateVariation;
            _prefabListData.RotationVariation = rotationVariation;
            _prefabListData.EnableSizeVariation = enableSizeVariation;
            _prefabListData.SizeVariation = sizeVariation;
            _prefabListData.DestroyTransformToPlace = destroyTransformToPlace;
            _prefabListData.MaxAttemps = maxAttempts;
            _prefabListData.Seed = seed;
            _prefabListData.SeedDivide = seedDevide;
            string _jsonData = JsonUtility.ToJson(_prefabListData, true);
            File.WriteAllText(_filePath, _jsonData);
            rapport = "Prefab paths saved to JSON: " + _filePath;
        }
    }

    /// <summary>
    /// Load preferences by a JSON file.
    /// </summary>
    void LoadPreferences()
    {
        //Initialize load
        string _filePath = EditorUtility.OpenFilePanel("Load prefabs list from JSON", "", "json");
        if (string.IsNullOrEmpty(_filePath)) return;
        string _jsonData = File.ReadAllText(_filePath);
        PrefabListData _prefabListData = JsonUtility.FromJson<PrefabListData>(_jsonData);
        if (_prefabListData == null || _prefabListData.PrefabPaths == null)
        {
            rapport = "Failed to load prefab list data from JSON.";
            return;
        }
        prefabs.Clear();
        //Execution load
        foreach (string _prefabPath in _prefabListData.PrefabPaths)
        {
            GameObject _prefab = AssetDatabase.LoadAssetAtPath<GameObject>(_prefabPath);
            if (_prefab != null)  prefabs.Add(_prefab);
            else
            {
                rapport = "Failed to load prefab at path: " + _prefabPath;
                return;
            }
        }
        transformToPlacePrefab = _prefabListData.TransformToPlace;
        enableRotateVariation = _prefabListData.EnableRotateVariation;
        rotationVariation = _prefabListData.RotationVariation;
        enableSizeVariation = _prefabListData.EnableSizeVariation;
        sizeVariation = _prefabListData.SizeVariation;
        destroyTransformToPlace = _prefabListData.DestroyTransformToPlace;
        maxAttempts = _prefabListData.MaxAttemps;
        seed = _prefabListData.Seed;
        seedDevide = _prefabListData.SeedDivide;
        rapport = "Prefab list loaded from JSON: " + _filePath;
    }

    /// <summary>
    /// Draw a GuiLine on the Procedural Decors Assets Tool window.
    /// </summary>
    /// <param name="height">Height of spacing.</param>
    void GuiLine(int height)
    {
        Color _color = new Color(0.5f, 0.5f, 0.5f, 1);
        Rect _rect = EditorGUILayout.GetControlRect(false, height);
        _rect.height = height;
        EditorGUI.DrawRect(_rect, _color);
    }

    //==================================================================
    //Automatic methods
    //==================================================================

    [MenuItem("Tools/Procedural decor Assets")]
    public static void ShowWindow() => GetWindow<ProceduralDecorAssets>("Procedural decor asset");

    private void OnEnable() => ResetAll();

    private void OnDisable() => ResetAll();

    void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll, false, true);
        GUILayout.Label("Procedural Decor Assets", EditorStyles.largeLabel);
        //Prefabs
        GUILayout.Space(10);
        GuiLine(1);
        GUILayout.Space(5);
        GUILayout.Label("Main parameters", EditorStyles.boldLabel);
        GUILayout.Space(5);
        GUILayout.Label("TransformToPlace prefab path", EditorStyles.boldLabel);
        transformToPlacePrefab = EditorGUILayout.TextField(transformToPlacePrefab);
        if (GUILayout.Button("Add prefab"))
        {
            prefabs.Add(null);
        }
        if (GUILayout.Button("Delete all prefabs"))
        {
            if (prefabs.Count > 0) prefabs.Clear();
            else rapport = "No prefab to delete.";
        }
        for (int _i = 0; _i < prefabs.Count; _i++) prefabs[_i] = (GameObject)EditorGUILayout.ObjectField("Prefab to place: " + _i, prefabs[_i], typeof(GameObject), true);
        enableRotateVariation = GUILayout.Toggle(enableRotateVariation, "Enable rotate Y variation");
        rotationVariation = EditorGUILayout.Vector2Field("Rotate variation", rotationVariation);
        enableSizeVariation = GUILayout.Toggle(enableSizeVariation, "Enable size variation");
        sizeVariation = EditorGUILayout.Vector2Field("Size variation", sizeVariation);
        destroyTransformToPlace = GUILayout.Toggle(destroyTransformToPlace, "Destroy TTP after instance");
        transformToPlaceCount = EditorGUILayout.IntField("TTP count for NavMesh", transformToPlaceCount);
        maxAttempts = EditorGUILayout.IntField("NavMesh max attemps", maxAttempts);
        seed = EditorGUILayout.FloatField("NavMesh seed", seed);
        seedDevide = EditorGUILayout.FloatField("NavMesh seed divider", seedDevide);
        if (GUILayout.Button("Save preferences as JSON file")) SavePreferences();
        if (GUILayout.Button("Load preferences as JSON file")) LoadPreferences();
        //Transform to place
        GUILayout.Space(10);
        GuiLine(1);
        GUILayout.Space(5);
        GUILayout.Label("Transform to place", EditorStyles.boldLabel);
        if (GUILayout.Button("Find transforms to place")) FindTransforms();
        if (GUILayout.Button("Place individual manualy transform to place")) AddTransformManual();
        if (GUILayout.Button("Place transform to place by Nav Mesh")) AddTransformByNavMesh();
        if (GUILayout.Button("Delete all transforms to place")) DeleteTransforms();
        //Instance
        GUILayout.Space(10);
        GuiLine(1);
        GUILayout.Space(5);
        GUILayout.Label("Instance", EditorStyles.boldLabel);
        if (GUILayout.Button("Reset all")) ResetAll();
        if (GUILayout.Button("Check error")) ErrorDetected();
        groupeInstantiatedObject = GUILayout.Toggle(groupeInstantiatedObject, "Group instantiated objects");
        groupName = EditorGUILayout.TextField("Groupe name", groupName);
        if (GUILayout.Button("Instance randomly prefab to Transforms")) InstanceRandomly();
        //Feedback
        GUILayout.Space(10);
        GuiLine(1);
        GUILayout.Space(5);
        GUILayout.Label("Rapport", EditorStyles.boldLabel);
        EditorGUILayout.TextArea(rapport, GUILayout.Height(200), GUILayout.Width(300), GUILayout.ExpandWidth(false));
        if (GUILayout.Button("Clean rapport")) rapport = "";
        EditorGUILayout.EndScrollView();
    }
}

/// <summary>
/// Prefab List Data
/// </summary>
[System.Serializable]
public class PrefabListData
{
    //==================================================================
    //Fields
    //==================================================================
        
    //Bool
    [SerializeField] private bool enableRotateVariation;
    [SerializeField] private bool enableSizeVariation;
    [SerializeField] private bool destroyTransformToPlace;
    //Int
    [SerializeField] private int maxAttemps;
    //Float
    [SerializeField] private float seed;
    [SerializeField] private float seedDivide;
    //Vector2
    [SerializeField] private Vector2 rotationVariation = new Vector2(0, 0);
    [SerializeField] private Vector2 sizeVariation = new Vector2(1, 1);
    //String
    [SerializeField] private string transformToPlace;
    [SerializeField] private List<string> prefabPaths;

    //==================================================================
    //Methods
    //==================================================================

    public bool EnableRotateVariation
    {
        get { return enableRotateVariation; }
        set { enableRotateVariation = value; }
    }

    public bool EnableSizeVariation
    {
        get { return enableSizeVariation; }
        set { enableSizeVariation = value; }
    }

    public bool DestroyTransformToPlace
    {
        get { return destroyTransformToPlace; }
        set { destroyTransformToPlace = value; }
    }

    public int MaxAttemps
    {
        get { return maxAttemps; }
        set { maxAttemps = value; }
    }

    public float Seed
    {
        get { return seed; }
        set { seed = value; }
    }

    public float SeedDivide
    {
        get { return seedDivide; }
        set { seedDivide = value; }
    }

    public Vector2 RotationVariation
    {
        get { return rotationVariation; }
        set { rotationVariation = value; }
    }

    public Vector2 SizeVariation
    {
        get { return sizeVariation; }
        set { sizeVariation = value; }
    }

    public string TransformToPlace
    {
        get { return transformToPlace; }
        set { transformToPlace = value; }
    }

    public List<string> PrefabPaths
    {
        get { return prefabPaths; }
        set { prefabPaths = value; }
    }
}
