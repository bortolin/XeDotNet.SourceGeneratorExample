using XeDotNet.SourceGeneratorExample.Domain;


Console.WriteLine("XeDotNet - Source Generator Examples");

var person =  new Person() {  Id = 1 , FirstName = "Mario", LastName = "Rossi", Age=43};
var dtoPerson = person.ToDto();

Console.WriteLine($"{dtoPerson.Id}: {dtoPerson.FirstName} {dtoPerson.LastName}");

var car = new Car() { Id = 1, Model = "Wagon", Motor = "Benz", Gears = 5 };
var dtoCar = car.ToDto();

Console.WriteLine($"{dtoCar.Id}: {dtoCar.Model} - {dtoCar.Motor}");
