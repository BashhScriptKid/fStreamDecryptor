using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;

namespace StreamFormatDecryptor;

public class FileInfoStruct
{
    internal enum ObjType : byte
    {
        nullType,
        boolType,
        byteType,
        uint16Type,
        uint32Type,
        uint64Type,
        sbyteType,
        int16Type,
        int32Type,
        int64Type,
        charType,
        stringType,
        singleType,
        doubleType,
        decimalType,
        dateTimeType,
        byteArrayType,
        charArrayType,
        otherType,
        bSerializableType
    }
    
    public struct FileInfos : bSerializable
    {
        public string Filename { get; private set; }
        public int Length { get; private set; }
        public int Offset { get; private set; }
        public byte[ /*16*/] Hash { get; private set; }
        public DateTime CreationTime { get; private set; }
        public DateTime ModifiedTime { get; private set; }


        public FileInfos(string filename, int offset, int length, byte[] hash, DateTime creationTime,
            DateTime modifiedTime) : this()
        {
            Filename = filename;
            Offset = offset;
            Length = length;
            Hash = hash;
            CreationTime = creationTime;
            ModifiedTime = modifiedTime;
        }
        
        public void ReadFromStream(SerializationReader sr)
        {
            Filename = sr.ReadString();
            Length = sr.ReadInt32();
            Offset = sr.ReadInt32();
            Hash = sr.ReadByteArray();
            CreationTime = (DateTime)sr.ReadObject();
            ModifiedTime = (DateTime)sr.ReadObject();
        }

        public void WriteToStream(SerializationWriter sw)
        {
            sw.Write(Filename);
            sw.Write(Length);
            sw.Write(Offset);
            sw.WriteByteArray(Hash);
            sw.WriteObject(CreationTime);
            sw.WriteObject(ModifiedTime);
        }
    }

    #region Serialisable

    #region Reader

        public class SerializationReader : BinaryReader
    {
        public SerializationReader(Stream s)
            : base(s)
        {
        }

        /// <summary> Static method to take a SerializationInfo object (an input to an ISerializable constructor)
        /// and produce a SerializationReader from which serialized objects can be read </summary>.
        public static SerializationReader GetReader(SerializationInfo info)
        {
            byte[] byteArray = (byte[])info.GetValue("X", typeof(byte[]));
            MemoryStream ms = new MemoryStream(byteArray);
            return new SerializationReader(ms);
        }

        /// <summary> Reads a string from the buffer.  Overrides the base implementation so it can cope with nulls. </summary>
        public override string ReadString()
        {
            if (0 == ReadByte()) return null;

            return base.ReadString();
        }

        /// <summary> Reads a byte array from the buffer, handling nulls and the array length. </summary>
        public byte[] ReadByteArray()
        {
            int len = ReadInt32();
            if (len > 0) return ReadBytes(len);
            if (len < 0) return null;

            return new byte[0];
        }

        /// <summary> Reads a char array from the buffer, handling nulls and the array length. </summary>
        public char[] ReadCharArray()
        {
            int len = ReadInt32();
            if (len > 0) return ReadChars(len);
            if (len < 0) return null;

            return new char[0];
        }

        /// <summary> Reads a DateTime from the buffer. </summary>
        public DateTime ReadDateTime()
        {
            long ticks = ReadInt64();
            if (ticks < 0) throw new AbandonedMutexException("oops");

            return new DateTime(ticks, DateTimeKind.Utc);
        }

        /// <summary> Reads a generic list from the buffer. </summary>
        public pList<T> ReadBList<T>() where T : bSerializable, IComparable<T>, new()
        {
            int count = ReadInt32();
            if (count < 0) return null;

            pList<T> d = new pList<T>(count);

            SerializationReader sr = new SerializationReader(BaseStream);

            for (int i = 0; i < count; i++)
            {
                T obj = new T();
                obj.ReadFromStream(sr);
                d.Add(obj);
            }

            return d;
        }

        /// <summary> Reads a generic list from the buffer. </summary>
        public List<T> ReadList<T>()
        {
            int count = ReadInt32();
            if (count < 0) return null;

            List<T> d = new List<T>(count);
            for (int i = 0; i < count; i++) d.Add((T)ReadObject());
            return d;
        }

        /// <summary> Reads a generic Dictionary from the buffer. </summary>
        public IDictionary<T, U> ReadDictionary<T, U>()
        {
            int count = ReadInt32();
            if (count < 0) return null;

            IDictionary<T, U> d = new Dictionary<T, U>();
            for (int i = 0; i < count; i++) d[(T)ReadObject()] = (U)ReadObject();
            return d;
        }

        /// <summary> Reads an object which was added to the buffer by WriteObject. </summary>
        public object ReadObject()
        {
            ObjType t = (ObjType)ReadByte();
            switch (t)
            {
                case ObjType.boolType:
                    return ReadBoolean();
                case ObjType.byteType:
                    return ReadByte();
                case ObjType.uint16Type:
                    return ReadUInt16();
                case ObjType.uint32Type:
                    return ReadUInt32();
                case ObjType.uint64Type:
                    return ReadUInt64();
                case ObjType.sbyteType:
                    return ReadSByte();
                case ObjType.int16Type:
                    return ReadInt16();
                case ObjType.int32Type:
                    return ReadInt32();
                case ObjType.int64Type:
                    return ReadInt64();
                case ObjType.charType:
                    return ReadChar();
                case ObjType.stringType:
                    return base.ReadString();
                case ObjType.singleType:
                    return ReadSingle();
                case ObjType.doubleType:
                    return ReadDouble();
                case ObjType.decimalType:
                    return ReadDecimal();
                case ObjType.dateTimeType:
                    return ReadDateTime();
                case ObjType.byteArrayType:
                    return ReadByteArray();
                case ObjType.charArrayType:
                    return ReadCharArray();
                case ObjType.otherType:
                    return DynamicDeserializer.Deserialize(BaseStream);
                default:
                    return null;
            }
        }
    }

    #endregion

    #region Writer

        public class SerializationWriter : BinaryWriter
    {
        public SerializationWriter(Stream s)
            : base(s)
        {
        }

        /// <summary> Static method to initialise the writer with a suitable MemoryStream. </summary>
        public static SerializationWriter GetWriter()
        {
            MemoryStream ms = new MemoryStream(1024);
            return new SerializationWriter(ms);
        }

        /// <summary> Writes a string to the buffer.  Overrides the base implementation so it can cope with nulls </summary>
        public override void Write(string str)
        {
            if (str == null)
            {
                Write((byte)ObjType.nullType);
            }
            else
            {
                Write((byte)ObjType.stringType);
                base.Write(str);
            }
        }

        /// <summary> Writes a byte array to the buffer.  Overrides the base implementation to
        /// send the length of the array which is needed when it is retrieved </summary>
        public override void Write(byte[] b)
        {
            if (b == null)
            {
                Write(-1);
            }
            else
            {
                int len = b.Length;
                Write(len);
                if (len > 0) base.Write(b);
            }
        }

        /// <summary> Writes a char array to the buffer.  Overrides the base implementation to
        /// sends the length of the array which is needed when it is read. </summary>
        public override void Write(char[] c)
        {
            if (c == null)
            {
                Write(-1);
            }
            else
            {
                int len = c.Length;
                Write(len);
                if (len > 0) base.Write(c);
            }
        }

        /// <summary> Writes a DateTime to the buffer. </summary>
        public void Write(DateTime dt)
        {
            Write(dt.ToUniversalTime().Ticks);
        }

        /// <summary> Writes a generic ICollection (such as an IList<T>) to the buffer. </summary>
        public void Write<T>(List<T> c) where T : bSerializable
        {
            SerializationWriter sw = new SerializationWriter(BaseStream);

            if (c == null)
            {
                Write(-1);
            }
            else
            {
                int count = c.Count;
                Write(count);
                for (int i = 0; i < count; i++)
                    c[i].WriteToStream(sw);
            }
        }

        /// <summary> Writes a generic IDictionary to the buffer. </summary>
        public void Write<T, U>(IDictionary<T, U> d)
        {
            if (d == null)
            {
                Write(-1);
            }
            else
            {
                Write(d.Count);
                foreach (KeyValuePair<T, U> kvp in d)
                {
                    WriteObject(kvp.Key);
                    WriteObject(kvp.Value);
                }
            }
        }

        /// <summary> Writes an arbitrary object to the buffer.  Useful where we have something of type "object"
        /// and don't know how to treat it.  This works out the best method to use to write to the buffer. </summary>
        [Obsolete("Obsolete")]
        public void WriteObject(object obj)
        {
            if (obj == null)
            {
                Write((byte)ObjType.nullType);
            }
            else
            {
                switch (obj.GetType().Name)
                {
                    case "Boolean":
                        Write((byte)ObjType.boolType);
                        Write((bool)obj);
                        break;

                    case "Byte":
                        Write((byte)ObjType.byteType);
                        Write((byte)obj);
                        break;

                    case "UInt16":
                        Write((byte)ObjType.uint16Type);
                        Write((ushort)obj);
                        break;

                    case "UInt32":
                        Write((byte)ObjType.uint32Type);
                        Write((uint)obj);
                        break;

                    case "UInt64":
                        Write((byte)ObjType.uint64Type);
                        Write((ulong)obj);
                        break;

                    case "SByte":
                        Write((byte)ObjType.sbyteType);
                        Write((sbyte)obj);
                        break;

                    case "Int16":
                        Write((byte)ObjType.int16Type);
                        Write((short)obj);
                        break;

                    case "Int32":
                        Write((byte)ObjType.int32Type);
                        Write((int)obj);
                        break;

                    case "Int64":
                        Write((byte)ObjType.int64Type);
                        Write((long)obj);
                        break;

                    case "Char":
                        Write((byte)ObjType.charType);
                        base.Write((char)obj);
                        break;

                    case "String":
                        Write((byte)ObjType.stringType);
                        base.Write((string)obj);
                        break;

                    case "Single":
                        Write((byte)ObjType.singleType);
                        Write((float)obj);
                        break;

                    case "Double":
                        Write((byte)ObjType.doubleType);
                        Write((double)obj);
                        break;

                    case "Decimal":
                        Write((byte)ObjType.decimalType);
                        Write((decimal)obj);
                        break;

                    case "DateTime":
                        Write((byte)ObjType.dateTimeType);
                        Write((DateTime)obj);
                        break;

                    case "Byte[]":
                        Write((byte)ObjType.byteArrayType);
                        base.Write((byte[])obj);
                        break;

                    case "Char[]":
                        Write((byte)ObjType.charArrayType);
                        base.Write((char[])obj);
                        break;

                    default:
                        Write((byte)ObjType.otherType);
                        BinaryFormatter b = new BinaryFormatter
                        {
                            AssemblyFormat = FormatterAssemblyStyle.Simple,
                            TypeFormat = FormatterTypeStyle.TypesWhenNeeded
                        };
                        b.Serialize(BaseStream, obj);
                        break;
                } // switch
            } // if obj==null
        } // WriteObject

        /// <summary> Adds the SerializationWriter buffer to the SerializationInfo at the end of GetObjectData(). </summary>
        public void AddToInfo(SerializationInfo info)
        {
            byte[] b = ((MemoryStream)BaseStream).ToArray();
            info.AddValue("X", b, typeof(byte[]));
        }

        public void WriteByteArray(byte[] b)
        {
            if (b == null)
            {
                Write(-1);
            }
            else
            {
                int len = b.Length;
                Write(len);
                if (len > 0) base.Write(b);
            }
        }
    }

    #endregion

    #endregion
    
    public interface bSerializable
    {
        void ReadFromStream(SerializationReader sr);
        void WriteToStream(SerializationWriter sw);
    }

    #region Dynamic Serialiser

        [Obsolete("Obsolete")]
        public class DynamicDeserializer
    {
        private static VersionConfigToNamespaceAssemblyObjectBinder versionBinder;
        private static BinaryFormatter formatter;


        private static void Initialize()
        {
            versionBinder = new VersionConfigToNamespaceAssemblyObjectBinder();
            formatter = new BinaryFormatter
            {
                AssemblyFormat = FormatterAssemblyStyle.Simple,
                Binder = versionBinder
            };
        }

        public static object Deserialize(Stream stream)
        {
            if (formatter == null)
                Initialize();
            return formatter.Deserialize(stream);
        }

        #region Nested type: VersionConfigToNamespaceAssemblyObjectBinder

        internal sealed class VersionConfigToNamespaceAssemblyObjectBinder : SerializationBinder
        {
            private readonly Dictionary<string, Type> cache = new Dictionary<string, Type>();

            public override Type BindToType(string assemblyName, string typeName)
            {
                Type typeToDeserialize;

                if (cache.TryGetValue(assemblyName + typeName, out typeToDeserialize))
                    return typeToDeserialize;

                List<Type> tmpTypes = new List<Type>();
                Type genType = null;

                try
                {
                    if (typeName.Contains("System.Collections.Generic") && typeName.Contains("[["))
                    {
                        string[] splitTyps = typeName.Split('[');

                        foreach (string typ in splitTyps)
                        {
                            if (typ.Contains("Version"))
                            {
                                string asmTmp = typ.Substring(typ.IndexOf(',') + 1);
                                string asmName = asmTmp.Remove(asmTmp.IndexOf(']')).Trim();
                                string typName = typ.Remove(typ.IndexOf(','));
                                tmpTypes.Add(BindToType(asmName, typName));
                            }
                            else if (typ.Contains("Generic"))
                            {
                                genType = BindToType(assemblyName, typ);
                            }
                        }

                        if (genType != null && tmpTypes.Count > 0)
                        {
                            return genType.MakeGenericType(tmpTypes.ToArray());
                        }
                    }

                    string ToAssemblyName = assemblyName.Split(',')[0];
                    Assembly[] Assemblies = AppDomain.CurrentDomain.GetAssemblies();
                    foreach (Assembly a in Assemblies)
                    {
                        if (a.FullName.Split(',')[0] == ToAssemblyName)
                        {
                            typeToDeserialize = a.GetType(typeName);
                            break;
                        }
                    }
                }
                catch (Exception exception)
                {
                    throw exception;
                }

                cache.Add(assemblyName + typeName, typeToDeserialize);

                return typeToDeserialize;
            }
        }

        #endregion
    }

    #endregion

    #region pList

    public class pList<T> : List<T> where T : IComparable<T>
    {
        private readonly bool forceSortOnAdd;
        internal bool UseBackwardsSearch;
        internal bool InsertAfterOnEqual;
        private readonly IComparer<T> comparer;

        public pList()
        {
        }

        public pList(int size)
            : base(size)
        {
        }

        public pList(IComparer<T> comparer, bool forceSortOnAdd)
        {
            this.comparer = comparer;
            this.forceSortOnAdd = forceSortOnAdd;
        }

        public new void Add(T item)
        {
            if (forceSortOnAdd)
                AddInPlace(item);
            else
                base.Add(item);
        }

        public int AddInPlace(T item)
        {
            return AddInPlace(item, UseBackwardsSearch);
        }

        public int AddInPlace(T item, bool useBackwardsSearch)
        {
            int index = -1;

            if (useBackwardsSearch)
            {
                int count = Count;
                if (count == 0)
                {
                    base.Add(item);
                    index = 0;
                }
                else
                {
                    for (index = count - 1; index >= 0; index--)
                    {
                        int compare = base[index].CompareTo(item);
                        if (compare > 0) continue;

                        Insert((compare < 0 || InsertAfterOnEqual) ? ++index : index, item);
                        return index;
                    }

                    Insert(0, item);
                    index = 0;
                }
            }
            else
            {
                index = comparer != null ? BinarySearch(item, comparer) : BinarySearch(item);
                index = index < 0 ? ~index : (InsertAfterOnEqual ? index + 1 : index);
                Insert(index, item);
            }

            return index;
        }
    }

    #endregion
}