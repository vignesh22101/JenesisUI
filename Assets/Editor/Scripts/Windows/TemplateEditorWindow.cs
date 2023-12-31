﻿using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class TemplateEditorWindow : EditorWindow
{
    private const string PathToTemplates = "Assets/Templates";
    private const string BlueprintJsonPath = "Assets/Templates/Example/Example.json";
    private string[] fileNames;
    string selectedFileContent, previousSelectedFileContent, selectedButton;
    private int selectedIndex = -1;
    private UIConstructor uiConstructor;
    private Vector2 scrollPositionJSONView;
    private UIElement selectedUIElement;
    private bool validJson;
    public List<int> address = new List<int>();

    UIElement insertUIElement;

    private SerializedObject serializedObject;
    private SerializedProperty textAreaContent;

    private TextData textData;

    [MenuItem("Window/Template Editor")]
    public static void ShowWindow()
    {
        GetWindow(typeof(TemplateEditorWindow));
    }

    #region Sections
    private void DrawLeftSection()
    {
        GUILayout.BeginVertical("box", GUILayout.Width(position.width * 0.2f));

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button) { fixedWidth = 275 };
        Color vibrantGreen = new Color(0, 1, 0, 0.7f); // Vibrant green color
        GUIStyle selectedButtonStyle = new GUIStyle(buttonStyle);
        selectedButtonStyle.normal.background = MakeTex(2, 2, vibrantGreen); // Setting the background color
        selectedButtonStyle.normal.textColor = Color.green; // Setting the text color to green for selected button

        for (int i = 0; i < fileNames.Length; i++)
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(fileNames[i], (i == selectedIndex) ? selectedButtonStyle : buttonStyle))
            {
                selectedButton = "Edit Template";
                if (i == selectedIndex) DeselectFile();
                else SelectFile(i);
            }
            if (GUILayout.Button(EditorGUIUtility.IconContent("d_editicon.sml")))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Rename"), false, RenameFile, i);
                menu.AddItem(new GUIContent("Delete"), false, DeleteFile, i);
                menu.ShowAsContext();
            }

            EditorGUILayout.EndHorizontal();
        }

        if (GUILayout.Button("Create New Template", buttonStyle)) CreateNewTemplate();

        GUILayout.EndVertical();
    }
    private void DrawRightSection()
    {

        if (selectedIndex != -1)
        {
            EditorGUILayout.BeginHorizontal();

            if (!validJson)
            {
                GUI.color = Color.red;
                GUILayout.Label("Invaild JSON (See console for more details)");
                GUI.color = Color.white;
            }
            else
            {
                GUI.color = Color.green;
                GUILayout.Label("UI Template is being rendered in Scene View");
                GUI.color = Color.white;
            }

            EditorGUILayout.EndHorizontal();

            serializedObject.Update();

            scrollPositionJSONView = EditorGUILayout.BeginScrollView(scrollPositionJSONView, GUILayout.Height(position.height * 0.55f));
            string newContent = EditorGUILayout.TextArea(textAreaContent.stringValue, GUILayout.ExpandHeight(true));
            if (newContent != textAreaContent.stringValue)
                textAreaContent.stringValue = newContent;
            bool jsonUpdated = !textData.text.Equals(previousSelectedFileContent);
            EditorGUILayout.EndScrollView();
            previousSelectedFileContent = textData.text;

            if (jsonUpdated)
            {
                HandleVisualization();
                selectedUIElement = JsonConvert.DeserializeObject<UIElement>(textData.text);
            }

            if (serializedObject.ApplyModifiedProperties())
            {
                Undo.RecordObject(textData, "Edit Text Area");
            }

            if (GUILayout.Button("Save")) SaveFile();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("InsertUIElement"))
            {
                insertUIElement = new UIElement();
                insertUIElement.color = new SerializableColor(Color.white);
                selectedButton = "InsertUIElement";
            }

            if (GUILayout.Button("InsertTemplate"))
                selectedButton = "InsertTemplate";

            if (GUILayout.Button("RemoveUIElement"))
                selectedButton = "RemoveUIElement";

            EditorGUILayout.EndHorizontal();

            if (selectedButton == "InsertTemplate")
                ShowAvailableTemplates();

            if (selectedButton == "InsertUIElement" || selectedButton == "Template")
                DrawUIElementEditor(insertUIElement);

            if (selectedButton == "RemoveUIElement")
                DrawRemovalAddress();
        }
        else
        {
            EditorGUILayout.LabelField("Click on any template to start editing");
        }

    }
    #endregion

    #region Handling Visuals
    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++) pix[i] = col;
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }

    private void ShowAvailableTemplates()
    {
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button) { fixedWidth = 275 };
        Color vibrantGreen = new Color(0, 1, 0, 0.7f);
        GUIStyle selectedButtonStyle = new GUIStyle(buttonStyle);
        selectedButtonStyle.normal.background = MakeTex(2, 2, vibrantGreen);
        selectedButtonStyle.normal.textColor = Color.green;

        // Display available templates
        for (int i = 0; i < fileNames.Length; i++)
        {
            if (GUILayout.Button(fileNames[i], (i == selectedIndex) ? selectedButtonStyle : buttonStyle))
            {
                string fileContent = File.ReadAllText(Path.Combine(PathToTemplates, fileNames[i] + ".json"));
                insertUIElement = JsonConvert.DeserializeObject<UIElement>(fileContent);
                selectedButton = "Template";
            }
        }
    }

    private void HandleVisualization()
    {
        DeleteCurrentUI();

        if (uiConstructor == null)
            uiConstructor = new GameObject("UIConstructor").AddComponent<UIConstructor>();

        if (!string.IsNullOrEmpty(textData.text))
            validJson = uiConstructor.ConstructUI(textData.text);
    }

    private void CreateNewTemplate()
    {
        string blueprintJson = File.ReadAllText(BlueprintJsonPath);
        string newFileName = "NewTemplate" + (fileNames.Length + 1) + ".json";
        File.WriteAllText(Path.Combine(PathToTemplates, newFileName), blueprintJson);
        LoadFileNames();
        int newFileIndex = fileNames.ToList().FindIndex(o => o.Equals(newFileName.Split(".")[0]));
        SelectFile(newFileIndex);
    }

    private void DrawUIElementEditor(UIElement insertUIElement)
    {
        if (insertUIElement == null) return;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Insert a UIElement/Template", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Name:", GUILayout.Width(80));
        insertUIElement.name = EditorGUILayout.TextField(insertUIElement.name);
        EditorGUILayout.LabelField("UIType:", GUILayout.Width(80));
        insertUIElement.UIType = (UIType)EditorGUILayout.EnumPopup(insertUIElement.UIType);
        EditorGUILayout.LabelField("Image Type:", GUILayout.Width(80));
        insertUIElement.imageType = (Image.Type)EditorGUILayout.EnumPopup(insertUIElement.imageType);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Position:", GUILayout.Width(80));
        insertUIElement.position = EditorGUILayout.Vector2Field("", insertUIElement.position);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Size:", GUILayout.Width(80));
        insertUIElement.size = EditorGUILayout.Vector2Field("", insertUIElement.size);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Color:", GUILayout.Width(80));
        insertUIElement.color = SerializableColorField("", insertUIElement.color);
        EditorGUILayout.LabelField("Text:", GUILayout.Width(80));
        insertUIElement.text = EditorGUILayout.TextField(insertUIElement.text);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Image Path:", GUILayout.Width(80));
        insertUIElement.imagePath = EditorGUILayout.TextField(insertUIElement.imagePath);
        EditorGUILayout.EndHorizontal();


        EditorGUILayout.LabelField("Address:", EditorStyles.boldLabel);

        insertUIElement.address ??= new List<int>();

        int addressCount = insertUIElement.address.Count;
        addressCount = EditorGUILayout.IntField("Address Count:", addressCount);

        // Adjust the size of the address list
        while (insertUIElement.address.Count < addressCount)
            insertUIElement.address.Add(0);

        while (insertUIElement.address.Count > addressCount)
            insertUIElement.address.RemoveAt(insertUIElement.address.Count - 1);

        for (int i = 0; i < insertUIElement.address.Count; i++)
            insertUIElement.address[i] = EditorGUILayout.IntField("Address Element " + (i + 1), insertUIElement.address[i]);

        if (GUILayout.Button("Insert")) InsertEditedUIElement(insertUIElement);
    }

    private void DrawRemovalAddress()
    {
        int addressCount = address.Count;
        addressCount = EditorGUILayout.IntField("Address Count:", addressCount);

        // Adjust the size of the address list
        while (address.Count < addressCount)
            address.Add(0);

        while (address.Count > addressCount)
            address.RemoveAt(address.Count - 1);

        for (int i = 0; i < address.Count; i++)
            address[i] = EditorGUILayout.IntField("Address Element " + (i + 1), address[i]);

        if (GUILayout.Button("Remove"))
        {
            selectedUIElement.RemoveUIElement(address);
            UpdateJSONTextArea();
        }
    }

    private SerializableColor SerializableColorField(string label, SerializableColor value)
    {
        Color color = new Color(value.r, value.g, value.b, value.a);

        color = EditorGUILayout.ColorField(label, color);

        return new SerializableColor(color);
    }

    private void InsertEditedUIElement(UIElement uiElement)
    {
        if (selectedUIElement == null) selectedUIElement = uiElement;
        else selectedUIElement.InsertUIElement(uiElement);

        UpdateJSONTextArea();
    }

    private void UpdateJSONTextArea()
    {

        var settings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        textData.text = JsonConvert.SerializeObject(selectedUIElement, Formatting.Indented, settings);
        textAreaContent.stringValue = textData.text;

        serializedObject.Update();
        if (serializedObject.ApplyModifiedProperties())
        {
            Undo.RecordObject(textData, "Edit Text Area");
        }
    }

    private void DeleteCurrentUI()
    {
        if (uiConstructor != null && uiConstructor.CanvasObject != null)
        {
            DestroyImmediate(uiConstructor.CanvasObject);
            uiConstructor.CanvasObject = null;
        }
    }
    #endregion

    #region FileHandling
    private void SaveFile()
    {
        File.WriteAllText(Path.Combine(PathToTemplates, fileNames[selectedIndex] + ".json"), textData.text);
        AssetDatabase.Refresh();
    }

    private void SelectFile(int index)
    {
        selectedIndex = index;
        textData.text = File.ReadAllText(Path.Combine(PathToTemplates, fileNames[index] + ".json"));
        textAreaContent.stringValue = textData.text;
        serializedObject.Update();
        serializedObject.ApplyModifiedProperties();
        GUIUtility.keyboardControl = 0; GUIUtility.hotControl = 0;
        selectedUIElement = JsonConvert.DeserializeObject<UIElement>(textData.text);
        insertUIElement = JsonConvert.DeserializeObject<UIElement>(textData.text);
    }

    private void LoadFileNames()
    {
        fileNames = Directory.GetFiles(PathToTemplates, "*.json");

        for (int i = 0; i < fileNames.Length; i++)
        {
            fileNames[i] = Path.GetFileNameWithoutExtension(fileNames[i]);
        }
    }

    private void DeselectFile()
    {
        selectedIndex = -1;
        textData.text = null;
    }

    private void RenameFile(object index)
    {
        int fileIndex = (int)index;
        string oldName = fileNames[fileIndex];
        RenameDialog.ShowDialog(oldName, newName =>
        {
            string oldPath = Path.Combine(PathToTemplates, oldName + ".json");
            string newPath = Path.Combine(PathToTemplates, newName + ".json");

            if (File.Exists(newPath))
            {
                EditorUtility.DisplayDialog("Error", "A file with that name already exists!", "OK");
                return;
            }

            File.Move(oldPath, newPath);
            LoadFileNames();

            if (selectedIndex == fileIndex) DeselectFile();
        });

        AssetDatabase.Refresh();
    }

    private void DeleteFile(object index)
    {
        int fileIndex = (int)index;

        if (EditorUtility.DisplayDialog("Delete File", "Are you sure you want to delete " + fileNames[fileIndex] + "?", "Delete", "Cancel"))
        {
            string filePath = Path.Combine(PathToTemplates, fileNames[fileIndex] + ".json");
            File.Delete(filePath);

            if (selectedIndex == fileIndex) DeselectFile();

            LoadFileNames();
        }

        AssetDatabase.Refresh();
    }
    #endregion

    #region UnityEvents
    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();

        // Left section
        EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.2f));
        DrawLeftSection();
        EditorGUILayout.EndVertical();

        // Right section
        EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.8f));
        DrawRightSection();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    private void OnEnable()
    {
        LoadFileNames();
        uiConstructor = FindObjectOfType<UIConstructor>();

        textData = ScriptableObject.CreateInstance<TextData>();
        serializedObject = new SerializedObject(textData);
        textAreaContent = serializedObject.FindProperty("text");
    }

    private void OnDestroy()
    {
        DeleteCurrentUI();

        if (uiConstructor != null) DestroyImmediate(uiConstructor.gameObject);
    }
    #endregion
}