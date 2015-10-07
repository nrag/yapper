$xslpath = "C:\Users\Nanda\Documents\Visual Studio 2010\Projects\Yapper\New folder\Yapper-InfrastructureChanges\Server\YapperServer\DBLayer\Message.xslt"
$result = "C:\Users\Nanda\Documents\Visual Studio 2010\Projects\Yapper\New folder\Yapper-InfrastructureChanges\Server\YapperServer\DBLayer\Message.generated.cs"
$xmlpath = "C:\Users\Nanda\Documents\Visual Studio 2010\Projects\Yapper\New folder\Yapper-InfrastructureChanges\Server\YapperServer\DBLayer\MessageProperties.xml"
$xmlreader = [System.Xml.XmlReader]::Create($xmlPath)
$xmlWriter = New-Object -TypeName System.Xml.XmlTextWriter -ArgumentList @($result, $null)
$xsl = New-Object -TypeName System.Xml.Xsl.XslCompiledTransform
$xsl.Load($xslpath)
$xsl.Transform($xmlreader, $xmlWriter)
$xmlWriter.Close()
$xmlReader.Close()




