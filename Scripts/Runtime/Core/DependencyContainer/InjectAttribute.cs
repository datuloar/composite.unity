using System;
using JetBrains.Annotations;

namespace composite.unity.Core
{
    [MeansImplicitUse, AttributeUsage(AttributeTargets.Method)]
    public class InjectAttribute : Attribute
    {

    }
}
