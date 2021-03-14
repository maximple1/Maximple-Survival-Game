#if UNITY_2018_2_OR_NEWER
#define NEW_PACKMAN

using System;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.UI;
using UnityEngine;

#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#else
using UnityEngine.Experimental.UIElements;
#endif

[UnityEditor.InitializeOnLoad]
internal class SamplesLinkPackageManagerExtension : IPackageManagerExtension {
    VisualElement rootVisualElement;
    const string SAMPLEBUTTON_TEXT = "Download Asset Samples from Asset Store";
    const string ASSETSTORE_URL = "http://u3d.as/1wLg";
    const string TERRAIN_TOOLS_NAME = "com.unity.terrain-tools";

    private Button samplesButton;
    private VisualElement parent;

    public VisualElement CreateExtensionUI() {
        samplesButton = new Button();
        samplesButton.text = SAMPLEBUTTON_TEXT;
        samplesButton.clickable.clicked += () => Application.OpenURL(ASSETSTORE_URL);

        return samplesButton;
    }

	static SamplesLinkPackageManagerExtension() {
		PackageManagerExtensions.RegisterExtension(new SamplesLinkPackageManagerExtension());
	}
	
	void IPackageManagerExtension.OnPackageSelectionChange(PackageInfo packageInfo) {
        // Prevent the button from rendering on other packages
        if (samplesButton.parent != null)
            parent = samplesButton.parent;

        bool shouldRender = packageInfo?.name == TERRAIN_TOOLS_NAME;
        if (!shouldRender) {
            samplesButton.RemoveFromHierarchy();
        }
        else {
            parent.Add(samplesButton);
        }
    }

	void IPackageManagerExtension.OnPackageAddedOrUpdated(PackageInfo packageInfo) {}

	void IPackageManagerExtension.OnPackageRemoved(PackageInfo packageInfo) {}
}

#endif