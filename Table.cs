using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System.Linq;
using Mono.Data.Sqlite;
using System.Data;

public class DbColumnAttribute : Attribute
{
    public string SqlType;
    public string DefaultValue;

    public DbColumnAttribute(string sqlType, string defaultValue)
    {
        SqlType = sqlType;
        DefaultValue = defaultValue;
    }
}

public abstract class DatabaseItem
{
    [ReadOnly]
    [DbColumn("INTEGER", "0")]
    public int Id { get; set; }

    public List<(string name, object value)> GetColumns()
    {
        var type = GetType();
        var lst = new List<(string name, object value)>();

        foreach (var field in type.GetFields())
        {
            DbColumnAttribute attr = (DbColumnAttribute)Attribute.GetCustomAttribute(field, typeof(DbColumnAttribute));
            if (attr == null)
                continue;
            //if (!field.CustomAttributes.Any(x => x.AttributeType == typeof(ReadOnlyAttribute)))
            lst.Add((field.Name, field.GetValue(this)));
        }

        foreach (var prop in type.GetProperties())
        {
            DbColumnAttribute attr = (DbColumnAttribute)Attribute.GetCustomAttribute(prop, typeof(DbColumnAttribute));
            if (attr == null)
                continue;
            //if (!field.CustomAttributes.Any(x => x.AttributeType == typeof(ReadOnlyAttribute)))
            lst.Add((prop.Name, prop.GetValue(this)));
        }

        return lst;
    }

    public void Update()
    {
        var cols = GetColumns().Where(x => x.name != "Id");
        var colText = string.Join(", ", cols.Select(x => x.name));
        var setters = string.Join(", ", cols.Select(x => $"{x.name} = '{x.value}'"));
        Table.ExecuteCommand("");
    }
}

public interface IDatabaseItem
{
    int Id { get; }
}

[ExecuteInEditMode]
public abstract class Table : MonoBehaviour
{
    public static string Url => $"URI=file:GameDb.sqlite";

    public static void ExecuteCommand(string command, Action<IDataReader> action = null)
    {
        var dbConnection = new SqliteConnection(Url);
        dbConnection.Open();
        IDbCommand dbCommand = dbConnection.CreateCommand();
        dbCommand.CommandText = command;
        var reader = dbCommand.ExecuteReader();
        if (action != null)
            action(reader);

        dbConnection.Close();
    }

}
[ExecuteInEditMode]
public abstract class Table<T> : Table
    where T : IDatabaseItem, new()
{
    public abstract string Name { get; }

    [TableList]
    [OnCollectionChanged(after: nameof(PutAll2), before: nameof(PutAll))]
    public List<T> Rows;

    public void PutAll2(CollectionChangeInfo info)
    {

    }

    public void PutAll(CollectionChangeInfo info)
    {
        switch (info.ChangeType)
        {
            case CollectionChangeType.Add:
                var cols = GetColumns().Where(x => x.name != "Id");
                var names = string.Join(", ", cols.Select(x => x.name));
                var vals = string.Join(", ", cols.Select(x => $"'{x.defaultValue}'"));
                ExecuteCommand($"INSERT INTO {Name}({names}) VALUES ({vals})");
                break;
            case CollectionChangeType.RemoveIndex:
                ExecuteCommand($"DELETE FROM {Name} WHERE Id = {Rows[info.SelectionIndex].Id}");
                break;

        }
        Get();
    }

    [Button("Reset")]
    public void Create()
    {
        var cols = GetColumns().Where(x => x.name != "Id");
        var colText = string.Join(", ", cols.Select(x => $"{x.name} {x.sqlType}"));
        Debug.Log($"CREATE TABLE {Name} (Id INTEGER PRIMARY KEY, {colText})");
        ExecuteCommand($"DROP TABLE IF EXISTS {Name}");
        ExecuteCommand($"CREATE TABLE {Name} (Id INTEGER PRIMARY KEY, {colText})");
        Get();
    }

    public abstract T Create(IDataReader reader);

    [Button]
    public void Get()
    {
        Rows = new List<T>();
        ExecuteCommand($"SELECT * FROM {Name}", r =>
        {
            while (r.Read())
            {
                Rows.Add(Create(r));
            }

        });
    }

    public static List<(string name, string sqlType, string defaultValue)> GetColumns()
    {
        var type = typeof(T);
        var lst = new List<(string name, string sqlType, string defaultValue)>();

        foreach (var field in type.GetFields())
        {
            DbColumnAttribute attr = (DbColumnAttribute)Attribute.GetCustomAttribute(field, typeof(DbColumnAttribute));
            if (attr == null)
                continue;
            var sqlType = attr.SqlType;
            var defaultValue = attr.DefaultValue;
            //if (!field.CustomAttributes.Any(x => x.AttributeType == typeof(ReadOnlyAttribute)))
            lst.Add((field.Name, sqlType, defaultValue));
        }

        foreach (var prop in type.GetProperties())
        {
            DbColumnAttribute attr = (DbColumnAttribute)Attribute.GetCustomAttribute(prop, typeof(DbColumnAttribute));
            if (attr == null)
                continue;
            var sqlType = attr.SqlType;
            var defaultValue = attr.DefaultValue;
            //if (!field.CustomAttributes.Any(x => x.AttributeType == typeof(ReadOnlyAttribute)))
            lst.Add((prop.Name, sqlType, defaultValue));
        }

        return lst;
    }

    public static List<(string name, string sqlType, string defaultValue, object value)> GetColumns(T item)
    {
        var type = typeof(T);
        var lst = new List<(string name, string sqlType, string defaultValue, object value)>();

        foreach (var field in type.GetFields())
        {
            DbColumnAttribute attr = (DbColumnAttribute)Attribute.GetCustomAttribute(field, typeof(DbColumnAttribute));
            if (attr == null)
                continue;
            var sqlType = attr.SqlType;
            var defaultValue = attr.DefaultValue;
            //if (!field.CustomAttributes.Any(x => x.AttributeType == typeof(ReadOnlyAttribute)))
            lst.Add((field.Name, sqlType, defaultValue, field.GetValue(item)));
        }

        foreach (var prop in type.GetProperties())
        {
            DbColumnAttribute attr = (DbColumnAttribute)Attribute.GetCustomAttribute(prop, typeof(DbColumnAttribute));
            if (attr == null)
                continue;
            var sqlType = attr.SqlType;
            var defaultValue = attr.DefaultValue;
            //if (!field.CustomAttributes.Any(x => x.AttributeType == typeof(ReadOnlyAttribute)))
            lst.Add((prop.Name, sqlType, defaultValue, prop.GetValue(item)));
        }

        return lst;
    }


}
