# Source Projects

Development references used while creating this skill:

- https://github.com/yatharth-doshi/image-to-psd
- https://github.com/rockstarcoder333/Image2PSD

## Capabilities absorbed

From `image-to-psd`:

- Split a flat image into color-derived layers.
- Save individual layer PNGs as a fallback.
- Treat ImageMagick/Wand PSD generation as optional inspiration, not a runtime dependency.

From `Image2PSD`:

- The idea that detected text/regions should become separate layers.
- The distinction between original-position layout and absolute-position reconstruction.

## Deliberate differences

- The bundled script does not require Photoshop, PaddleOCR, Wand, or ImageMagick.
- OCR is not bundled because PaddleOCR is a heavy dependency and the output would still need human font/style cleanup.
- The skill creates raster text layers through the manifest. Editable Photoshop text layers can be added later as an optional Photoshop-specific extension.
- External repositories are references only. Do not import them from the skill at runtime.
