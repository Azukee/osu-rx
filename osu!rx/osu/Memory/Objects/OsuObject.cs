using osu_rx.Dependencies;
using System;

namespace osu_rx.osu.Memory.Objects
{
    public abstract class OsuObject
    {
        protected OsuProcess OsuProcess;

        public virtual UIntPtr BaseAddress { get; protected set; }

        public OsuObject() => OsuProcess = DependencyContainer.Get<OsuProcess>();
    }
}
