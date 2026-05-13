# Implementation Notes

## Core choices

- `scripts/image2psd.py` writes PSD directly. This keeps the skill independent from Photoshop, ImageMagick, Wand, and psd-tools.
- PSD output is 8-bit RGB with raster layers, alpha channels, Unicode layer names, and a flattened composite image.
- Layers are supplied bottom-to-top by the workflow, then written top-to-bottom in the PSD layer records.
- Layer image data is cropped to non-empty alpha bounds to keep files smaller, while saved PNG layers remain full-canvas for manual reuse.

## From the provided `参考.md`

The useful pattern was:

1. Generate or collect one image per intended design element.
2. Convert white backgrounds to transparency for foreground assets.
3. Preserve pale subjects with an extra soft foreground mask when plain white-to-alpha would erase important whites.
4. Write a PSD with a normal background layer, separate element layers, and a preview PNG for validation.
5. Also save transparent PNG layers as a fallback for PSD readers with limited layer support.

That process is now bundled as `assemble`.

## Successful Codex case review

The poster decomposition succeeded because it treated "PSD conversion" as a
repeatable Codex workflow instead of a single file conversion:

1. Use visual understanding to name the intended layers before coding masks.
2. Keep all extracted layers as full-canvas transparent PNGs so Photoshop can
   stack them at `(0, 0)` without manual alignment.
3. Preserve original pixels for text and logos when the user asks not to change
   relative positions.
4. Make a clean background by inpainting the union of extracted masks.
5. Validate with a flattened preview and inspect suspicious regions at high
   zoom. A clipped header was fixed by expanding its mask, not by moving it.
6. Keep a zip of full-canvas PNG layers as a compatibility fallback.

This pattern should be the default for single-image-to-PSD tasks in Codex.

## Project directory convention

Every real task should live under:

```text
bggg-creator-image2psd/projects/YYYYMMDD_slug/
```

Recommended contents:

- `original_reference.png`: copied source image.
- `layer_sources/`: transparent layer images used by the manifest.
- `psd_full_canvas_layers/`: same-size PNGs for direct Photoshop stacking.
- `manifest.json`: bottom-to-top layer order.
- `output.psd`: final PSD.
- `output.preview.png`: flattened validation preview.
- `psd_full_canvas_layers.zip`: fallback bundle.
- `process_notes.md`: layer list, tool path, validation, known limits.

## Background removal modes

- `none`: keep the image as-is.
- `white`: best for logos, text, dark/colored graphics on white.
- `white-preserve`: best for light foreground subjects on white, such as sails or white packaging.
- `corner`: sample the four corners as the background color.
- `color`: use an explicit `color` field in the manifest layer.

`white-preserve` uses OpenCV if available. If OpenCV is missing, it falls back to plain white-to-alpha.

## Color split mode

`split-colors` reproduces the practical part of color-cluster-to-layer workflows:

- `quantize` is fast and dependency-free beyond Pillow and NumPy.
- `kmeans` uses scikit-learn when available, with a NumPy fallback.
- Each cluster becomes a transparent raster layer using the original pixels.

This is useful for flat posters, scanned graphics, and rough first-pass editability. It is not a semantic object separator.

## Text layers

The manifest supports `type: "text"` to render text as an independent raster layer. The script deliberately does not create Photoshop editable text descriptors because those are fragile and require a much larger PSD implementation. If editable text is mandatory, use Photoshop scripting as an optional downstream step.

## Imagegen integration

In Codex, imagegen should be used by default as a visual/generative helper when:

- a clean background must be reconstructed beyond what local inpaint can do;
- missing elements need to be regenerated;
- the user wants a fresh PSD from a description;
- a product or subject cutout needs a cleaner generated companion layer.

For exact source-image decomposition, avoid regenerating layers that must remain
pixel-identical. Use imagegen only for assisted cleanup or explicitly requested
new/edited assets.
