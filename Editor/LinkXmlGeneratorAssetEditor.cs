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
        private TextField searchField;
        private System.Collections.Generic.List<LinkXmlResourceSettings> filteredResources;

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

            // Search container
            var searchContainer = new VisualElement
            {
                style = 
                {
                    flexDirection = FlexDirection.Row,
                    marginBottom = 10
                }
            };

            searchField = new TextField("Search by Value:")
            {
                style = { flexGrow = 1, marginRight = 5 }
            };
            searchField.AddToClassList("linkxml-search-field");
            searchField.tooltip = "Filter resources by their string value";
            searchField.RegisterValueChangedCallback(OnSearchChanged);

            var clearButton = new Button(() => 
            {
                searchField.value = "";
                RefreshFilteredList();
            })
            {
                text = "Clear",
                style = { width = 50 }
            };
            clearButton.AddToClassList("linkxml-generator-add-button");

            searchContainer.Add(searchField);
            searchContainer.Add(clearButton);
            root.Add(searchContainer);

            // Search results counter
            var counterLabel = new Label()
            {
                style = 
                {
                    fontSize = 12,
                    color = new StyleColor(new Color(0.7f, 0.7f, 0.7f)),
                    marginBottom = 5
                }
            };
            root.Add(counterLabel);
            
            // Store reference for updates
            searchField.userData = counterLabel;

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
            InitializeFilteredResources();
            CreateResourcesList();

            return root;
        }

        private void CreateResourcesList()
        {
            var listContainer = new VisualElement();
            listContainer.AddToClassList("linkxml-resources-container");

            resourcesListView = new ListView
            {
                itemsSource = filteredResources,
                fixedItemHeight = 80,
                reorderable = true,
                showBorder = false,
                style = { flexGrow = 1 }
            };

            // Handle reordering - only when not filtering
            resourcesListView.itemsSourceChanged += OnItemsSourceChanged;
            resourcesListView.onItemsChosen += OnItemsChosen;

            resourcesListView.makeItem = () =>
            {
                var container = new VisualElement();
                // Style will be applied in bindItem for alternating rows

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
                if (index >= filteredResources.Count) return;

                var resource = filteredResources[index];
                var container = element;
                
                // Apply alternating row styles
                container.RemoveFromClassList("linkxml-resource-item");
                container.RemoveFromClassList("linkxml-resource-item-even");
                
                if (index % 2 == 0)
                {
                    container.AddToClassList("linkxml-resource-item");
                }
                else
                {
                    container.AddToClassList("linkxml-resource-item-even");
                }
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
                    if (index >= 0 && index < filteredResources.Count)
                    {
                        var resourceToRemove = filteredResources[index];
                        targetAsset.Resources.Remove(resourceToRemove);
                        EditorUtility.SetDirty(targetAsset);
                        RefreshFilteredList();
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
            RefreshFilteredList();
            
            // Scroll to the newly added item
            resourcesListView.ScrollToItem(filteredResources.Count - 1);
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

        private void InitializeFilteredResources()
        {
            filteredResources = new System.Collections.Generic.List<LinkXmlResourceSettings>(targetAsset.Resources);
        }

        private void RefreshFilteredList()
        {
            var searchText = searchField?.value?.ToLower() ?? "";
            
            if (string.IsNullOrEmpty(searchText))
            {
                filteredResources.Clear();
                filteredResources.AddRange(targetAsset.Resources);
                // Enable reordering when not filtering
                if (resourcesListView != null)
                    resourcesListView.reorderable = true;
            }
            else
            {
                filteredResources.Clear();
                filteredResources.AddRange(targetAsset.Resources.Where(r => 
                    r.StringValue != null && r.StringValue.ToLower().Contains(searchText)));
                // Disable reordering when filtering to avoid confusion
                if (resourcesListView != null)
                    resourcesListView.reorderable = false;
            }
            
            // Update counter
            var counterLabel = searchField?.userData as Label;
            if (counterLabel != null)
            {
                if (string.IsNullOrEmpty(searchText))
                {
                    counterLabel.text = $"Total: {filteredResources.Count} resources";
                }
                else
                {
                    counterLabel.text = $"Found: {filteredResources.Count} of {targetAsset.Resources.Count} resources";
                }
            }
            
            resourcesListView?.RefreshItems();
        }

        private void OnSearchChanged(ChangeEvent<string> evt)
        {
            RefreshFilteredList();
        }

        private void OnItemsSourceChanged()
        {
            // Update original list when items are reordered, but only if no search filter is active
            if (filteredResources != null && string.IsNullOrEmpty(searchField?.value))
            {
                targetAsset.Resources.Clear();
                targetAsset.Resources.AddRange(filteredResources);
                EditorUtility.SetDirty(targetAsset);
            }
        }

        private void OnItemsChosen(System.Collections.Generic.IEnumerable<object> items)
        {
            // Handle item selection if needed
        }
    }
}