using composite.unity.Core;
using UnityEngine;

namespace composite.unity.Example
{
    public class Example2CompositeRoot : CompositeRootBase
    {
        [SerializeField] private int somePayload = 1337;

        public override void InstallBindings()
        {
            var booServiceIfLocal = GetLocal<IFooService>();
            var booService3IfLocal = GetLocal<IFooService>();

        }
    }
}