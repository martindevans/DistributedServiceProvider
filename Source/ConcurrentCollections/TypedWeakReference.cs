using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConcurrentCollections
{
    /// <summary>
    /// A WeakReference with an enforced type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class TypedWeakReference<T>
        where T : class
    {
        private WeakReference reference;

        /// <summary>
        /// Gets a value indicating whether this instance is alive.
        /// </summary>
        /// <value><c>true</c> if this instance is alive; otherwise, <c>false</c>.</value>
        public bool IsAlive
        {
            get
            {
                return reference.IsAlive;
            }
        }
        /// <summary>
        /// Gets the target.
        /// </summary>
        /// <value>The target.</value>
        public T Target
        {
            get
            {
                return reference.Target as T;
            }
        }
        /// <summary>
        /// Gets a value indicating whether the target is tracked after finalisation.
        /// </summary>
        /// <value><c>true</c> if the target is tracked after finalisation; otherwise, <c>false</c>.</value>
        public bool TrackResurrection
        {
            get
            {
                return reference.TrackResurrection;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TypedWeakReference&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="target">The target.</param>
        public TypedWeakReference(T target)
        {
            reference = new WeakReference(target);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TypedWeakReference&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="trackResurrection">if set to <c>true</c> the target is tracked after finalisation.</param>
        public TypedWeakReference(T target, bool trackResurrection)
        {
            reference = new WeakReference(target, trackResurrection);
        }

        /// <summary>
        /// Determines whether the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>.
        /// If you pass in an object of Type T, the object and the Target will be compared, if you pass in a typed weak reference, the targets of both will be compared.
        /// </summary>
        /// <param name="obj">The <see cref="T:System.Object"/> to compare with the current <see cref="T:System.Object"/>.</param>
        /// <returns>
        /// true if the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>; otherwise, false.
        /// </returns>
        /// <exception cref="T:System.NullReferenceException">The <paramref name="obj"/> parameter is null.</exception>
        public override bool Equals(object obj)
        {
            if (obj is T)
            {
                return (obj as T).Equals(Target);
            }
            else if (obj is TypedWeakReference<T>)
            {
                return (obj as TypedWeakReference<T>).Target.Equals(this.Target);
            }
            else
                return base.Equals(obj);
        }

        /// <summary>
        /// Returns the hash code of the target of this weak reference
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"/>.
        /// </returns>
        public override int GetHashCode()
        {
            return Target.GetHashCode();
        }
    }
}
