<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="2.0" 
                xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output method="text"/>
 
  <xsl:template match="/">
    <xsl:variable name="tableName" select="Table/@TableName" as="xs:string"/>
    <xsl:variable name="blobexists" select="boolean(false)" as="xs:boolean"/>
    <xsl:text>
using System; 
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

using ProtoBuf;
using YapperChat.Common;
using YapperChat.Sync;

namespace YapperChat.Models
{
    [Table]
    [DataContract(Name = "Message", Namespace = "http://schemas.datacontract.org/2004/07/Yapper")]
    [ProtoContract]
    public partial class </xsl:text>
    <xsl:value-of select="$tableName"/>
    <xsl:text>Model
    {</xsl:text>
    <xsl:for-each select="Table/Column">
      <xsl:variable name="datacontract" select="Client/SentToClient = 'true'" as="xs:boolean"/>
      <xsl:variable name="clientprimarykey" select="Client/ClientPrimaryKey = 'true'" as="xs:boolean"/>
      <xsl:variable name="storedonclient" select="Client/StoredOnClient = 'true'" as="xs:boolean"/>
      <xsl:variable name="generateonclient" select="Client/GenerateOnClient = 'true'" as="xs:boolean"/>
      <xsl:variable name="name" select="Name" as="xs:string"/>
      <xsl:variable name="canbenull" select="Required = 'false'" as="xs:boolean"/>
      
      <xsl:if test="$datacontract = boolean('true') and ($generateonclient = boolean('true') or not(Client/GenerateOnClient))">
        private<xsl:text> </xsl:text><xsl:value-of select="Type"/><xsl:text> _</xsl:text><xsl:value-of select="$name"/>;
        <xsl:text></xsl:text>
        [DataMember]
        [ProtoMember(<xsl:value-of select="ColumnId"/><xsl:text>, IsRequired = </xsl:text><xsl:value-of select="Required"/>)]<xsl:if test="$storedonclient = boolean('true')"><xsl:text>
        [Column(IsPrimaryKey = </xsl:text><xsl:value-of select="$clientprimarykey"/><xsl:text>, CanBeNull = </xsl:text><xsl:value-of select="$canbenull"/>)]</xsl:if>
        public<xsl:text> </xsl:text><xsl:value-of select="Type"/><xsl:text> </xsl:text><xsl:value-of select="Name"/>
        <xsl:text>
        {
            get
            {
                return _</xsl:text><xsl:value-of select="$name"/><xsl:text>;
            }
            
            set
            {
                this._</xsl:text><xsl:value-of select="$name"/><xsl:text> = value;
                this.NotifyPropertyChanged();
            }
        }
        </xsl:text>
      </xsl:if>
    </xsl:for-each>
    <xsl:text>
        public MessageModel CloneInternal()
        {
            MessageModel m = new MessageModel();
            </xsl:text>
    <xsl:for-each select="Table/Column">
      <xsl:variable name="name" select="Name" as="xs:string"/>
               <xsl:text>m.</xsl:text><xsl:value-of select="$name"/><xsl:text> = this.</xsl:text><xsl:value-of select="$name"/><xsl:text>;
            </xsl:text>
    </xsl:for-each>
    <xsl:text>
            return m;
         }
    }
}
      </xsl:text>
  </xsl:template>
    </xsl:stylesheet>
