using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System.Linq;
using System.Reflection;
using System.ComponentModel.Design;

[Serializable]
public class ItemEntry : DatabaseItem, IDatabaseItem
{

    [DbColumn("NVARCHAR(200)", "")]
    [OnValueChanged(nameof(UnityEngine.PlayerLoop.Update))]
    public string Name;

    [DbColumn("INTEGER", "0")]
    [ValueDropdown(nameof(AllItemTypes))]
    [OnValueChanged(nameof(UnityEngine.PlayerLoop.Update))]
    public ItemType ItemType;

    [DbColumn("INTEGER", "0")]
    [OnValueChanged(nameof(UnityEngine.PlayerLoop.Update))]
    public int Value;

    public IEnumerable<ItemType> AllItemTypes => Utils.GetValues<ItemType>();
}

[ExecuteInEditMode]
public class Items : Table<ItemEntry>
{
    public override string Name => "Item";

    public override ItemEntry Create(System.Data.IDataReader reader)
    {
        return new ItemEntry
        {
            Id = reader.GetInt32(0),
            Name = reader.GetString(1),
            ItemType = (ItemType)reader.GetInt32(2)
        };
    }
}
