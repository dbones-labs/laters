using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using JasperFx.Core;
using Weasel.Core.Identity;
using Weasel.Core.Sequences;

namespace Laters.Data.Marten;

using global::Marten;
using global::Marten.Internal.ClosedShape;
using global::Marten.Schema;
using global::Marten.Schema.Identity;
using global::Marten.Storage;
using JasperFx.CodeGeneration;
using JasperFx.CodeGeneration.Frames;
using JasperFx.Core.Reflection;

public class StringIdGeneration : IIdGeneration
{
    public void GenerateCode(GeneratedMethod method, DocumentMapping mapping)
    {
        Use use = new Use(mapping.DocumentType);
        
        method.Frames.Code(
            $"if ({{0}}.{mapping.IdMember.Name} == null || {{0}}.{mapping.IdMember.Name} == \"\") " +
            $"_setter({{0}}, {typeof(Convert).FullNameInCode()}" +
            $".ToBase64String({typeof(Guid).FullNameInCode()}.NewGuid().ToByteArray()).Replace(\"/\", \"_\").Replace(\"+\", \"-\").Substring(0, 22));",
            use);
        
        method.Frames.Code("return {0}." + mapping.IdMember.Name + ";", use);
    }

    public IEnumerable<Type> KeyTypes { get; } = new[] { typeof(string) };
    
    public bool RequiresSequences => false;
    public bool IsNumeric { get; } = false;
}


public sealed class StringIdentification<TDoc> : IIdentification<TDoc, string>
    where TDoc : notnull
{
    private readonly Func<TDoc, string> _getter;
    private readonly Action<TDoc, string>? _setter;

    [RequiresUnreferencedCode("Builds an FEC-compiled accessor delegate over the id member via LambdaBuilder.")]
    public StringIdentification(MemberInfo idMember)
    {
        _getter = LambdaBuilder.Getter<TDoc, string>(idMember);
        _setter = LambdaBuilder.Setter<TDoc, string>(idMember);
    }

    public string Identity(TDoc document) => _getter(document);

    public string AssignIfMissing(TDoc document, ISequenceSource sequences)
    {
        var current = _getter(document);
        if (current.IsNotEmpty())
        {
            return current;
        }
        
        if (_setter is null)
        {
            throw new InvalidOperationException($"{typeof(TDoc).Name} must have a writable string identity.");
        }

        string encoded = Convert.ToBase64String(Guid.NewGuid().ToByteArray());

        encoded = encoded
            .Replace("/", "_")
            .Replace("+", "-");

        var id = encoded.Substring(0, 22);
        _setter(document, id);

        return id;
    }
}

internal interface IIdentificationRegistration : IIdGeneration
{
    void Register(IDocumentStore store, DocumentMapping mapping);
}

internal sealed class IdentificationRegistration<TDoc> : IIdentificationRegistration
    where TDoc : class
{
    private readonly Func<MemberInfo, IIdentification<TDoc, string>> _factory;

    public IdentificationRegistration(Func<MemberInfo, IIdentification<TDoc, string>> factory)
    {
        _factory = factory;
    }

    public bool IsNumeric => false;

    public void Register(IDocumentStore store, DocumentMapping mapping)
    {
        store.UseIdentification(mapping, _factory(mapping.IdMember));
    }
}

public static class IdentificationExtensions
{
    private static readonly MethodInfo FindMapping = typeof(StorageFeatures)
        .GetMethod("FindMapping", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new MissingMethodException(typeof(StorageFeatures).FullName, "FindMapping");

    private static readonly MethodInfo RegisterClosedShape = typeof(ClosedShapeRegistration)
        .GetMethod("RegisterClosedShape", BindingFlags.Static | BindingFlags.NonPublic)
        ?? throw new MissingMethodException(typeof(ClosedShapeRegistration).FullName, "RegisterClosedShape");

    private static readonly PropertyInfo RegisteredDocumentTypes = typeof(StorageFeatures)
        .GetProperty("RegisteredDocumentTypes", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new MissingMemberException(typeof(StorageFeatures).FullName, "RegisteredDocumentTypes");

    [RequiresUnreferencedCode("Uses Marten's private closed-shape registration API.")]
    public static MartenRegistry.DocumentMappingExpression<TDoc> Identification<TDoc>(
        this MartenRegistry.DocumentMappingExpression<TDoc> mapping,
        Func<MemberInfo, IIdentification<TDoc, string>> factory)
        where TDoc : class
    {
        ArgumentNullException.ThrowIfNull(factory);
        return mapping.IdStrategy(new IdentificationRegistration<TDoc>(factory));
    }

    [RequiresUnreferencedCode("Uses Marten's private closed-shape registration API.")]
    internal static void RegisterIdentifications(this IDocumentStore store)
    {
        var documentStore = (DocumentStore)store;

        var documentTypes = (IEnumerable<Type>)(RegisteredDocumentTypes.GetValue(documentStore.Options.Storage)
            ?? throw new InvalidOperationException("Marten did not expose its registered document types."));

        foreach (var documentType in documentTypes)
        {
            var mapping = FindDocumentMapping(documentStore, documentType);
            if (mapping.IdStrategy is IIdentificationRegistration registration)
            {
                registration.Register(store, mapping);
            }
        }
    }

    [RequiresUnreferencedCode("Uses Marten's private closed-shape registration API.")]
    internal static void UseIdentification<TDoc, TId>(
        this IDocumentStore store,
        DocumentMapping mapping,
        IIdentification<TDoc, TId> identification)
        where TDoc : class
        where TId : notnull
    {
        if (mapping.IdType != typeof(TId))
        {
            throw new InvalidOperationException(
                $"{typeof(TDoc).Name} uses {mapping.IdType.Name} identities, not {typeof(TId).Name}.");
        }

        RegisterClosedShape
            .MakeGenericMethod(typeof(TDoc), typeof(TId))
            .Invoke(null, new object[] { (DocumentStore)store, mapping, identification });
    }

    private static DocumentMapping FindDocumentMapping(DocumentStore store, Type documentType)
    {
        return (DocumentMapping)(FindMapping.Invoke(
            store.Options.Storage,
            new object[] { documentType }) ?? throw new InvalidOperationException(
            $"Marten did not create a document mapping for {documentType.FullName}."));
    }
}
