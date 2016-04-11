Scarlet
=======
__Scarlet__ and its related libraries are aiming to provide functionality to convert, export and import various types of game data. They are written in C# and based on the .NET Framework.

Disclaimer
==========
This project is still incomplete and work-in-progress. Functionality will change, be added or removed, and all interfaces, calling conventions, etc. should be considered subject to change.

Parts
=====
* __Scarlet__: Common main library (required)
* __Scarlet.IO.ImageFormats__: Library for image file format conversion (requires Scarlet)
* __Scarlet.IO.ContainerFormats__: Library for container/archive management (requires Scarlet)
* __Scarlet.IO.CompressionFormats__: Library for handling compressed data (requires Scarlet)
* __ScarletTestApp__ (ScarletConvert): Sample converter application implementation (requires all the above)

Formats
=======
File formats that can be loaded and exported/extracted by the libraries as of [this commit](https://github.com/xdanieldzd/Scarlet/tree/6947bb6edb508a4520e126be864099096bc473d9) contain the following:

* __Images__
 * GXT (various PlayStation Vita games)
 * NMT (ex. Disgaea 4, PS Vita version, possibly more Nippon Ichi Software games)
 * SHTX (ex. Danganronpa Another Episode)
 * SHTXFS (ex. Danganronpa Another Episode)
 * STEX (ex. Etrian Odyssey IV, Shin Megami Tensei IV, possibly more Atlus games)
 * TEX (various Capcom games)
 * TID (ex. Hyperdimension Neptunia ReBirth 1, PC _and_ PS Vita versions, possibly more Idea Factory/Compile Heart/Felistella games)
 * TMX (various Atlus games)
 * TX2 (ex. Phantom Brave, various other Nippon Ichi Software games)
 * TXF (ex. Disgaea 4, PS3 version, possibly more Nippon Ichi Software games)
 * TXP (ex. Z.H.P: Unlosing Ranger vs Darkdeath Evilman, possibly more Nippon Ichi Software games)
* __Containers__
 * NISPACK (various Nippon Ichi Software games)
 * NSAC (ex. Disgaea 4, PS Vita version, possibly more Nippon Ichi Software games)
* __Compression__
 * NIS LZS (ex. Disgaea 4, PS3 and PS Vita versions)

Note that support for these is not 100% complete, especially Capcom TEX is lacking, and the unintentional bias towards NIS games.

Acknowledgements
================
* Many parts taken from [GXTConvert](https://github.com/xdanieldzd/GXTConvert), thus that project's acknowledgements apply here as well:
 * PVRTC texture decompression code ported from [PowerVR Graphics Native SDK](https://github.com/powervr-graphics/Native_SDK), Copyright (c) Imagination Technologies Ltd. (see *\Scarlet\Drawing\Compression\PVRTC.cs* and *LICENSE.md*)
 * Texture swizzle logic reverse-engineering and original C implementation by [FireyFly](https://github.com/FireyFly)
 * Testing and moral support by [Ehm2k](https://twitter.com/Ehm2k)
