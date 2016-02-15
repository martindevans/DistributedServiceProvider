using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DistributedServiceProvider.Base
{
    public class TargetedIdentifierComparer
        : Comparer<Identifier512>
    {
        /// <summary>
        /// The target to measure distance to
        /// </summary>
        public readonly Identifier512 Target;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContactComparer"/> class.
        /// </summary>
        /// <param name="target">The target.</param>
        public TargetedIdentifierComparer(Identifier512 target)
        {
            Target = target;
        }

        /// <summary>
        /// Compares the specified x.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns></returns>
        public override int Compare(Identifier512 x, Identifier512 y)
        {
            var x2Target = Identifier512.Distance(x, Target);
            var y2Target = Identifier512.Distance(y, Target);

            if (x2Target > y2Target)
                return 1;
            else if (x2Target < y2Target)
                return -1;
            else
                return 0;
        }
    }
}
