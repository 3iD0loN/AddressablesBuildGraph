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
    class GroupInfo : IEqualityComparer<GroupInfo>
    {
        #region Static Methods
        public static bool operator ==(GroupInfo leftHand, GroupInfo rightHand)
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

            return string.Compare(leftHand.guid, rightHand.guid, StringComparison.Ordinal) == 0;
        }

        public static bool operator !=(GroupInfo lhs, GroupInfo rhs)
        {
            return !(lhs == rhs);
        }
        #endregion

        #region Properties
        public string name { get; }

        public string guid { get; }

        public bool isDefault { get; }

        public bool readOnly { get; }

        /// <summary>
        /// Gets a unique set of asset bundles that are generated from the group by the build.
        /// </summary>
        public HashSet<AssetBundleInfo> AssetBundles { get; }
        #endregion

        #region Methods
        #region Constructors
        public GroupInfo(AddressableAssetGroup group) :
            this(group.Name, group.Guid, group.Default, group.ReadOnly)
        {
        }

        public GroupInfo(string name, string guid, bool isDefault = false, bool readOnly = false)
        {
            this.name = name;
            this.guid = guid;
            this.isDefault = isDefault;
            this.AssetBundles = new HashSet<AssetBundleInfo>();
        }
        #endregion

        public override int GetHashCode()
        {
            return guid.GetHashCode();
        }

        public override bool Equals(object other)
        {
            if (other is not GroupInfo asset)
            {
                return false;
            }

            return this == asset;
        }

        public int GetHashCode(GroupInfo obj)
        {
            return obj.GetHashCode();
        }

        public bool Equals(GroupInfo lhs, GroupInfo rhs)
        {
            return lhs == rhs;
        }

        public override string ToString()
        {
            return name;
        }
        #endregion
    }
}