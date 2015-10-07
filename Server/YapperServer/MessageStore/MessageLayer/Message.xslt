<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="2.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output method="text"/>
  <xsl:template match="/">
    <xsl:variable name="tableName" select="Table/@TableName" as="xs:string"/>
    <xsl:variable name="blobexists" select="boolean(false)" as="xs:boolean"/>
    <xsl:text>
using System; 
using System.Collections.Generic;
using System.Runtime.Serialization;

using MessageStore.Database;
using ProtoBuf;

namespace MessageStore.MessageLayer
{
      [YapperTable(Name="</xsl:text><xsl:value-of select="$tableName"/><xsl:text>Table")]
      [DataContract]
      [ProtoContract]
      public partial class </xsl:text><xsl:value-of select="$tableName"/><xsl:text>
      {</xsl:text>
    <xsl:for-each select="Table/Column">
      <xsl:variable name="blobcolumn" select="Server/ColumnLocation = 'BlobStore'" as="xs:boolean"/>
      <xsl:variable name="dbcolumn" select="Server/ColumnLocation = 'Database'" as="xs:boolean"/>
      <xsl:variable name="identity" select="Server/Identity = 'true'" as="xs:boolean"/>

        <xsl:if test="$dbcolumn = boolean('true')">
           [YapperColumn(Name="<xsl:value-of select="Name"/>"<xsl:if test="$identity = boolean('true')">, Identity=<xsl:value-of select="Identity"/></xsl:if>, Type=typeof(<xsl:value-of select="Type"/>), ColumnLocation=ColumnLocation.Database)]</xsl:if>
        <xsl:variable name="datacontract" select="Client/SentToClient = 'true'" as="xs:boolean"/>
        <xsl:if test="$datacontract = boolean('true')">
           [DataMember]</xsl:if>
        <xsl:variable name="shouldserializeinblob" select="Server/ShouldSerializeInBlob = 'true'" as="xs:boolean"/>
        <xsl:if test="$blobcolumn = boolean('true')">
           [ProtoMember(<xsl:value-of select="ColumnId"/><xsl:text>, IsRequired = </xsl:text><xsl:value-of select="Required"/>)]</xsl:if>
           public<xsl:text> </xsl:text><xsl:value-of select="Type"/><xsl:text> </xsl:text><xsl:value-of select="Name"/>
           {
               get;
               set;
           }
    </xsl:for-each>
    <xsl:call-template name="blobTemplate" >
      <xsl:with-param name="tableName" select="Table/@TableName"/>
    </xsl:call-template>
    <xsl:text>
      }
}
</xsl:text>
  </xsl:template>

  <xsl:template name="blobTemplate" match="Table/Column/Server[@ColumnLocation='BlobStore']">
    <xsl:param name="tableName"/>
            [YapperColumn(Name="<xsl:value-of select="$tableName"/>BlobName", Type=typeof(Guid), ColumnLocation=ColumnLocation.Database, IsBlobName = true)]
            public<xsl:text> Guid </xsl:text><xsl:value-of select="Type"/><xsl:text> </xsl:text><xsl:value-of select="$tableName"/>BlobName
            {
                get;
                set;
            }

            [YapperColumn(Name="<xsl:value-of select="$tableName"/>BlobValue", Type=typeof(byte[]), ColumnLocation=ColumnLocation.BlobStore)]
            public<xsl:text> byte[] </xsl:text><xsl:text> </xsl:text><xsl:value-of select="$tableName"/><xsl:text>BlobValue</xsl:text>
            {
                get
                {
                    return this.Get<xsl:value-of select="$tableName"/>BlobValue();
                }

                set
                {
                    this.Parse<xsl:value-of select="$tableName" />BlobValue(value);
                }
            }
  </xsl:template>
</xsl:stylesheet>
