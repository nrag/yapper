/**
 * Autogenerated by Thrift Compiler (0.9.1)
 *
 * DO NOT EDIT UNLESS YOU ARE SURE THAT YOU KNOW WHAT YOU ARE DOING
 *  @generated
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Thrift;
using Thrift.Collections;
using System.Runtime.Serialization;
using Thrift.Protocol;
using Thrift.Transport;

namespace UserServiceRole
{

  #if !SILVERLIGHT
  [Serializable]
  #endif
  public partial class User : TBase
  {
    private UserData _UserData;
    private GroupData _GroupData;

    public UserData UserData
    {
      get
      {
        return _UserData;
      }
      set
      {
        __isset.UserData = true;
        this._UserData = value;
      }
    }

    public GroupData GroupData
    {
      get
      {
        return _GroupData;
      }
      set
      {
        __isset.GroupData = true;
        this._GroupData = value;
      }
    }


    public Isset __isset;
    #if !SILVERLIGHT
    [Serializable]
    #endif
    public struct Isset {
      public bool UserData;
      public bool GroupData;
    }

    public User() {
    }

    public void Read (TProtocol iprot)
    {
      TField field;
      iprot.ReadStructBegin();
      while (true)
      {
        field = iprot.ReadFieldBegin();
        if (field.Type == TType.Stop) { 
          break;
        }
        switch (field.ID)
        {
          case 1:
            if (field.Type == TType.Struct) {
              UserData = new UserData();
              UserData.Read(iprot);
            } else { 
              TProtocolUtil.Skip(iprot, field.Type);
            }
            break;
          case 2:
            if (field.Type == TType.Struct) {
              GroupData = new GroupData();
              GroupData.Read(iprot);
            } else { 
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
    }

    public void Write(TProtocol oprot) {
      TStruct struc = new TStruct("User");
      oprot.WriteStructBegin(struc);
      TField field = new TField();
      if (UserData != null && __isset.UserData) {
        field.Name = "UserData";
        field.Type = TType.Struct;
        field.ID = 1;
        oprot.WriteFieldBegin(field);
        UserData.Write(oprot);
        oprot.WriteFieldEnd();
      }
      if (GroupData != null && __isset.GroupData) {
        field.Name = "GroupData";
        field.Type = TType.Struct;
        field.ID = 2;
        oprot.WriteFieldBegin(field);
        GroupData.Write(oprot);
        oprot.WriteFieldEnd();
      }
      oprot.WriteFieldStop();
      oprot.WriteStructEnd();
    }

    public override string ToString() {
      StringBuilder sb = new StringBuilder("User(");
      sb.Append("UserData: ");
      sb.Append(UserData== null ? "<null>" : UserData.ToString());
      sb.Append(",GroupData: ");
      sb.Append(GroupData== null ? "<null>" : GroupData.ToString());
      sb.Append(")");
      return sb.ToString();
    }

  }

}
