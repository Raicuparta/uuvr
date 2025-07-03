using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using UnityEngine;

namespace Uuvr;

// TODO a lot of uncached reflection here.
// I want to overhaul the config for this anyway, so will clean this up when that time comes.
public class ThingDisabler : UuvrBehaviour
{
#if CPP
    public ThingDisabler(IntPtr pointer) : base(pointer)
    {
    }
#endif
    private float _previousSearchTime;

    protected override void Awake()
    {
        base.Awake();

        OnSettingChanged();
    }

    private void Update()
    {
        if (Time.unscaledTime < _previousSearchTime + ModConfiguration.Instance.ComponentSearchInterval.Value) return;

        _previousSearchTime = Time.unscaledTime;
        SearchAndDisableComponents();
    }

    private static void SearchAndDisableComponents()
    {
        try
        {
            DeactivateObjectsByComponent();
            DisableComponents();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ThingDisabler] Error during component search: {ex.Message}");
        }
    }

    private static IEnumerable<UnityEngine.Object>? FindObjects(string componentNamesString)
    {
        if (componentNamesString.IsNullOrWhiteSpace()) return null;
        
        return componentNamesString
            .Split('/')
            .Select(name => name.Trim())
            .Where(name => !string.IsNullOrEmpty(name))
            .SelectMany(name =>
            {
                var type = Type.GetType(name);
                return FindObjectsOfType(type);
            });
    }

    private static void DeactivateObjectsByComponent()
    {
        var componentObjects = FindObjects(ModConfiguration.Instance.ObjectsToDeactivateByComponent.Value);
        if (componentObjects == null) return;
        
        foreach (var componentObject in componentObjects)
        {
            var gameObjectProperty = componentObject.GetType()
                .GetProperty("gameObject", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (gameObjectProperty == null) continue;
            var gameObject = (GameObject)gameObjectProperty.GetValue(componentObject);
            if (gameObject == null) continue;
            gameObject.SetActive(false);
        }
    }

    private static void DisableComponents()
    {
        var componentObjects = FindObjects(ModConfiguration.Instance.ComponentsToDisable.Value);
        if (componentObjects == null) return;
        
        foreach (var componentObject in componentObjects)
        {
            var type = componentObject.GetType();
            var enabledProperty = type.GetProperty("enabled",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

            if (enabledProperty == null || !enabledProperty.CanWrite ||
                enabledProperty.PropertyType != typeof(bool)) return;

            enabledProperty.SetValue(componentObject, false);
        }
    }
}