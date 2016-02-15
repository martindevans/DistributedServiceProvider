using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DistributedServiceProvider.Base;

namespace DistributedServiceProvider.Contacts
{
    public class ContactComparer
        : Comparer<Contact>
    {
        private TargetedIdentifierComparer comparer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContactComparer"/> class.
        /// </summary>
        /// <param name="target">The target.</param>
        public ContactComparer(Identifier512 target)
        {
            comparer = new TargetedIdentifierComparer(target);
        }

        /// <summary>
        /// Compares the specified x.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns></returns>
        public override int Compare(Contact x, Contact y)
        {
            return comparer.Compare(x.Identifier, y.Identifier);
        }
    }
}
