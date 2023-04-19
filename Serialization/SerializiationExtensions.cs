using EastFive.Extensions;
using EastFive.Linq;
using EastFive.Reflection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace EastFive.Serialization
{
    public static class HashExtensions
    {
        #region ByteArray

        #region Numbers

        #region Ints

        public static int[] ToIntsFromByteArray(this byte[] byteArrayOfInts)
        {
            if (byteArrayOfInts == null)
                return new int[] { };

            var intStorageLength = sizeof(int);
            return Enumerable.Range(0, byteArrayOfInts.Length / intStorageLength)
                .Select((index) => BitConverter.ToInt32(byteArrayOfInts, index * intStorageLength))
                .ToArray();
        }

        public static byte[] ToByteArrayOfInts(this IEnumerable<int> ints)
        {
            return ints.SelectMany(i => BitConverter.GetBytes(i)).ToArray();
        }
        
        #endregion

        #region Longs

        public static long[] ToLongsFromByteArray(this byte[] byteArrayOfLongs)
        {
            if (byteArrayOfLongs == null)
                return new long[] { };

            var longStorageLength = sizeof(long);
            return Enumerable.Range(0, byteArrayOfLongs.Length / longStorageLength)
                .Select((index) => BitConverter.ToInt64(byteArrayOfLongs, index * longStorageLength))
                .ToArray();
        }

        public static byte[] ToByteArrayOfLongs(this IEnumerable<long> longs)
        {
            return longs.SelectMany(i => BitConverter.GetBytes(i)).ToArray();
        }

        #endregion

        #region Decimal

        public static byte[] ToByteArrayOfDecimals(this IEnumerable<decimal> dec)
        {
            return dec.SelectMany(i => i.ConvertToBytes()).ToArray();
        }

        public static byte[] ConvertToBytes(this decimal dec)
        {
            //Load four 32 bit integers from the Decimal.GetBits function
            var bytes = decimal.GetBits(dec)
                .SelectMany(i => BitConverter.GetBytes(i))
                .ToArray();
            return bytes;
        }

        public static bool TryConvertToDecimal(this IEnumerable<byte> bytesEnumerable, out decimal value)
        {
            var bytes = bytesEnumerable.ToArray();
            //check that it is even possible to convert the array
            if (bytes.Length != 16)
            {
                value = default(decimal);
                return false;
            }
            //make an array to convert back to int32's
            Int32[] bits = bytes
                .Segment(4)
                .Select(byteBlock => BitConverter.ToInt32(byteBlock.ToArray(), 0))
                .ToArray();

            try
            {
                //Use the decimal's new constructor to
                //create an instance of decimal
                value = new decimal(bits);
                return true;
            } catch(ArgumentException) 
            {
                // sometimes the bytes, despite being the correct length, are not valid decimal bytes
                value = default(decimal);
                return false;
            }
        }

        public static decimal[] ToDecimalsFromByteArray(this byte[] bytes)
        {
            if (bytes == null)
                return new decimal[] { };

            var storageLength = sizeof(decimal);
            return bytes
                .Segment(storageLength)
                .Where(bytesSegment => bytesSegment.TryConvertToDecimal(out decimal value))
                .Select(
                    bytesSegment =>
                    {
                        bytesSegment.TryConvertToDecimal(out decimal value);
                        return value;
                    })
                .ToArray();
        }

        #endregion

        #region Double

        public static byte[] ToByteArrayOfDoubles(this IEnumerable<double> values)
        {
            return values.SelectMany(v => BitConverter.GetBytes(v)).ToArray();
        }

        public static double[] ToDoublesFromByteArray(this byte[] byteArrayOfDoubles)
        {
            if (byteArrayOfDoubles == null)
                return new double[] { };

            var doubleStorageLength = sizeof(double);
            return Enumerable.Range(0, byteArrayOfDoubles.Length / doubleStorageLength)
                .Select((index) => BitConverter.ToDouble(byteArrayOfDoubles, index * doubleStorageLength))
                .ToArray();
        }      

        #endregion

        #endregion

        #region Guids

        public static Guid[] ToGuidsFromByteArray(this IEnumerable<byte> bytesOfGuids)
        {
            if (bytesOfGuids == null)
                return new Guid[] { };
            var byteArrayOfGuids = bytesOfGuids.ToArray();

            var guidStorageLength = Guid.NewGuid().ToByteArray().Length;
            return Enumerable.Range(0, byteArrayOfGuids.Length / guidStorageLength)
                .Select((index) => byteArrayOfGuids.Skip(index * guidStorageLength).Take(guidStorageLength).ToArray())
                .Select((byteArray) => new Guid(byteArray))
                .ToArray();
        }

        public static byte[] ToByteArrayOfGuids(this IEnumerable<Guid> guids)
        {
            if (default(IEnumerable<Guid>) == guids)
                return new byte[] { };
            return guids.SelectMany(guid => guid.ToByteArray()).ToArray();
        }

        #endregion

        #region Dates
        
        public static DateTime[] ToDateTimesFromByteArray(this byte[] byteArrayOfDates)
        {
            return byteArrayOfDates
                .ToLongsFromByteArray()
                .Select(ticks => new DateTime(ticks, DateTimeKind.Utc))
                .ToArray();
        }

        public static byte[] ToByteArrayOfDateTimes(this IEnumerable<DateTime> dates)
        {
            return dates.SelectMany(date => BitConverter.GetBytes(date.Ticks)).ToArray();
        }

        public static DateTime[] ToDatesFromByteArray(this byte[] byteArrayOfDates)
        {
            return byteArrayOfDates
                .ToIntsFromByteArray()
                .Select(day => new DateTime(day >> 9, (day >> 5) & 0xF, day & 0x1F))
                .ToArray();
        }

        public static byte[] ToByteArrayFromDates(this IEnumerable<DateTime> dates)
        {
            return dates.SelectMany(date => BitConverter.GetBytes((date.Year << 9) | (date.Month << 5) | date.Day)).ToArray();
        }

        public static DateTime?[] ToNullableDateTimesFromByteArray(this byte[] byteArrayOfDates)
        {
            return byteArrayOfDates
                .ToLongsFromByteArray()
                .Select(ticks => (ticks < DateTime.MinValue.Ticks || ticks > DateTime.MaxValue.Ticks) ? default(DateTime?) : new DateTime(ticks, DateTimeKind.Utc))
                .ToArray();
        }

        public static byte[] ToByteArrayOfNullableDateTimes(this IEnumerable<DateTime?> dates)
        {
            return dates.SelectMany(date => BitConverter.GetBytes(date.HasValue ? ((DateTime)date).Ticks : 0)).ToArray();
        }

        #endregion

        #region Strings
        
        public static string[] ToStringsFromUTF8ByteArray(this byte[] byteArrayOfStrings)
        {
            if (byteArrayOfStrings.IsDefaultNullOrEmpty())
                return new string[] { };

            return byteArrayOfStrings
                .FromByteArray(
                    (bytes) => System.Text.Encoding.UTF8.GetString(bytes))
                .ToArray();
        }

        public static byte[] ToUTF8ByteArrayOfStrings(this IEnumerable<string> strings)
        {
            return strings
                .NullToEmpty()
                .ToByteArray(
                    str =>
                    {
                        return str.IsDefaultOrNull() ?
                            new byte[] { }
                            :
                            Encoding.UTF8.GetBytes(str);
                    });
        }

        public static string[] ToStringNullOrEmptysFromUTF8ByteArray(this byte[] byteArrayOfStrings)
        {
            if (byteArrayOfStrings.IsDefaultNullOrEmpty())
                return new string[] { };

            return byteArrayOfStrings
                .FromByteArray(
                    (bytes) =>
                    {
                        if(!bytes.Any())
                            return System.Text.Encoding.UTF8.GetString(new byte[] { });

                        var indicatorByte = bytes[0];
                        var strBytes = bytes.Skip(1).ToArray();
                        if (indicatorByte == 0)
                        {
                            var str = System.Text.Encoding.UTF8.GetString(strBytes);
                            return str;
                        }

                        if (indicatorByte == 1)
                            return string.Empty;

                        return null;
                    })
                .ToArray();
        }

        public static byte[] ToUTF8ByteArrayOfStringNullOrEmptys(this IEnumerable<string> strings)
        {
            return strings
                .NullToEmpty()
                .ToByteArray(
                    str =>
                    {
                        if(null == str)
                            return new byte[] { 2 };

                        if (str.Length > 0)
                            return (new byte[] { 0 })
                                .Concat(Encoding.UTF8.GetBytes(str))
                                .ToArray();

                        if (string.Empty == str)
                            return new byte[] { 1 };

                        // Should never hit
                        return new byte[] { 2 };
                    });
        }

        public static string GetString(this byte[] bytes, Encoding encoding = default(Encoding))
        {
            if (encoding.IsDefaultOrNull())
                encoding = Encoding.UTF8;
            return encoding.GetString(bytes);
        }

        public static byte[] GetBytes(this string text, Encoding encoding = default(Encoding))
        {
            if (text.IsDefaultOrNull())
                return new byte[] { };
            if (encoding.IsDefaultOrNull())
                encoding = Encoding.UTF8;
            return encoding.GetBytes(text);
        }

        #endregion

        #region Enums

        public static object ToEnumsFromByteArray(this byte[] byteArrayOfEnums, Type enumType,
            bool repair = false)
        {
            if(repair)
                return byteArrayOfEnums
                    .ToStringsFromUTF8ByteArray()
                    .Select(
                        enumName =>
                        {
                            if (!Enum.TryParse(enumType, enumName, out object result))
                                return null;
                            return result;
                            // Enum.Parse(enumType, enumName);
                        })
                    .Where(v => v != null)
                    .CastArray(enumType);

            return byteArrayOfEnums
                .ToStringsFromUTF8ByteArray()
                .Select(enumName => Enum.Parse(enumType, enumName))
                .CastArray(enumType);
        }

        public static T[] ToEnumsFromByteArray<T>(this byte [] byteArrayOfEnums)
        {
            var enumType = typeof(T);
            return byteArrayOfEnums
                .ToStringsFromUTF8ByteArray()
                .Select(enumName => (T)Enum.Parse(enumType, enumName))
                .ToArray();
        }

        public static byte[] ToByteArrayOfEnums(this IEnumerable<object> enums, Type enumType)
        {
            return enums
                .NullToEmpty()
                .Select(enumValue => Enum.GetName(enumType, enumValue))
                .ToUTF8ByteArrayOfStrings();
        }

        public static byte[] ToByteArrayOfEnums<T>(this IEnumerable<T> enums) where T : System.Enum // : struct, IConvertible // When we switch to C# 7.3 
        {
            if (!typeof(T).IsEnum)
            {
                throw new ArgumentException("T must be an enumerated type");
            }
            return enums
                .NullToEmpty()
                .Select(enumValue => Enum.GetName(typeof(T), enumValue))
                .ToUTF8ByteArrayOfStrings();
        }

        #endregion

        public static byte[] ToByteArray<TKey, TValue>(this IDictionary<TKey, TValue> obj,
            Func<TKey, byte[]> keyConverter, Func<TValue, byte[]> valueConverter)
        {
            if (default(IDictionary<TKey, TValue>) == obj)
            {
                return BitConverter.GetBytes(((int)0));
            }

            var meat = obj.Select((kvp) =>
            {
                var keyBytes = keyConverter(kvp.Key);
                var valueBytes = valueConverter(kvp.Value);
                var bytes = new byte[][]
                {
                    BitConverter.GetBytes(keyBytes.Length),
                    keyBytes,
                    BitConverter.GetBytes(valueBytes.Length),
                    valueBytes,
                };
                return bytes.SelectMany(b => b).ToArray();
            });
            return meat.SelectMany(b => b).ToArray();
        }

        public static IDictionary<TKey, TValue> FromByteArray<TKey, TValue>(this byte[] data,
            Func<byte[], TKey> keyConverter, Func<byte[], TValue> valueConverter)
        {
            var offsets = FromByteArrayOffsets(data).ToArray();
            var byteLines = offsets
                .Select(offset =>
                    data
                        .Skip(offset + sizeof(Int32))
                        .Take(BitConverter.ToInt32(data, offset))
                        .ToArray())
                .ToArray();
            var kvps = byteLines
                .SelectEvenOdd(
                    bytes => keyConverter(bytes),
                    bytes => valueConverter(bytes));
            var result = kvps
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value);
            return result;
        }

        public static IEnumerable<IEnumerable<TItem>> Segment<TItem>(this IEnumerable<TItem> items, int segmentSize)
        {
            if (segmentSize <= 0)
                throw new ArgumentException("Segment size must be greater than 0", "segmentSize");
            var enumerator = items.GetEnumerator();
            while(enumerator.MoveNext())
            {
                var results = SegmentSubset(enumerator, segmentSize).ToArray();
                // TODO: Make this work w/o the ToArray() unless the outer enumerator 
                // is iterated before the inner enumerator.
                yield return results;
            }
        }

        private static IEnumerable<TItem> SegmentSubset<TItem>(this IEnumerator<TItem> items, int segmentSize)
        {
            do
            {
                yield return items.Current;
                segmentSize--;
            } while(segmentSize > 0 && items.MoveNext());
        }

        public static IEnumerable<TItem[]> Segment<TItem>(this IEnumerable<TItem> items,
            Func<TItem[], int, bool> shouldSplit)
        {
            var enumerator = items.GetEnumerator();
            int index = 0;
            var set = new TItem[] { };
            while (enumerator.MoveNext())
            {
                set = set.Append(enumerator.Current).ToArray();
                index = index + 1;
                if (shouldSplit(set, index))
                {
                    yield return set;
                    set = new TItem[] { };
                }
            }
            if (set.Any())
                yield return set;
        }

        /// <summary>
        /// Index 0 is even so starts with even (comp sci, not math)
        /// </summary>
        public static IEnumerable<KeyValuePair<TEven, TOdd>> SelectEvenOdd<TSelect, TEven, TOdd>(
            this IEnumerable<TSelect> items, Func<TSelect, TEven> evenSelect, Func<TSelect, TOdd> oddSelect)
        {
            var itemsEnumerator = items.GetEnumerator();
            while (itemsEnumerator.MoveNext())
            {
                var evenValue = evenSelect.Invoke(itemsEnumerator.Current);
                if (!itemsEnumerator.MoveNext())
                    break;
                yield return new KeyValuePair<TEven, TOdd>(evenValue, oddSelect.Invoke(itemsEnumerator.Current));
            }
        }
        private static IEnumerable<Int32> FromByteArrayOffsets(byte[] data)
        {
            if (data == null)
                yield break;

            int index = 0;
            while (index + sizeof(int) <= data.Length && index >= 0) // incase it loads a negative number (should be unsigned anyway)
            {
                yield return index;
                var offset = BitConverter.ToInt32(data, index);
                index += offset + sizeof(Int32);
            }
        }

        public static byte[] ToByteArray<TKey, TValue>(this IDictionary<TKey, TValue> obj, Func<TValue, byte[]> lineConverter)
        {
            var byte1 = BitConverter.GetBytes(obj.Keys.Count);
            var meat = obj.Select((kvp) => lineConverter(kvp.Value));
            var offsets = meat.Select((piece) => piece.Length).Select(pieceLength => BitConverter.GetBytes(pieceLength)).SelectMany(b => b);
            return new byte[][]
            {
                byte1,
                offsets.ToArray(),
                meat.SelectMany(piece => piece).ToArray(),
            }.SelectMany(b => b).ToArray();
        }
        public static byte[] FromByteArray<TKey, TValue>(this IDictionary<TKey, TValue> obj, Func<TValue, byte[]> lineConverter)
        {
            var byte1 = BitConverter.GetBytes(obj.Keys.Count);
            var meat = obj.Select((kvp) => lineConverter(kvp.Value));
            var offsets = meat.Select((piece) => piece.Length).Select(pieceLength => BitConverter.GetBytes(pieceLength)).SelectMany(b => b);
            return new byte[][]
            {
                byte1,
                offsets.ToArray(),
                meat.SelectMany(piece => piece).ToArray(),
            }.SelectMany(b => b).ToArray();
        }

        public static byte[] ToByteArray<TITem>(this IEnumerable<TITem> items, Func<TITem, byte[]> lineConverter)
        {
            if (default(IEnumerable<TITem>) == items)
                return new byte[] { };

            var bytes = items.Select(
                item =>
                {
                    var line = lineConverter(item);
                    return BitConverter.GetBytes(line.Length).Concat(line);
                })
                .SelectMany(b => b)
                .ToArray();
            return bytes;
        }

        public static IEnumerable<TItem> FromByteArray<TItem>(this byte[] bytes, Func<byte[], TItem> lineConverter)
        {
            var index = 0;
            if (default(byte[]) == bytes)
                yield break;
            while (index < bytes.Length && index >= 0)
            {
                var length = BitConverter.ToInt32(bytes, index);
                index += sizeof(Int32);
                var nextBytes = bytes.Skip(index).Take(length).ToArray();
                yield return lineConverter(nextBytes);
                index += length;
            }
        }

        public static Nullable<T>[] ToNullablesFromByteArray<T>(this byte[] byteArrayOfNullables, Func<byte[], T> convert)
            where T : struct
        {
            if (byteArrayOfNullables == null)
                return new T?[] { };

            return byteArrayOfNullables
                .ToNullableEnumerableFromByteArray(convert)
                .ToArray();
        }

        private static IEnumerable<Nullable<T>> ToNullableEnumerableFromByteArray<T>(this byte[] byteArrayOfNullables, Func<byte[], T> convert)
            where T : struct
        {
            int storageLength;
            try
            {
                if (!byteArrayOfNullables.Any())
                    yield break;
                storageLength = BitConverter.ToInt32(byteArrayOfNullables, 0);
            } catch(ArgumentOutOfRangeException)
            {
                yield break;
            }
            if (storageLength <= 0)
                yield break; // TODO: Data warning
            var byteArrayOfNullable = new byte[storageLength];

            var index = sizeof(int); // Read first byte after size array
            while (index < byteArrayOfNullables.Length)
            {
                var nullIndicator = byteArrayOfNullables[index];
                var isNull = nullIndicator == 0;
                index++;
                if (isNull)
                {
                    yield return new Nullable<T>();
                    continue;
                }
                
                if (byteArrayOfNullables.Length < index + storageLength)
                    yield break; // TODO: Data warning
                
                Array.Copy(byteArrayOfNullables, index, byteArrayOfNullable, 0, storageLength);
                yield return convert(byteArrayOfNullable);
                index += storageLength;
            }
        }

        public static byte[] ToByteArrayOfNullables<T>(this IEnumerable<Nullable<T>> nullables, Func<T, byte[]> convert)
            where T : struct
        {
            int size = convert(default(T)).Length;

            var bytes = nullables
                .SelectMany(
                    (nullable) =>
                    {
                        if (nullable.HasValue)
                            return new byte[] { 1 }.Concat(convert(nullable.Value));
                        return new byte[] { 0 };
                    });
            return BitConverter.GetBytes(size).Concat(bytes).ToArray();
        }

        public static byte[] ToByteArray(this IEnumerable<byte[]> items)
        {
            var meat = items.Select((itemBytes) =>
            {
                var bytes = new byte[][]
                {
                    BitConverter.GetBytes(itemBytes.Length),
                    itemBytes,
                };
                return bytes.SelectMany(b => b).ToArray();
            });
            return meat.SelectMany(b => b).ToArray();
        }

        public static IEnumerable<byte[]> FromByteArray(this byte[] data)
        {
            var offsets = FromByteArrayOffsets(data).ToArray();
            var byteLines = offsets.Select(offset =>
                data.Skip(offset + sizeof(Int32)).
                Take(BitConverter.ToInt32(data, offset)).
                ToArray());
            return byteLines;
        }

        internal static List<Guid> GetGuidStorageString(this string storageString)
        {
            var list = GetGuidStorage(storageString);
            return list ?? new List<Guid>();
        }

        public static string SetGuidStorageString(this List<Guid> steps)
        {
            return Encode(steps);
        }

        public static string SetGuidStorageString(this Guid[] steps)
        {
            return SetGuidStorageString(new List<Guid>(steps));
        }

        private static List<Guid> GetGuidStorage(string storage)
        {
            return storage == null ? new List<Guid>() : Decode<List<Guid>>(storage);
        }
        #endregion

        #region Hashes

        public static byte[] SHA256Hash(this byte[] bytes, SHA256 sha256 = default(SHA256))
        {
            byte[] getHash(SHA256 algorithm) => algorithm.ComputeHash(bytes);

            if (default(SHA256) != sha256)
                return getHash(sha256);

            using (var algorithm = SHA256.Create())
                return getHash(algorithm);
        }

        public static Guid MD5HashGuid(this byte[] bytes, MD5 md5 = default(MD5))
        {
            #pragma warning disable SCS0006 // Weak hashing function
            Guid getHashGuid(MD5 algorithm) => new Guid(algorithm.ComputeHash(bytes));

            if (default(MD5) != md5)
                return getHashGuid(md5);

            using (var algorithm = MD5.Create())
                return getHashGuid(algorithm);
            #pragma warning restore SCS0006 // Weak hashing function
        }

        public static Guid MD5HashGuid(this string concatination, MD5 md5 = default(MD5))
        {
            var bytes = concatination.HasBlackSpace() ?
                Encoding.UTF8.GetBytes(concatination)
                :
                new byte[] { };

            return bytes.MD5HashGuid(md5);
        }

        public static Guid MD5HashGuid(this Stream stream, MD5 md5 = default(MD5))
        {
            #pragma warning disable SCS0006 // Weak hashing function
            Guid getHashGuid(MD5 algorithm) => new Guid(algorithm.ComputeHash(stream));

            if (default(MD5) != md5)
                return getHashGuid(md5);

            using (var algorithm = MD5.Create())
                return getHashGuid(algorithm);
            #pragma warning restore SCS0006 // Weak hashing function
        }

        /// <summary>
        /// This method is ideally used to create a unique string.  WARNING: Using this method to protect sensitive information would be a security vulnerability due to it using a weak hashing function.
        /// </summary>
        /// <param name="concatination"></param>
        /// <param name="md5"></param>
        /// <returns></returns>
        public static string MD5HashString(this string concatination, MD5 md5 = default(MD5))
        {
            #pragma warning disable SCS0006 // Weak hashing function
            string getHashString(MD5 algorithm) => Convert.ToBase64String(
                algorithm.ComputeHash(
                    Encoding.UTF8.GetBytes(concatination)));

            if (default(MD5) != md5)
                return getHashString(md5);

            using (var algorithm = MD5.Create())
                return getHashString(algorithm);
            #pragma warning restore SCS0006 // Weak hashing function
        }

        /// <summary>
        /// This method is ideally used to create a unique string.  WARNING: Using this method to protect sensitive information would be a security vulnerability due to it using a weak hashing function.
        /// </summary>
        /// <param name="concatination"></param>
        /// <param name="md5"></param>
        /// <returns></returns>
        public static string MD5HashHex(this string concatination, MD5 md5 = default(MD5))
        {
            #pragma warning disable SCS0006 // Weak hashing function
            string getHashHex(MD5 algorithm) => algorithm
                .ComputeHash(Encoding.UTF8.GetBytes(concatination))
                .Select(b => b.ToString("X2"))
                .Join("");

            if (default(MD5) != md5)
                return getHashHex(md5);

            using (var algorithm = MD5.Create())
                return getHashHex(algorithm);
            #pragma warning restore SCS0006 // Weak hashing function
        }

        public static byte[] SHAHash(this byte[] bytes, SHA256 sha256 = default(SHA256))
        {
            return bytes.SHA256Hash(sha256);
        }

        public static byte[] SHAHash(this string stringToHash,
            System.Text.Encoding encoding = default(System.Text.Encoding),
            SHA256 sha256 = default(SHA256))
        {
            if (default(System.Text.Encoding) == encoding)
                encoding = System.Text.Encoding.UTF8;
            var bytes = stringToHash.HasBlackSpace() ?
                encoding.GetBytes(stringToHash)
                :
                new byte[] { };
            return bytes.SHA256Hash(sha256);
        }

        #endregion

        #region Object Storage String
        internal static List<Guid> GetTDocumentStorageString(this string storageString)
        {
            return GetGuidStorage(storageString);
        }

        public static string SetTDocumentStorageString<TDocument>(List<TDocument> steps)
        {
            return Encode(steps);
        }

        private static List<TDocument> GetTDocumentStorage<TDocument>(string storage)
        {
            return storage == null ? new List<TDocument>() : Decode<List<TDocument>>(storage);
        }
        #endregion 


        //abstract class AtomicEntity<TKey, TDocument> : IDisposable
        //where TDocument : TableEntity, IDocument

        internal static T Decode<T>(string value)
        {
            value = value.Replace("-", "");
            if (String.IsNullOrWhiteSpace(value))
            {
                return default(T);
            }
            try
            {
                var settings = new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Ignore, // prevents XXE attacks, such as Billion Laughs
                    MaxCharactersFromEntities = 1024,
                    XmlResolver = null,                   // prevents external entity DoS attacks, such as slow loading links or large file requests
                };

                using (var strReader = new StringReader(value))
                using (var xmlReader = XmlReader.Create(strReader, settings))
                {
                    var serializer = new DataContractSerializer(typeof(T));
                    T result = (T)serializer.ReadObject(xmlReader);
                    return result;
                }
            }
            catch (SerializationException)
            {
                return default(T);
            }
        }

        internal static string Encode<T>(T value)
        {
            if (EqualityComparer<T>.Default.Equals(value))
            {
                return String.Empty;
            }
            string serializedString;
            using (MemoryStream memoryStream = new MemoryStream())
            using (StreamReader reader = new StreamReader(memoryStream))
            {
                DataContractSerializer serializer = new DataContractSerializer(typeof(T));
                serializer.WriteObject(memoryStream, value);
                memoryStream.Position = 0;
                serializedString = reader.ReadToEnd();
            }
            return serializedString;
        }

        public static Guid ComposeGuid(this Guid guid1, Guid guid2)
        {
            var id = guid1.ToByteArray().Concat(guid2.ToByteArray()).ToArray().MD5HashGuid();
            return id;
        }

        public static Guid ComposeGuid(this Guid guid1, string textKey)
        {
            var id = guid1
                .ToByteArray()
                .Concat(
                    Encoding.UTF8.GetBytes(textKey))
                .ToArray()
                .MD5HashGuid();
            return id;
        }

        public static Guid ComposeGuid(this Guid guid1, int intKey)
        {
            var id = guid1
                .ToByteArray()
                .Concat(BitConverter.GetBytes(intKey))
                .ToArray()
                .MD5HashGuid();
            return id;
        }

        public static Guid ComposeGuid(this Guid guid, DateTime dateTime, TimeSpanUnits? fidelityMaybe =default)
        {
            var id = guid
                .ToByteArray()
                .Concat(GetDateBytes())
                .ToArray()
                .MD5HashGuid();
            return id;

            byte[] GetDateBytes()
            {
                if (!fidelityMaybe.HasValue)
                    return BitConverter.GetBytes(dateTime.Ticks);

                var fidelity = fidelityMaybe.Value;
                if(fidelity == TimeSpanUnits.continuous)
                    return BitConverter.GetBytes(dateTime.Ticks);

                var bytes = BitConverter.GetBytes(dateTime.Year);
                if (fidelity == TimeSpanUnits.years)
                    return bytes;

                if (fidelity == TimeSpanUnits.months)
                    return bytes.Concat(BitConverter.GetBytes(dateTime.Month)).ToArray();

                if (fidelity == TimeSpanUnits.weeks)
                {
                    var timeSinceEpoch = dateTime - new DateTime(1, 1, 1);
                    var weeks = timeSinceEpoch.Days / 7;
                    return bytes.Concat(BitConverter.GetBytes(weeks)).ToArray();
                }

                bytes = bytes.Concat(BitConverter.GetBytes(dateTime.DayOfYear)).ToArray();
                if (fidelity == TimeSpanUnits.days)
                    return bytes;

                bytes = bytes.Concat(BitConverter.GetBytes(dateTime.Hour)).ToArray();
                if (fidelity == TimeSpanUnits.hours)
                    return bytes;

                bytes = bytes.Concat(BitConverter.GetBytes(dateTime.Minute)).ToArray();
                if (fidelity == TimeSpanUnits.minutes)
                    return bytes;

                bytes = bytes.Concat(BitConverter.GetBytes(dateTime.Second)).ToArray();
                if (fidelity == TimeSpanUnits.seconds)
                    return bytes;

                return bytes;
            }
        }

        public static Guid ComposeGuid(this Guid guid1, Guid guid2, int intKey1)
        {
            var id = guid1
                .ToByteArray()
                .Concat(guid2.ToByteArray())
                .Concat(BitConverter.GetBytes(intKey1))
                .ToArray()
                .MD5HashGuid();
            return id;
        }


    }
}
