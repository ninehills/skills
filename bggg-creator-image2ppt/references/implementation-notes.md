# Implementation Notes

This skill follows an IR-first architecture inspired by the local `reference/image2ppt` materials:

- Bitmap images are recognition problems: detect slide structure, OCR text, infer styles, split foreground/background assets, then rebuild the slide from native PPT objects and image components.
- HTML/SVG inputs are compiler problems: parse DOM/SVG nodes directly into manifest elements, preserving geometry and text wherever the source code exposes them.
- Codex image generation is a required companion for bitmap inputs. The scripts cannot call Codex model tools by themselves, so the skill workflow instructs Codex to generate or edit components first, save them in `projects/YYYYMMDD_slug/`, and then run the deterministic PPTX builder.
- The PPTX writer should be independent and reproducible: given `manifest.json` and local assets, it should generate the same `.pptx` without needing Photoshop, PowerPoint, external repos, or network access.

## Conversion Priorities

1. Native PowerPoint text boxes for all readable text.
2. Native shapes for rectangles, rounded rectangles, ellipses, and lines.
3. Tables for simple grid data.
4. Separate image components for photos, illustrations, icons, complex charts, shadows, textures, and anything produced by imagegen.
5. Full-slide image fallback only when the page cannot be safely decomposed; overlay important editable text and shapes on top.

## Project Hygiene

Every run should keep all artifacts in the skill project folder:

```text
projects/YYYYMMDD_slug/
  original_inputs/
  component_images/
  imagegen_assets/
  diagnostics/
  exports/
  manifest.json
  output.pptx
  summary.json
  process_notes.md
```

`imagegen_assets/` stores raw Codex-generated images. `component_images/` stores final cropped/transparent images used by the manifest.

## Known Limits

- `python-pptx` does not render previews by itself. Use `--render-pdf` when LibreOffice is installed, or manually inspect the output in PowerPoint/Keynote/LibreOffice.
- SVG paths, gradients, masks, filters, and complex transforms are not fully converted to native PPT shapes by the helper parser. Convert them to image components when fidelity matters.
- HTML flow layout is only partially recoverable without a browser layout engine. Absolute-positioned slide HTML converts best.
