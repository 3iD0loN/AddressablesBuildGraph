using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEngine;
using UnityEngine.AddressableAssets.Initialization;
using UnityEngine.AddressableAssets.ResourceLocators;

using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.BuildPipelineTasks;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Tasks;
using UnityEditor.Build.Content;
using UnityEngine.Serialization;
using UnityEngine.U2D;
using UnityEngine.AddressableAssets;
using UnityEditor.VersionControl;
using UnityEditor.Build.Utilities;
using System.Runtime.Remoting.Contexts;
using NUnit.Framework.Internal.Commands;
using NUnit.Framework;
using static UnityEditor.AddressableAssets.Build.Layout.BuildLayout;
using UnityEngine.Assertions;

namespace USP.AddressablesBuildGraph
{
    class AssetBundleInfo : IEqualityComparer<AssetBundleInfo>
    {
        #region Static Methods
        public static bool operator ==(AssetBundleInfo leftHand, AssetBundleInfo rightHand)
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

            return string.Compare(leftHand.assetBundleName, rightHand.assetBundleName, StringComparison.Ordinal) == 0;
        }

        public static bool operator !=(AssetBundleInfo lhs, AssetBundleInfo rhs)
        {
            return !(lhs == rhs);
        }
        #endregion

        #region Properties
        public string assetBundleName { get; }

        //public string assetBundleVariant { get; }

        /// <summary>
        /// Gets a unique set of all assets that are packed into a bundle.
        /// </summary>
        public HashSet<AssetInfo> Assets { get; }

        /// <summary>
        /// Gets the Addressbles group used to generate the asset bundle.
        /// </summary>
        public GroupInfo Group { get; set; }
        #endregion

        #region Methods
        public AssetBundleInfo(string assetBundleName)
        {
            this.assetBundleName = assetBundleName;
            this.Assets = new HashSet<AssetInfo>();
        }

        public override int GetHashCode()
        {
            return assetBundleName.GetHashCode();
        }

        public override bool Equals(object other)
        {
            if (other is not AssetBundleInfo assetBundle)
            {
                return false;
            }

            return this == assetBundle;
        }

        public int GetHashCode(AssetBundleInfo obj)
        {
            return obj.GetHashCode();
        }

        public bool Equals(AssetBundleInfo lhs, AssetBundleInfo rhs)
        {
            return lhs == rhs;
        }

        public override string ToString()
        {
            return assetBundleName;
        }
        #endregion
    }
}