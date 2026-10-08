<?xml version="1.0" encoding="utf-8"?>
<!-- Removes the two executables from the harvested payload: Package.wxs authors them explicitly
     because the service needs ServiceInstall/ServiceControl and the Agent needs a Start menu shortcut. -->
<xsl:stylesheet version="1.0"
                xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                xmlns:wix="http://wixtoolset.org/schemas/v4/wxs">
  <xsl:output method="xml" indent="yes" />

  <xsl:key name="authored"
           match="wix:Component[wix:File[substring(@Source, string-length(@Source) - string-length('\BitShelter.Service.exe') + 1) = '\BitShelter.Service.exe'
                                          or substring(@Source, string-length(@Source) - string-length('\BitShelter.Agent.exe') + 1) = '\BitShelter.Agent.exe']]"
           use="@Id" />

  <xsl:template match="@*|node()">
    <xsl:copy>
      <xsl:apply-templates select="@*|node()" />
    </xsl:copy>
  </xsl:template>

  <xsl:template match="wix:Component[key('authored', @Id)]" />
  <xsl:template match="wix:ComponentRef[key('authored', @Id)]" />
</xsl:stylesheet>
