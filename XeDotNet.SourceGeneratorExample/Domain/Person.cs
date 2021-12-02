using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XeDotNet.SourceGenerator;

namespace XeDotNet.SourceGeneratorExample.Domain
{
    public class Person : IEntity
    {
        public int Id { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        [NoDto]
        public int Age { get; set; }

    }

    
}
