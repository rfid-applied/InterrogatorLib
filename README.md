# InterrogatorLib

This is a library for dealing with RFID interrogators: use it to
decode or encode TID, EPC memory banks.

## Getting Started

* instantiate `IReaderLowLevel reader = ...` with a suitable
  implementation (these are shipped in separate assemblies)
* singulate a tag using `var tag = reader.SingulateTag()` (this might
  take some time, the method is synchronous)
* read the TID contents:

````
TID tid;
var err = reader.ReadTID(tag, out tid);
if (err == None) {
  Console.WriteLine("Tag contents: MDID = {0}", tid.MDID);
}
````

* dispose of the reader

## Supported readers

Currently: not too many!

* WindowsCE:
  * M3 Orange OX10
  * M3 Orange UHF Gun
* Windows 7+
  * iDTRONIC Evo

## Building

* Windows: use `build.cmd` in command line prompt
  * VS 2017 CE is required
  * for .NET CF install:
    * [.NET Compact Framework 3.5 Redistributable](https://www.microsoft.com/en-us/download/details.aspx?id=65)
    * [Power Toys for .NET Compact Framework 3.5](https://www.microsoft.com/en-us/download/details.aspx?id=13442)
* Linux/Other: not supported yet

## Testing

* Some unit-tests are provided.
* An integration test for VS2008 is also provided, but you will have
  to ensure everything is built (via `build.cmd`) prior to building
  the test project.

## License

[MIT](LICENSE.md)

## Authors

* Dmitry Shalkhakov (principal author), dmitry AT abitech DOT kz
* Artyom Shalkhakov (contributor, RFID UHF codec author), artyom AT abitech DOT kz
