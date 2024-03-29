#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
/*
 * Autogenerated by Thrift Compiler (0.10.0)
 *
 * DO NOT EDIT UNLESS YOU ARE SURE THAT YOU KNOW WHAT YOU ARE DOING
 *  @generated
 */
using System.Collections.Generic;
using System.Text;
using Thrift.Protocol;

namespace Parquet.Thrift
{

   public class RowGroup : TBase
   {
      private List<SortingColumn> _sorting_columns;

      public List<ColumnChunk> Columns { get; set; }

      /// <summary>
      /// Total byte size of all the uncompressed column data in this row group *
      /// </summary>
      public long Total_byte_size { get; set; }

      /// <summary>
      /// Number of rows in this row group *
      /// </summary>
      public long Num_rows { get; set; }

      /// <summary>
      /// If set, specifies a sort ordering of the rows in this RowGroup.
      /// The sorting columns can be a subset of all the columns.
      /// </summary>
      public List<SortingColumn> Sorting_columns
      {
         get
         {
            return _sorting_columns;
         }
         set
         {
            __isset.sorting_columns = true;
            this._sorting_columns = value;
         }
      }


      public Isset __isset;



      public struct Isset
      {
         public bool sorting_columns;
      }

      public RowGroup()
      {
      }

      public RowGroup(List<ColumnChunk> columns, long total_byte_size, long num_rows) : this()
      {
         this.Columns = columns;
         this.Total_byte_size = total_byte_size;
         this.Num_rows = num_rows;
      }

      public void Read(TProtocol iprot)
      {
         iprot.IncrementRecursionDepth();
         try
         {
            bool isset_columns = false;
            bool isset_total_byte_size = false;
            bool isset_num_rows = false;
            TField field;
            iprot.ReadStructBegin();
            while (true)
            {
               field = iprot.ReadFieldBegin();
               if (field.Type == TType.Stop)
               {
                  break;
               }
               switch (field.ID)
               {
                  case 1:
                     if (field.Type == TType.List)
                     {
                        {
                           Columns = new List<ColumnChunk>();
                           TList _list16 = iprot.ReadListBegin();
                           for (int _i17 = 0; _i17 < _list16.Count; ++_i17)
                           {
                              ColumnChunk _elem18;
                              _elem18 = new ColumnChunk();
                              _elem18.Read(iprot);
                              Columns.Add(_elem18);
                           }
                           iprot.ReadListEnd();
                        }
                        isset_columns = true;
                     }
                     else
                     {
                        TProtocolUtil.Skip(iprot, field.Type);
                     }
                     break;
                  case 2:
                     if (field.Type == TType.I64)
                     {
                        Total_byte_size = iprot.ReadI64();
                        isset_total_byte_size = true;
                     }
                     else
                     {
                        TProtocolUtil.Skip(iprot, field.Type);
                     }
                     break;
                  case 3:
                     if (field.Type == TType.I64)
                     {
                        Num_rows = iprot.ReadI64();
                        isset_num_rows = true;
                     }
                     else
                     {
                        TProtocolUtil.Skip(iprot, field.Type);
                     }
                     break;
                  case 4:
                     if (field.Type == TType.List)
                     {
                        {
                           Sorting_columns = new List<SortingColumn>();
                           TList _list19 = iprot.ReadListBegin();
                           for (int _i20 = 0; _i20 < _list19.Count; ++_i20)
                           {
                              SortingColumn _elem21;
                              _elem21 = new SortingColumn();
                              _elem21.Read(iprot);
                              Sorting_columns.Add(_elem21);
                           }
                           iprot.ReadListEnd();
                        }
                     }
                     else
                     {
                        TProtocolUtil.Skip(iprot, field.Type);
                     }
                     break;
                  default:
                     TProtocolUtil.Skip(iprot, field.Type);
                     break;
               }
               iprot.ReadFieldEnd();
            }
            iprot.ReadStructEnd();
            if (!isset_columns)
               throw new TProtocolException(TProtocolException.INVALID_DATA);
            if (!isset_total_byte_size)
               throw new TProtocolException(TProtocolException.INVALID_DATA);
            if (!isset_num_rows)
               throw new TProtocolException(TProtocolException.INVALID_DATA);
         }
         finally
         {
            iprot.DecrementRecursionDepth();
         }
      }

      public void Write(TProtocol oprot)
      {
         oprot.IncrementRecursionDepth();
         try
         {
            TStruct struc = new TStruct("RowGroup");
            oprot.WriteStructBegin(struc);
            TField field = new TField();
            field.Name = "columns";
            field.Type = TType.List;
            field.ID = 1;
            oprot.WriteFieldBegin(field);
            {
               oprot.WriteListBegin(new TList(TType.Struct, Columns.Count));
               foreach (ColumnChunk _iter22 in Columns)
               {
                  _iter22.Write(oprot);
               }
               oprot.WriteListEnd();
            }
            oprot.WriteFieldEnd();
            field.Name = "total_byte_size";
            field.Type = TType.I64;
            field.ID = 2;
            oprot.WriteFieldBegin(field);
            oprot.WriteI64(Total_byte_size);
            oprot.WriteFieldEnd();
            field.Name = "num_rows";
            field.Type = TType.I64;
            field.ID = 3;
            oprot.WriteFieldBegin(field);
            oprot.WriteI64(Num_rows);
            oprot.WriteFieldEnd();
            if (Sorting_columns != null && __isset.sorting_columns)
            {
               field.Name = "sorting_columns";
               field.Type = TType.List;
               field.ID = 4;
               oprot.WriteFieldBegin(field);
               {
                  oprot.WriteListBegin(new TList(TType.Struct, Sorting_columns.Count));
                  foreach (SortingColumn _iter23 in Sorting_columns)
                  {
                     _iter23.Write(oprot);
                  }
                  oprot.WriteListEnd();
               }
               oprot.WriteFieldEnd();
            }
            oprot.WriteFieldStop();
            oprot.WriteStructEnd();
         }
         finally
         {
            oprot.DecrementRecursionDepth();
         }
      }

      public override string ToString()
      {
         StringBuilder __sb = new StringBuilder("RowGroup(");
         __sb.Append(", Columns: ");
         __sb.Append(Columns);
         __sb.Append(", Total_byte_size: ");
         __sb.Append(Total_byte_size);
         __sb.Append(", Num_rows: ");
         __sb.Append(Num_rows);
         if (Sorting_columns != null && __isset.sorting_columns)
         {
            __sb.Append(", Sorting_columns: ");
            __sb.Append(Sorting_columns);
         }
         __sb.Append(")");
         return __sb.ToString();
      }

   }

}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member