OpenNETCF.Orm.Oracle requires the following native Oracle client files:

oci.dll
orannzsbb11.dll
oraocci11.dll
oraociei11.dll
OraOps11w.dll


The build system expects these to be present in the source directory at

$(ProjectDir)\ODAC\x86 or it will not build.  Normally I would put these into source control, but one of them is 130MB.  I leave it to you to download and extract the appropriate files from Oracle.