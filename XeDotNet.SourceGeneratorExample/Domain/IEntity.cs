using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XeDotNet.SourceGeneratorExample.Domain
{
    internal interface IEntity
    {
        int Id { get; set; }
    }

    [System.AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
    sealed class NoDto : Attribute
    {
       
    }
}
