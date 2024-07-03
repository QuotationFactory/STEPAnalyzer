using System.Text.RegularExpressions;

var entityRegex = new Regex(@"(#\d+)=(.*?)\((.*)\);", RegexOptions.Compiled);
var paramRegex = new Regex(@"(#\d+)", RegexOptions.Compiled);

var fileName = args[0];
var lines = File.ReadAllLines(fileName);
var entities = new List<Entity>();

var previousLine = "";
for (var index = 0; index < lines.Length; index++)
{
    var line = previousLine + lines[index];
    if (!line.EndsWith(";"))
    {
        previousLine = line;
        continue;
    }

    previousLine = "";
    var match = entityRegex.Match(line);
    if (!match.Success) continue;

    var entity = new Entity()
    {
        EntityId = match.Groups[1].Value,
        EntityName = match.Groups[2].Value,
        OriginalLine = line
    };
    var paramString = match.Groups[3].Value;
    var paramMatches = paramRegex.Matches(paramString);
    foreach (Match paramMatch in paramMatches)
    {
        entity.Parameters.Add(paramMatch.Value);
    }

    // Console.WriteLine($"{entity.EntityId}={entity.EntityName}({string.Join(",", entity.Parameters)});");
    entities.Add(entity);
}

var entitiesById = entities.ToDictionary(x => x.EntityId);

foreach (var entity in entities)
{
    if (entity.Parameters.Count > 0)
    {
        foreach (var parameter in entity.Parameters)
        {
            if (!entitiesById.TryGetValue(parameter, out var refEntity))
            {
                Console.Error.WriteLine($"Entity {entity.EntityId}={entity.EntityName}: Could not find parameter entity: {parameter}");
                continue;
            }
            
            entity.ReferencedEntities.Add(refEntity);
        }
    }
}

var filter = "MANIFOLD_SOLID_BREP";
if (args.Length >= 2)
{
    filter = args[1];
    if (filter == "-faces" || filter == "-f")
    {
        filter = "ADVANCED_FACE";
    }
}

if (!string.IsNullOrEmpty(filter))
{
    entities = entities.Where(x => x.EntityName.Equals(filter, StringComparison.OrdinalIgnoreCase)).ToList();
}

foreach (var entity in entities)
{
    PrintEntity(entity, 0);
}

void PrintEntity(Entity entity, int indent)
{
    Console.Write(new string(' ', indent));
    Console.WriteLine(entity.OriginalLine);
    foreach (var refEntity in entity.ReferencedEntities)
    {
        PrintEntity(refEntity, indent + 2);
    }
}

class Entity
{
    public required string EntityId;
    public required string EntityName;
    public readonly List<string> Parameters = new();
    public readonly List<Entity> ReferencedEntities = new();
    public required string OriginalLine;
}