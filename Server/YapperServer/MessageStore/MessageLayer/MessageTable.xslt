<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="2.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output method="text"/>
  <xsl:template match="/">
    <xsl:variable name="tableName" select="concat(Table/@TableName, 'Table')" as="xs:string"/>
    <xsl:text>
using System; 
using System.Collections.Generic;
using System.Runtime.Serialization;

using MessageStore.Database;

namespace MessageStore.MessageLayer
{
      partial class </xsl:text> <xsl:value-of select="$tableName"/><xsl:text> : DatabaseTable
      {
          private static List&lt;IColumn&gt; </xsl:text><xsl:value-of select="$tableName"/><xsl:text>Columns;

          private static </xsl:text> <xsl:value-of select="$tableName"/><xsl:text> _instance = new </xsl:text> <xsl:value-of select="$tableName"/><xsl:text>();

          private IColumn blobNameColumn;

          private IColumn blobValueColumn;

          private IColumn identityColumn;

    </xsl:text>
    <xsl:for-each select="Table/Column">
      <xsl:variable name="blobcolumn" select="Server/ColumnLocation = 'BlobStore'" as="xs:boolean"/>
      <xsl:variable name="dbcolumn" select="Server/ColumnLocation = 'Database'" as="xs:boolean"/>
      <xsl:variable name="blobContainer" select="Server/BlobContainer"/>
      <xsl:variable name="blobName" select="Server/BlobName"/>
      <xsl:variable name="identity" select="Server/Identity = 'true'" as="xs:boolean"/>

      <xsl:if test="$dbcolumn = boolean('true')">
        <xsl:text>
          public static IColumn </xsl:text><xsl:value-of select="Name"/><xsl:text>Column = new DatabaseColumn(){</xsl:text>
              Name="<xsl:value-of select="Name"/>",<xsl:if test="$identity = boolean('true')">
              Identity=true,</xsl:if>
              Type=typeof(<xsl:value-of select="Type"/>).IsEnum ? typeof(int) : typeof(<xsl:value-of select="Type"/>),
              ColumnLocation = ColumnLocation.Database,
              IsRequired = <xsl:value-of select="Required"/>};
      </xsl:if>

      <xsl:if test="$blobContainer = 'true'">
        <xsl:text>
          public override IColumn BlobContainerColumn
          {
              get
              {
                  return </xsl:text><xsl:value-of select="$tableName"/><xsl:text>.</xsl:text><xsl:value-of select="Name"/><xsl:text>Column;
              }
          }
          </xsl:text>
      </xsl:if>

      <xsl:if test="$blobName = 'true'">
        <xsl:text>
          public override IColumn BlobNameColumn
          {
              get
              {
                  return </xsl:text>
        <xsl:value-of select="$tableName"/>
        <xsl:text>.</xsl:text>
        <xsl:value-of select="Name"/>
        <xsl:text>Column;
              }
          }
          </xsl:text>
      </xsl:if>

    </xsl:for-each>
    <xsl:text>

          static </xsl:text><xsl:value-of select="$tableName"/><xsl:text>()
          {
              if (</xsl:text><xsl:value-of select="$tableName"/><xsl:text>.</xsl:text><xsl:value-of select="$tableName"/><xsl:text>Columns == null)
              {
                  </xsl:text><xsl:value-of select="$tableName"/><xsl:text>.</xsl:text><xsl:value-of select="$tableName"/><xsl:text>Columns = new List&lt;IColumn&gt;();
          </xsl:text>
    <xsl:for-each select="Table/Column">
      <xsl:variable name="blobcolumn" select="Server/ColumnLocation = 'BlobStore'" as="xs:boolean"/>
      <xsl:variable name="dbcolumn" select="Server/ColumnLocation = 'Database'" as="xs:boolean"/>
      
      <xsl:variable name="identity" select="Server/Identity = 'true'" as="xs:boolean"/>

      <xsl:if test="$dbcolumn = boolean('true')">
          <xsl:text>
                  </xsl:text><xsl:value-of select="$tableName"/><xsl:text>.</xsl:text><xsl:value-of select="$tableName"/><xsl:text>Columns.Add(</xsl:text><xsl:value-of select="$tableName"/><xsl:text>.</xsl:text><xsl:value-of select="Name"/><xsl:text>Column);</xsl:text>
      </xsl:if>
    </xsl:for-each>
      <xsl:call-template name="blobTemplate" >
      <xsl:with-param name="tableName" select="$tableName"/>
    </xsl:call-template>
    <xsl:text>
              }
          }

          public static ITable Instance
          {
              get
              {
                  return </xsl:text> <xsl:value-of select="$tableName"/><xsl:text>._instance;
              }
          }

          public static IColumn GetColumnFromName(string name)
          {
              return </xsl:text><xsl:value-of select="$tableName"/><xsl:text>.</xsl:text><xsl:value-of select="$tableName"/><xsl:text>Columns.Find( x => string.Compare(x.Name, name, StringComparison.OrdinalIgnoreCase) == 0);
          }

          public override string Name
          {
              get
              {
                  return "</xsl:text><xsl:value-of select="$tableName"/><xsl:text>";
              }
          }

          public override List&lt;IColumn&gt; Columns
          {
              get
              {
                  return </xsl:text><xsl:value-of select="$tableName"/><xsl:text>.</xsl:text><xsl:value-of select="$tableName"/><xsl:text>Columns;
              } 
          }
</xsl:text>
      <xsl:call-template name="blobPropertiesTemplate" >
      <xsl:with-param name="tableName" select="$tableName"/>
    </xsl:call-template>
    <xsl:call-template name="identityPropertyTemplate" >
      <xsl:with-param name="tableName" select="$tableName"/>
    </xsl:call-template><xsl:text>
      }
}
</xsl:text>
  </xsl:template>
  
  <xsl:template name="blobTemplate" match="Table/Column/Server[@ColumnLocation='BlobStore']">
    <xsl:param name="tableName"/>
                  <xsl:text>
                  </xsl:text><xsl:value-of select="$tableName"/><xsl:text>.</xsl:text><xsl:value-of select="$tableName"/>Columns.Add(new DatabaseColumn(){
                      Name="<xsl:value-of select="Table/@TableName"/>BlobName",
                      Identity=false,
                      Type=typeof(Guid),
                      ColumnLocation = ColumnLocation.Database,
                      IsRequired = true,
                      IsBlobName = true});
                  <xsl:text>
                  </xsl:text><xsl:value-of select="$tableName"/><xsl:text>.</xsl:text><xsl:value-of select="$tableName"/>Columns.Add(new DatabaseColumn(){
                      Name="<xsl:value-of select="Table/@TableName"/>BlobValue",
                      Identity=false,
                      Type=typeof(byte[]),
                      ColumnLocation = ColumnLocation.BlobStore,
                      IsRequired = true});
  </xsl:template>

  <xsl:template name="blobPropertiesTemplate" match="Table/Column/Server[@ColumnLocation='BlobStore']">
    <xsl:param name="tableName"/>
    <xsl:text>
          public override IColumn BlobValueColumn
          {
              get
              {
                  if (this.blobValueColumn == null)
                  {
                      foreach (IColumn column in </xsl:text><xsl:value-of select="$tableName"/><xsl:text>.</xsl:text><xsl:value-of select="$tableName"/><xsl:text>Columns)
                      {
                          if (column.ColumnLocation == ColumnLocation.BlobStore)
                          {
                              this.blobValueColumn = column;
                          }
                      }
                  }

                  return this.blobValueColumn;
              }
          }
          </xsl:text>
  </xsl:template>

  <xsl:template name="identityPropertyTemplate" match="Table/Column/Server[@Identity='true']">
    <xsl:param name="tableName"/>
    <xsl:text>
          public override IColumn Identity
          {
              get
              {
                  if (this.identityColumn == null)
                  {
                      foreach (IColumn column in </xsl:text><xsl:value-of select="$tableName"/><xsl:text>.</xsl:text><xsl:value-of select="$tableName"/><xsl:text>Columns)
                      {
                          if (column.Identity == true)
                          {
                              this.identityColumn = column;
                          }
                      }
                  }

                  return this.identityColumn;
              }
          }
          </xsl:text>
  </xsl:template>
  <xsl:template name="blobContainerTemplate" match="Table/Column[@BlobContainer='true']">
    <xsl:param name="tableName"/>
    <xsl:param name="column" select="."/>
    <xsl:param name="blobContainer" select="$column/@BlobContainer"/>
  </xsl:template>

</xsl:stylesheet>
