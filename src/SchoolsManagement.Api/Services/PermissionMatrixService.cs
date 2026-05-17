using System.Text.Json;
using System.Text.Json.Nodes;
using SchoolsManagement.Api.Models.Identity;
using SchoolsManagement.Api.Security;

namespace SchoolsManagement.Api.Services;

public class PermissionMatrixService
{
    public static readonly string[] ActionKeys = ["view", "create", "edit", "delete"];
    public static readonly string[] ClientActionKeys = ["view", "add", "edit", "delete"];

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public JsonObject CreateEmptyMatrix()
    {
        var root = new JsonObject();
        foreach (var def in PermissionCatalog.All)
        {
            root[def.Key] = new JsonObject
            {
                ["view"] = false,
                ["create"] = false,
                ["edit"] = false,
                ["delete"] = false,
            };
        }

        return root;
    }

    public JsonObject CreateFullMatrix()
    {
        var root = new JsonObject();
        foreach (var def in PermissionCatalog.All)
        {
            root[def.Key] = new JsonObject
            {
                ["view"] = true,
                ["create"] = true,
                ["edit"] = true,
                ["delete"] = true,
            };
        }

        return root;
    }

    public JsonObject NormalizeMatrix(JsonNode? source, IReadOnlyCollection<string>? pageKeys = null)
    {
        var empty = CreateEmptyMatrix();
        if (source is null && pageKeys is { Count: > 0 })
        {
            foreach (var key in pageKeys)
            {
                if (empty[key] is JsonObject section)
                {
                    section["view"] = true;
                }
            }

            return empty;
        }

        if (source is not JsonObject obj)
        {
            return empty;
        }

        foreach (var def in PermissionCatalog.All)
        {
            if (empty[def.Key] is not JsonObject target)
            {
                continue;
            }

            if (obj[def.Key] is not JsonObject row)
            {
                continue;
            }

            target["view"] = ReadBool(row, "view");
            target["create"] = ReadBool(row, "create") || ReadBool(row, "add");
            target["edit"] = ReadBool(row, "edit");
            target["delete"] = ReadBool(row, "delete");
        }

        return empty;
    }

    public List<string> PageKeysFromMatrix(JsonObject matrix)
    {
        var keys = new List<string>();
        foreach (var def in PermissionCatalog.All)
        {
            if (matrix[def.Key] is JsonObject row && ReadBool(row, "view"))
            {
                keys.Add(def.Key);
            }
        }

        return keys;
    }

    public JsonObject ToClientMatrix(JsonObject internalMatrix)
    {
        var client = new JsonObject();
        foreach (var def in PermissionCatalog.All)
        {
            if (internalMatrix[def.Key] is not JsonObject row)
            {
                continue;
            }

            client[def.Key] = new JsonObject
            {
                ["view"] = ReadBool(row, "view"),
                ["add"] = ReadBool(row, "create"),
                ["edit"] = ReadBool(row, "edit"),
                ["delete"] = ReadBool(row, "delete"),
            };
        }

        return client;
    }

    public JsonObject FromClientMatrix(JsonObject clientMatrix)
    {
        var internalMatrix = CreateEmptyMatrix();
        foreach (var def in PermissionCatalog.All)
        {
            if (clientMatrix[def.Key] is not JsonObject row)
            {
                continue;
            }

            if (internalMatrix[def.Key] is not JsonObject target)
            {
                continue;
            }

            target["view"] = ReadBool(row, "view");
            target["create"] = ReadBool(row, "add") || ReadBool(row, "create");
            target["edit"] = ReadBool(row, "edit");
            target["delete"] = ReadBool(row, "delete");
        }

        return internalMatrix;
    }

    public string Serialize(JsonObject matrix) => matrix.ToJsonString(JsonOpts);

    public JsonObject? TryParse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonNode.Parse(json) as JsonObject;
        }
        catch
        {
            return null;
        }
    }

    private static bool ReadBool(JsonObject row, string key)
    {
        var node = row[key];
        if (node is null)
        {
            return false;
        }

        if (node is JsonValue val)
        {
            if (val.TryGetValue<bool>(out var b))
            {
                return b;
            }

            if (val.TryGetValue<string>(out var s))
            {
                return bool.TryParse(s, out var parsed) && parsed;
            }
        }

        return false;
    }
}
