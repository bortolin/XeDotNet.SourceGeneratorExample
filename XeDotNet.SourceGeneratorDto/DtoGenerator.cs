using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace XeDotNet.SourceGeneratorDto
{
    [Generator]
    public class DtoGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            var sw = new StringWriter();

            var itext = new System.CodeDom.Compiler.IndentedTextWriter(sw);
            
            var ctxr = (EntitySyntaxRec)context.SyntaxContextReceiver;

            foreach (var classDeclaration in ctxr.EntityClasses)
            {
                itext.WriteLine(FormatRecordClassMember(classDeclaration.Identifier.ValueText, GetAllPropertues(classDeclaration)));
            }
            itext.Flush();
            itext.Close();

            context.AddSource("DtoGen",sw.GetStringBuilder().ToString());
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new EntitySyntaxRec());
        }

        private IEnumerable<PropertyDeclarationSyntax> GetAllPropertues(ClassDeclarationSyntax clasDeclaration)
        {
            var props = new List<PropertyDeclarationSyntax>();

            foreach (var prop in clasDeclaration.Members)
            {
                if (prop is PropertyDeclarationSyntax propertyDeclaration)
                {
                    if (!prop.AttributeLists.Any(r=>r.ToString() == "[NoDto]")) props.Add(propertyDeclaration);
                }
            }

            return props;
        }

        private string FormatRecordClassMember(string name, IEnumerable<PropertyDeclarationSyntax> props)
        {
            var sb = new StringBuilder();
            
            sb.Append($"public record {name}Dto (");

            foreach (var prop in props)
            {
                sb.Append(prop.Type.ToString());
                sb.Append(" ");
                sb.Append(prop.Identifier.ValueText);
                sb.Append(", ");
            }
            sb.Append(");");
            sb.Replace(", );",");");
            return sb.ToString();
        }
    }

    public class EntitySyntaxRec : ISyntaxContextReceiver
    {
        public List<ClassDeclarationSyntax> EntityClasses = new List<ClassDeclarationSyntax>();

        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            if (context.Node is ClassDeclarationSyntax classDeclaration)
            {
                var symbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;

                if (symbol != null && symbol.AllInterfaces.Any(r=>r.Name == "IEntity"))
                {
                    EntityClasses.Add(classDeclaration);
                }
            }
        }
    }
}
