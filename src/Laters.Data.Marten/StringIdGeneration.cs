namespace Laters.Data.Marten;

using global::Marten.Schema;
using global::Marten.Schema.Identity;
using JasperFx.CodeGeneration;
using JasperFx.CodeGeneration.Frames;
using JasperFx.Core.Reflection;

public class StringIdGeneration : IIdGeneration
{
    public void GenerateCode(GeneratedMethod method, DocumentMapping mapping)
    {
        Use use = new Use(mapping.DocumentType);
        method.Frames.Code(
            $"if ({{0}}.{mapping.IdMember.Name} == null)" +
            $" _setter({{0}}, {typeof(Convert).FullNameInCode()}" +
            $".ToBase64String({typeof(Guid).FullNameInCode()}.NewGuid().ToByteArray()).Replace(\"/\", \"_\").Replace(\"+\", \"-\").Substring(0, 22));",
            use);
        method.Frames.Code("return {0}." + mapping.IdMember.Name + ";", use);
    }

    public IEnumerable<Type> KeyTypes { get; } = new[] { typeof(string) };
    public bool RequiresSequences => false;
}