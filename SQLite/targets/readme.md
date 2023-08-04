# building the SQLIT Interop assemblies and libraries

on the desired target (i.e. on an ARM64 linux device if you want a linux-arm64 binary):

- Create a folder for the source
- copy contents of the source zip to that folder
  - zip available at: https://system.data.sqlite.org/downloads/1.0.118.0/sqlite-netFx-source-1.0.118.0.zip
- edit `Setup/compile-interop-assembly-release.sh` to include `gccflags` for the target architecture
  - e.g. modify line 10 to be `gccflags="-arch x86_64 -arch arm64"`

```
$ cd Setup
$ chmod +x compile-interop-assembly-release.sh
$./compile-interop-assembly-release.sh`
```

The `libSQLite.Interop.so` and `SQLite.Interop.dll` will now be in `./bin/2013/Release/bin`

```
$ curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel STS
$ cd ../System.Data.SQLite.Module.2013.csproj
$ dotnet build -c Release System.Data.SQLite.NetStandard21.csproj
```

The `System.Data.SQLite.dll` assembly will be at `src/bin/NetStandard21/ReleaseNetStandard21/bin/netstandard2.1/System.Data.SQLite.dll`