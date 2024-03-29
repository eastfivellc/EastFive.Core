﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EastFive
{
    public delegate object UnsettableDelegate<T>(
            Func<T, object> hasValue,
            Func<object> unset);

    /// <summary>
    /// Struct wrapping a nullable type.
    /// The main distinction point between
    /// Settable and Nullable is the IsSet
    /// property, which is false for the default value,
    /// but true if an explicit value has been specified
    /// (evev if it is null).
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct Settable<T>
    {
        private T _value;
        private bool _isSet;

        /// <summary>
        /// Standard constructor.
        /// </summary>
        /// <param name="value"></param>
        public Settable(T value)
        {
            _isSet = true;
            _value = value;
        }

        /// <summary>
        /// True if a value has been set, even if it is null.
        /// </summary>
        public bool IsSet
        {
            get { return _isSet; }
        }

        /// <summary>
        /// Gets the value of the current <see cref="Settable{T}"/>. 
        /// </summary>
        public T Value
        {
            get
            {
                if (_isSet)
                    return _value;

                throw new InvalidOperationException("The value is null or undefined.");
            }
            set
            {
                _isSet = true;
                _value = value;
            }
        }

        /// <summary>
        /// Conversion from value
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static implicit operator Settable<T>(T value)
        {
            var s = new Settable<T>
            {
                _isSet = true,
                _value = value,
            };

            return s;
        }

        /// <summary>
        /// Conversion to value.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static implicit operator T(Settable<T> value)
        {
            if (value._isSet)
                return value._value;

            throw new InvalidOperationException("Cannot convert null or undefined values.");
        }

        /// <summary>
        /// Indicates whether this instance and a specified object are equal.
        /// </summary>
        /// <returns>
        /// true if <paramref name="other"/> and this instance are the same type and represent the same value; otherwise, false.
        /// </returns>
        /// <param name="other">Another object to compare to. </param>
        public override bool Equals(object other)
        {
            if (!_isSet)
                return other == null;
            return _value.Equals(other);
        }

        /// <summary>
        /// Implementation of the equals operator.
        /// </summary>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <returns></returns>
        public static bool operator ==(Settable<T> t1, Settable<T> t2)
        {
            // undefined equals undefined
            if (!t1._isSet && !t2.IsSet) return true;

            // undefined != everything else
            if (t1._isSet != t2._isSet) return false;

            // if both are values, compare them
            return t1._value.Equals(t2._value);
        }

        /// <summary>
        /// Implementation of the inequality operator.
        /// </summary>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <returns></returns>
        public static bool operator !=(Settable<T> t1, Settable<T> t2)
        {
            return !(t1 == t2);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>
        /// A 32-bit signed integer that is the hash code for this instance.
        /// </returns>
        public override int GetHashCode()
        {
            if (!_isSet) return -1;
            return _value.GetHashCode();
        }

        /// <summary>
        /// Returns a text representation of
        /// the value, or an empty string if no value
        /// is present.
        /// </summary>
        public override string ToString()
        {
            return _isSet ?
                 _value.ToString() :
                "undefined";
        }
    }
}
