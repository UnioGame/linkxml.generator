using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Linq;

namespace UniGame.Editor
{
    [CustomEditor(typeof(LinkXmlGeneratorAsset))]
    public class LinkXmlGeneratorAssetEditor : UnityEditor.Editor
    {
        private VisualElement root;
        private ListView resourcesListView;
        private LinkXmlGeneratorAsset targetAsset;

        public override VisualElement CreateInspectorGUI()
        {
            targetAsset = target as LinkXmlGeneratorAsset;
            
            // Create root container
            root = new VisualElement();
            root.style.paddingTop = 10;
            root.style.paddingBottom = 10;
            root.style.paddingLeft = 10;
            root.style.paddingRight = 10;

            // Load USS stylesheet
            var guids = AssetDatabase.FindAssets("LinkXmlGeneratorAssetEditor t:StyleSheet");
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
                if (styleSheet != null)
                    root.styleSheets.Add(styleSheet);
            }

            // Header
            var headerLabel = new Label("Link XML Generator");
            headerLabel.AddToClassList("linkxml-generator-header");
            root.Add(headerLabel);

            // Generate button
            var generateButton = new Button(() => targetAsset.Generate())
            {
                text = "Generate Link XML"
            };
            generateButton.AddToClassList("linkxml-generator-button");
            root.Add(generateButton);

            // Resources section
            var resourcesHeader = new Label("Resources");
            resourcesHeader.AddToClassList("linkxml-section-header");
            root.Add(resourcesHeader);

            // Add resource buttons container
            var buttonsContainer = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    marginBottom = 10
                }
            };

            var addNamespaceBtn = new Button(() => AddResource(LinkXmlResourceType.Namespace))
            {
                text = "Add Namespace"
            };
            addNamespaceBtn.AddToClassList("linkxml-generator-add-button");

            var addBaseTypeBtn = new Button(() => AddResource(LinkXmlResourceType.BaseType))
            {
                text = "Add Base Type"
            };
            addBaseTypeBtn.AddToClassList("linkxml-generator-add-button");

            var addRegexBtn = new Button(() => AddResource(LinkXmlResourceType.RegexPattern))
            {
                text = "Add Regex"
            };
            addRegexBtn.AddToClassList("linkxml-generator-add-button");

            buttonsContainer.Add(addNamespaceBtn);
            buttonsContainer.Add(addBaseTypeBtn);
            buttonsContainer.Add(addRegexBtn);
            root.Add(buttonsContainer);

            // Assembly and ConcreteType buttons
            var buttonsContainer2 = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    marginBottom = 10
                }
            };

            var addAssemblyBtn = new Button(() => AddResource(LinkXmlResourceType.Assembly))
            {
                text = "Add Assembly"
            };
            addAssemblyBtn.AddToClassList("linkxml-generator-add-button");

            var addConcreteTypeBtn = new Button(() => AddResource(LinkXmlResourceType.ConcreteType))
            {
                text = "Add Concrete Type"
            };
            addConcreteTypeBtn.AddToClassList("linkxml-generator-add-button");

            buttonsContainer2.Add(addAssemblyBtn);
            buttonsContainer2.Add(addConcreteTypeBtn);
            root.Add(buttonsContainer2);

            // Resources list
            CreateResourcesList();

            return root;
        }

        private void CreateResourcesList()
        {
            var listContainer = new VisualElement();
            listContainer.AddToClassList("linkxml-resources-container");

            resourcesListView = new ListView
            {
                itemsSource = targetAsset.Resources,
                fixedItemHeight = 80,
                reorderable = true,
                showBorder = false,
                style = { flexGrow = 1 }
            };

            resourcesListView.makeItem = () =>
            {
                var container = new VisualElement();
                container.AddToClassList("linkxml-resource-item");

                var enabledToggle = new Toggle
                {
                    style = { marginRight = 10, alignSelf = Align.Center }
                };

                var contentContainer = new VisualElement();
                contentContainer.AddToClassList("linkxml-resource-content");

                var typeField = new EnumField();
                typeField.AddToClassList("linkxml-resource-type");

                var valueField = new TextField
                {
                    style = { flexGrow = 1 }
                };

                var deleteButton = new Button
                {
                    text = "×"
                };
                deleteButton.AddToClassList("linkxml-delete-button");

                contentContainer.Add(typeField);
                contentContainer.Add(valueField);
                container.Add(enabledToggle);
                container.Add(contentContainer);
                container.Add(deleteButton);

                return container;
            };

            resourcesListView.bindItem = (element, index) =>
            {
                if (index >= targetAsset.Resources.Count) return;

                var resource = targetAsset.Resources[index];
                var container = element;
                var enabledToggle = container.Q<Toggle>();
                var contentContainer = container.Q<VisualElement>();
                var typeField = contentContainer.Q<EnumField>();
                var valueField = contentContainer.Q<TextField>();
                var deleteButton = container.Q<Button>();

                enabledToggle.value = resource.Enabled;
                enabledToggle.RegisterValueChangedCallback(evt =>
                {
                    resource.Enabled = evt.newValue;
                    EditorUtility.SetDirty(targetAsset);
                });

                typeField.Init(resource.Type);
                typeField.value = resource.Type;
                typeField.RegisterValueChangedCallback(evt =>
                {
                    resource.Type = (LinkXmlResourceType)evt.newValue;
                    UpdatePlaceholder(valueField, resource.Type);
                    EditorUtility.SetDirty(targetAsset);
                });

                valueField.value = resource.StringValue ?? "";
                UpdatePlaceholder(valueField, resource.Type);
                valueField.RegisterValueChangedCallback(evt =>
                {
                    resource.StringValue = evt.newValue;
                    EditorUtility.SetDirty(targetAsset);
                });

                deleteButton.clicked += () =>
                {
                    if (index >= 0 && index < targetAsset.Resources.Count)
                    {
                        targetAsset.Resources.RemoveAt(index);
                        EditorUtility.SetDirty(targetAsset);
                        resourcesListView.RefreshItems();
                    }
                };
            };

            listContainer.Add(resourcesListView);
            root.Add(listContainer);
        }

        private void UpdatePlaceholder(TextField field, LinkXmlResourceType type)
        {
            // Set tooltip instead of placeholder text for better compatibility
            switch (type)
            {
                case LinkXmlResourceType.Namespace:
                    field.tooltip = "Example: MyProject.Core";
                    break;
                case LinkXmlResourceType.Assembly:
                    field.tooltip = "Example: MyAssembly";
                    break;
                case LinkXmlResourceType.ConcreteType:
                    field.tooltip = "Example: MyProject.MyClass";
                    break;
                case LinkXmlResourceType.RegexPattern:
                    field.tooltip = "Example: .*Controller$";
                    break;
                case LinkXmlResourceType.BaseType:
                    field.tooltip = "Example: UnityEngine.MonoBehaviour";
                    break;
            }
        }

        private void AddResource(LinkXmlResourceType type)
        {
            var newResource = new LinkXmlResourceSettings
            {
                Type = type,
                Enabled = true,
                StringValue = GetDefaultValue(type)
            };

            targetAsset.Resources.Add(newResource);
            EditorUtility.SetDirty(targetAsset);
            resourcesListView.RefreshItems();
            
            // Scroll to the newly added item
            resourcesListView.ScrollToItem(targetAsset.Resources.Count - 1);
        }

        private string GetDefaultValue(LinkXmlResourceType type)
        {
            switch (type)
            {
                case LinkXmlResourceType.Namespace:
                    return "MyProject.Core";
                case LinkXmlResourceType.Assembly:
                    return "MyAssembly";
                case LinkXmlResourceType.ConcreteType:
                    return "MyProject.MyClass";
                case LinkXmlResourceType.RegexPattern:
                    return @".*Controller$";
                case LinkXmlResourceType.BaseType:
                    return "UnityEngine.MonoBehaviour";
                default:
                    return "";
            }
        }
    }
}