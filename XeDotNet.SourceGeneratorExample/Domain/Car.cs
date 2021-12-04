using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XeDotNet.SourceGenerator;

namespace XeDotNet.SourceGeneratorExample.Domain
{
    public class Car : IEntity
    {
        public int Id { get; set; }

        public string Model { get; set; }

        public string Motor { get; set; }

        [NoDto]
        public int Gears { get; set; }

    }

    
}
