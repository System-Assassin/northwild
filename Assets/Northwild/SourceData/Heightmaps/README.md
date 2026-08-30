# Femundsmarka terrain source

This prototype terrain is a real 1 km x 1 km crop around the western shore of
Nedre Roasten in Femundsmarka National Park, Norway.

- Source: Kartverket / Høydedata, `NHM_DTM_25833` image service
- Source format: floating-point digital terrain model (DTM), GeoTIFF
- WGS84 crop: west `12.05745`, south `62.32820`, east `12.07675`, north `62.33720`
- Runtime resolution: 1025 x 1025 samples (approximately one metre per sample)
- Source elevation range in this crop: 720.77 m to 767.63 m above sea level
- Runtime vertical datum: 0 Unity metres = 715 m above sea level
- Surveyed lake surface used by the prototype: 720.95 m above sea level
- Retrieved: 2026-08-29

`femundsmarka_nedre_roasten_heightmap.png` is the north-up, 16-bit grayscale
heightmap. The runtime `.bytes` copy is little-endian unsigned 16-bit data,
vertically flipped for Unity's terrain axes and normalized across a 60 metre
vertical range.

The source DTM records the lake as a flat surface rather than a lake bed. For
gameplay, only pixels identified as lake surface were lowered by 0.2 to 4.0
metres with a feathered shoreline. Land elevations and shoreline shape remain
from the measured DTM. This synthetic bathymetry is not suitable for navigation
or real-world field use.

Data attribution: ©Kartverket. Kartverket's free products are licensed under
Creative Commons Attribution 4.0 International (CC BY 4.0):
https://www.kartverket.no/en/api-and-data/terms-of-use

Terrain data information:
https://www.kartverket.no/api-og-data/terrengdata
