using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace XeDotNet.SourceGenerator
{
    /// <summary>
    /// Generatore di codice sorgente che per ogni classe che implementa IEntity genera il relativo dto con tutte le propietà
    /// </summary>
    [Generator]
    public class DtoGenerator : ISourceGenerator
    {

        // Attrbuto custom NoDto per decorare le propietà da on generare
        private const string attributeText = @"
        using System;
        namespace XeDotNet.SourceGenerator
        {
            [System.AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
            sealed class NoDto : Attribute
            {
       
            }
        }
        ";

        public void Initialize(GeneratorInitializationContext context)
        {
            // Resgitro l'attributo nel codice sorgente
            context.RegisterForPostInitialization((i) => i.AddSource("NoDtoAttribute", attributeText));

            // Registro il mio syntax receiver per collezionare le classi di cui generare i dto
            context.RegisterForSyntaxNotifications(() => new EntitySyntaxRec());
        }

        public void Execute(GeneratorExecutionContext context)
        {
           
            // Recupero dal contesto di compilazione il mio syntax receiver
            var ctxr = (EntitySyntaxRec)context.SyntaxContextReceiver;
            
            // Genero tutti i dto passandoli le classe trovate tramite il mio syntax receiver
            var generatedDto = GenerateAllDto(ctxr);
            
            // Genero tutti i meteodi di mappatura passandoli le classe trovate tramite il mio syntax receiver
            var generatedDtoMap = GenerateAllMap(ctxr, context);

            // Aggiungo tutti i dto generati al codice sorgente
            context.AddSource("GeneratedDto", generatedDto);

            // Aggiungo tutti i meteodi di mappatura al codice sorgente
            context.AddSource("GeneratedDtoMap", generatedDtoMap);
        }

        private string GenerateAllDto(EntitySyntaxRec ctxr)
        {
            using (var sw = new StringWriter())
            using (var itext = new System.CodeDom.Compiler.IndentedTextWriter(sw))
            {
                foreach (var classDeclaration in ctxr.EntityClasses)
                {
                    var className = classDeclaration.Identifier.ValueText;
                    itext.WriteLine(FormatRecordClassMember(className, GetAllPropertues(classDeclaration)));
                }
                return sw.ToString();
            }
        }

        private string GenerateAllMap(EntitySyntaxRec ctxr, GeneratorExecutionContext context)
        {
            using (var sw = new StringWriter())
            using (var itext = new System.CodeDom.Compiler.IndentedTextWriter(sw))
            {
                itext.WriteLine("public static class GeneratedDtoMapper");
                itext.WriteLine("{");
                itext.Indent++;

                foreach (var classDeclaration in ctxr.EntityClasses)
                {
                    var className = classDeclaration.Identifier.ValueText;

                    // ottengo il namespace in cui è dichiarata la classe
                    if (classDeclaration?.Parent is NamespaceDeclarationSyntax namespaceDeclarationSyntax)
                    {
                        var fullClassName = $"{namespaceDeclarationSyntax.Name.ToString()}.{className}" ;
                        itext.WriteLine(FormatClassMapExtension(className, fullClassName, GetAllPropertues(classDeclaration)));
                    }
                    else
                    {
                        //Emetto un report di diagnastica per segnalare il problema
                        context.ReportDiagnostic(Diagnostic.Create(
                                                    new DiagnosticDescriptor(
                                                        "SG0001",
                                                        "Uanble find class namespace",
                                                        "Namespace no found for class {0}",
                                                        "DtoMapGenerator",
                                                        DiagnosticSeverity.Error,true), classDeclaration.GetLocation(), className));
                    }
                }

                itext.Indent--;
                itext.WriteLine("}");
                return sw.ToString();
            }
        }

        private IEnumerable<PropertyDeclarationSyntax> GetAllPropertues(ClassDeclarationSyntax clasDeclaration)
        {
            var props = new List<PropertyDeclarationSyntax>();

            foreach (var prop in clasDeclaration.Members)
            {
                if (prop is PropertyDeclarationSyntax propertyDeclaration)
                {
                    if (!prop.AttributeLists.SelectMany(r=>r.Attributes).Any(r => r.Name.ToString().Trim() == "NoDto")) props.Add(propertyDeclaration);
                }
            }

            return props;
        }

        private string FormatRecordClassMember(string name, IEnumerable<PropertyDeclarationSyntax> props)
        {
            //Genero la record class con tutte le propietà a partire dalla classe
            //es. public record ClassNameDto (int prop1, string prop2, ...)
            var sb = new StringBuilder();

            foreach (var prop in props)
            {
                sb.Append(prop.Type.ToString()); //Tipo
                sb.Append(" ");
                sb.Append(prop.Identifier.ValueText); //Nome propietà
                sb.Append(", ");
            }
            if (sb.Length > 0) sb.Remove(sb.Length - 2, 2); //elimino l'eventuale vigola finale
            
            sb.Insert(0,$"public record {name}Dto (");
            sb.Append(");");

            return sb.ToString();
        }

        private string FormatClassMapExtension(string name, string fullClassName, IEnumerable<PropertyDeclarationSyntax> props)
        {
            //Genero metodo di estensione ToDto per la classe che ritorna il dto
            //es. public static ClassNameDto ToDto(this Namespace.ClassName entity) => new ClassNameDto(entity.Prop1, entity.Prop2, ...);
            var sb = new StringBuilder();

            foreach (var prop in props)
            {
                sb.Append($"entity.{prop.Identifier.ValueText}"); //Nome propietà
                sb.Append(", ");
            }
            if (sb.Length > 0) sb.Remove(sb.Length - 2, 2); //elimino l'eventuale vigola finale

            sb.Insert(0, $"public static {name}Dto ToDto(this {fullClassName} entity) => new {name}Dto(");
            sb.Append(");");

            return sb.ToString();
        }

    }

    /// <summary>
    ///  Syntax Receiver usato per eplorare i nodi dell'albero delle espresisoni del mio codice sorgente
    /// </summary>
    public class EntitySyntaxRec : ISyntaxContextReceiver
    {
        public List<ClassDeclarationSyntax> EntityClasses = new List<ClassDeclarationSyntax>();

        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            // Verifico di essere su una dichiarazione di classe
            if (context.Node is ClassDeclarationSyntax classDeclaration)
            {
                // Utilizzo il modello semantico per ottenre i simboli collegati alla dichiarazione di classe
                var symbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;

                // Verifico che la classe implementi l'intefaccia IEntity
                if (symbol != null && symbol.AllInterfaces.Any(r => r.Name == "IEntity"))
                {
                    // Aggiungo la defizione di classe alla collezione di classi trovare
                    EntityClasses.Add(classDeclaration);
                }
            }
        }
    }
}
