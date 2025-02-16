using System;
using System.IO;
using System.Collections.Generic;

using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Utilities;

namespace USP.AddressablesBuildGraph
{
    public class AssetInfo : IEqualityComparer<AssetInfo>
    {
        #region Constants
        private const string SceneExtension = ".unity";

        private static readonly string ExteriorEditorFolder = $"{Path.DirectorySeparatorChar}Editor";

        private static readonly string InteriorEditorFolder = $"{ExteriorEditorFolder}{Path.DirectorySeparatorChar}";

        private static readonly string ExteriorResourcesFolder = $"{Path.DirectorySeparatorChar}resources";

        private static readonly string InteriorResourcesFolder = $"{ExteriorEditorFolder}{Path.DirectorySeparatorChar}";

        private static readonly HashSet<string> ExcludedExtensions = new HashSet<string>
        {
            ".cs",
            ".js",
            ".boo",
            ".exe",
            ".dll",
            ".meta",
            ".preset",
            ".asmdef"
        };
        #endregion

        #region Static Methods
        public static AssetInfo Create(GUID assetGuid, IReadOnlyDictionary<GUID, List<string>> assetGuidsToArchiveFile, AddressableAssetSettings settings)
        {
            // Attempt to find a list of archive files that are associated with the asset guid.
            // If there is a asset guid that is associated with a list of archive files, then the asset was explicitly pulled into this asset bundle build.
            bool explicitAsset = assetGuidsToArchiveFile.ContainsKey(assetGuid);

            // If the asset was implicitly pulled into this build, then:
            if (!explicitAsset)
            {
                // Create a new instance of the info that defines a non-Addressable asset.
                return new AssetInfo(assetGuid);
            }

            // Otherwise, the asset was explicitly pulled into this build.

            // Find the Addressable asset entry associated with the asset guid.
            AddressableAssetEntry assetEntry = settings.FindAssetEntry(assetGuid.ToString());

            // If no entry was found in the Addressables system, then:
            if (assetEntry == null)
            {
                // Throw an error.
                // NOTE: Unlikely, but you never know
                throw new NullReferenceException("The build output says that this was explicitly defined in the build, but it is not Addressable. Perhaps this was added outside of Addressables?");
            }

            // Otherwise, the entry was found in the Addressable system.

            // Create a new instance of the info that defines a Addressables asset.
            return new AssetInfo(assetEntry);
        }

        public static void PopulateAssetDependencyGraph(IEnumerable<AssetInfo> assets)
        {
            var assetSet = new HashSet<AssetInfo>(assets);

            PopulateAssetDependencyGraph(assetSet);
        }

        public static void PopulateAssetDependencyGraph(HashSet<AssetInfo> assetSet)
        {
            foreach (AssetInfo asset in assetSet)
            {
                string[] dependencyAssetPaths = AssetDatabase.GetDependencies(asset.FilePath);

                foreach (string dependencyAssetPath in dependencyAssetPaths)
                {
                    if (!assetSet.TryGetValue(new AssetInfo(dependencyAssetPath), out AssetInfo dependencyAsset))
                    {
                        // TODO: Shouldn't happen. throw exception instead?
                        dependencyAsset = default;
                    }

                    asset.DependsOn(dependencyAsset);
                }
            }
        }

        public static bool IsScene(string assetFilePath)
        {
            return assetFilePath.EndsWith(SceneExtension, StringComparison.OrdinalIgnoreCase);
        }

        public bool IsValidPath(string filePath)
        {
            return IsPathValidForEntry(filePath) &&
                !filePath.Contains(InteriorResourcesFolder, StringComparison.OrdinalIgnoreCase) &&
                !filePath.StartsWith(ExteriorResourcesFolder, StringComparison.OrdinalIgnoreCase);
        }

        internal static bool StringContains(string input, string value, StringComparison comp)
        {
#if NET_UNITY_4_8
            return input.Contains(value, comp);
#else
            return input.Contains(value);
#endif
        }

        public static bool IsPathValidForEntry(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return false;
            }

            if (Path.DirectorySeparatorChar != '\\' && filePath.Contains('\\'))
            {
                filePath = filePath.Replace('\\', Path.DirectorySeparatorChar);
            }

            if (Path.DirectorySeparatorChar != '/' && filePath.Contains('/'))
            {
                filePath = filePath.Replace('/', Path.DirectorySeparatorChar);
            }

            if (!filePath.StartsWith("Assets", StringComparison.OrdinalIgnoreCase) &&
                !IsPathValidPackageAsset(filePath))
            {
                return false;
            }

            string fileExtension = Path.GetExtension(filePath);
            if (string.IsNullOrEmpty(fileExtension))
            {
                if (filePath == "Assets")
                {
                    return false;
                }

                int editorIndex = filePath.IndexOf(ExteriorEditorFolder, StringComparison.OrdinalIgnoreCase);
                if (editorIndex != -1)
                {
                    int length = filePath.Length;
                    int folderLength = ExteriorEditorFolder.Length;

                    if (editorIndex == length - folderLength)
                    {
                        return false;
                    }

                    if (filePath[editorIndex + folderLength] == Path.DirectorySeparatorChar)
                    {
                        return false;
                    }

                    // Could still have something like Assets/editorthings/Editor/things, but less likely
                    if (StringContains(filePath, InteriorEditorFolder, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }

                if (String.Equals(filePath, CommonStrings.UnityEditorResourcePath, StringComparison.Ordinal) ||
                    String.Equals(filePath, CommonStrings.UnityDefaultResourcePath, StringComparison.Ordinal) ||
                    String.Equals(filePath, CommonStrings.UnityBuiltInExtraPath, StringComparison.Ordinal))
                {
                    return false;
                }
            }
            else
            {
                // asset type
                if (StringContains(filePath, InteriorEditorFolder, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                if (ExcludedExtensions.Contains(fileExtension))
                {
                    return false;
                }
            }

            var settings = AddressableAssetSettingsDefaultObject.SettingsExists ?
                AddressableAssetSettingsDefaultObject.Settings : null;

            if (settings != null && filePath.StartsWith(settings.ConfigFolder, StringComparison.Ordinal))
            {
                return false;
            }

            return true;
        }

        internal static bool IsPathValidPackageAsset(string pathLowerCase)
        {
            string[] splitPath = pathLowerCase.Split(Path.DirectorySeparatorChar);

            if (splitPath.Length < 3)
            {
                return false;
            }

            if (!String.Equals(splitPath[0], "packages", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (String.Equals(splitPath[2], "package.json", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }

        #region Operators
        public static bool operator ==(AssetInfo leftHand, AssetInfo rightHand)
        {
            var lhs = (object)leftHand;
            var rhs = (object)rightHand;

            if (lhs == rhs)
            {
                return true;
            }

            if (rhs == null || lhs == null)
            {
                return false;
            }

            return string.Compare(leftHand.Guid, rightHand.Guid, StringComparison.Ordinal) == 0;
        }

        public static bool operator !=(AssetInfo lhs, AssetInfo rhs)
        {
            return !(lhs == rhs);
        }
        #endregion
        #endregion

        #region Properties
        public string Guid { get; }

        public string FilePath { get; }

        public string Address { get; }

        public bool IsAddressable => !string.IsNullOrEmpty(Address);

        public bool IsReadOnly { get; }

        public bool IsSubAsset { get; }

        public bool IsSceneAsset { get; }

        public HashSet<string> Labels { get; }

        public HashSet<AssetInfo> DependentAssets { get; }

        /// <summary>
        /// Gets the assets that that this asset references; the assets that this asset is dependent on.
        /// </summary>
        public HashSet<AssetInfo> AssetDependencies { get; }

        /// <summary>
        /// Gets a unique set of all asset bundles that this asset is packed into.
        /// </summary>
        public HashSet<AssetBundleInfo> Bundles { get; }

        /// <summary>
        /// Gets a value indicating whether the asset is being duplicated in multiple asset bundles.
        /// </summary>
        public bool IsDuplicate => Bundles.Count > 1;
        
        /// <summary>
        /// A value indicating whether or not the asset is a root for a subtree of assets implicitly pulled into the build.
        /// </summary>
        public bool IsImplicitRoot
        {
            get
            {
                // If this asset is addressable, then it is an asset that is explicitly pulled into the build.
                // If this asset is explicitly defined in the build, then:
                if (IsAddressable)
                {
                    // It is not an the root of an implicit graph.
                    return false;
                }

                // Otherwise, the asset is implicitly defined in the build.

                // For each of the assets dependents, perform the following:
                foreach (var assetDependent in DependentAssets)
                {
                    // If the asset dependent on this one is explicitly pulled into this build, then:
                    if (assetDependent.IsAddressable)
                    {
                        // At least one of the dependent assets is explicit, so this asset is an root of implicit assets
                        return true;
                    }

                    // Otherwise, the dependent asset is also implicit, and it is unclear whether this is an implicit root.
                    // Move onto the next item until we find a match or exhaust all items.
                }
                
                // All items have been exhausted, and there was no explicit asset that was dependent on this asset.

                // This asset is not a root of implicit assets.
                return false;
            }
        }

        public bool IsValid
        {
            get
            {
                return IsValidPath(FilePath);
            }
        }
        #endregion

        #region Methods
        #region Constructors
        public AssetInfo(AddressableAssetEntry assetEntry) :
            this(assetEntry.guid, assetEntry.AssetPath, assetEntry.address, assetEntry.labels, assetEntry.ReadOnly, assetEntry.IsSubAsset, assetEntry.IsScene)
        {
        }

        public AssetInfo(GUID guid) :
            this(guid.ToString(), null)
        {
        }

        public AssetInfo(string assetPath) :
            this(null, assetPath)
        {
        }

        public AssetInfo(string guid, string filePath, string address = null, HashSet<string> labels = null, bool readOnly = false, bool isSubAsset = false, bool? isScene = null)
        {
            this.Guid = guid ?? AssetDatabase.AssetPathToGUID(filePath);
            this.FilePath = filePath ?? AssetDatabase.GUIDToAssetPath(guid);
            this.Address = address;
            this.Labels = labels != null ? new HashSet<string>(labels) : new HashSet<string>();
            this.IsReadOnly = readOnly;
            this.IsSubAsset = isSubAsset;
            this.IsSceneAsset = isScene ?? IsScene(this.FilePath);

            this.DependentAssets = new HashSet<AssetInfo>();
            this.AssetDependencies = new HashSet<AssetInfo>();
            this.Bundles = new HashSet<AssetBundleInfo>();
        }
        #endregion

        public override int GetHashCode()
        {
            return Guid.GetHashCode();
        }

        public override bool Equals(object other)
        {
            if (other is not AssetInfo asset)
            {
                return false;
            }

            return this == asset;
        }

        public int GetHashCode(AssetInfo obj)
        {
            return obj.GetHashCode();
        }

        public bool Equals(AssetInfo lhs, AssetInfo rhs)
        {
            return lhs == rhs;
        }

        public override string ToString()
        {
            return FilePath;
        }

        public void DependsOn(AssetInfo dependency)
        {
            if (dependency == null || this == dependency)
            {
                return;
            }

            this.AssetDependencies.Add(dependency);

            dependency.DependentAssets.Add(this);
        }
        #endregion
    }
}