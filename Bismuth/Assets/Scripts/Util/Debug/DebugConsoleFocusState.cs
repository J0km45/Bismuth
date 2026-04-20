using System;

public sealed class DebugConsoleFocusState
{
    public int FocusedGameObjectId { get; private set; }
    public int FocusedComponentId { get; private set; }
    public string FocusedObjectName { get; private set; } = string.Empty;
    public string FocusedComponentName { get; private set; } = string.Empty;

    public bool HasGameObjectFocus => FocusedGameObjectId != 0;
    public bool HasComponentFocus => FocusedComponentId != 0;

    public void FocusGameObject(int gameObjectId, string objectName)
    {
        FocusedGameObjectId = gameObjectId;
        FocusedComponentId = 0;
        FocusedObjectName = objectName ?? string.Empty;
        FocusedComponentName = string.Empty;
    }

    public void FocusComponent(int gameObjectId, int componentId, string objectName, string componentName)
    {
        FocusedGameObjectId = gameObjectId;
        FocusedComponentId = componentId;
        FocusedObjectName = objectName ?? string.Empty;
        FocusedComponentName = componentName ?? string.Empty;
    }

    public void Clear()
    {
        FocusedGameObjectId = 0;
        FocusedComponentId = 0;
        FocusedObjectName = string.Empty;
        FocusedComponentName = string.Empty;
    }

    public bool IsFocusedObjectParent(int gameObjectId)
    {
        return FocusedGameObjectId == gameObjectId && FocusedComponentId != 0;
    }

    public bool IsObjectFocused(int gameObjectId)
    {
        return FocusedGameObjectId == gameObjectId && FocusedComponentId == 0;
    }

    public bool IsComponentFocused(int componentId)
    {
        return FocusedComponentId == componentId;
    }

    public string GetLabel()
    {
        if (FocusedComponentId != 0)
            return $"Focus : {FocusedObjectName}/{FocusedComponentName}";

        if (FocusedGameObjectId != 0)
            return $"Focus : {FocusedObjectName} (All Components)";

        return "Focus : All";
    }

    public string GetFooterLabel(int segmentMaxLength)
    {
        if (FocusedComponentId != 0)
        {
            string objectName = TrimSegment(FocusedObjectName, segmentMaxLength);
            string componentName = TrimSegment(FocusedComponentName, segmentMaxLength);

            if (string.Equals(FocusedObjectName, FocusedComponentName, StringComparison.Ordinal))
                return $"Focus : {objectName}";

            return $"Focus : {objectName} / {componentName}";
        }

        if (FocusedGameObjectId != 0)
            return $"Focus : {TrimSegment(FocusedObjectName, segmentMaxLength)}";

        return "Focus : All";
    }

    public string GetSuffix()
    {
        if (FocusedComponentId != 0)
            return $"({FocusedObjectName}/{FocusedComponentName})";

        if (FocusedGameObjectId != 0)
            return $"({FocusedObjectName})";

        return string.Empty;
    }

    private static string TrimSegment(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        if (maxLength <= 0 || value.Length <= maxLength)
            return value;

        return value.Substring(0, maxLength) + "...";
    }
}
