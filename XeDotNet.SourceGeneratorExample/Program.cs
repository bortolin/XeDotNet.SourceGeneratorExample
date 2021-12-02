using XeDotNet.SourceGeneratorExample.Domain;


Console.WriteLine("XeDotNet - Source Generator Examples");

var p =  new Person() {  Id = 1 , FirstName = "Marco", LastName = "Bortolin"};
var dto = p.ToDto();

Console.WriteLine($"{dto.Id}: {dto.FirstName} {dto.LastName}");

