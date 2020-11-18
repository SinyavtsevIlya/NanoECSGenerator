using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NanoEcs.Generator
{
    public static class ParsedComponentExtensions
    {
        public static bool IsReactive(this NanoEcsGenerator.ParsedComponent component)
        {
            return component.Attributes.Contains(NanoEcsGenerator.ReactiveAttribute) || component.ForseReactive;
        }
    }
}
